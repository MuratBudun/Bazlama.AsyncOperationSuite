export interface IFetchApiOptions {
    url: string,
    method: "GET" | "POST" | "PUT" | "DELETE" | "PATCH" | "OPTIONS" | "HEAD" | "CONNECT" | "TRACE",
    headers: Record<string, string>,
    queryStrings: Record<string, string>,
    body: string | FormData | Blob | ArrayBufferView | ArrayBuffer | URLSearchParams | ReadableStream<Uint8Array> | null | undefined,
    isResponseJson: boolean,
    timeout: number
}

export const defaultFetchApiOptions: IFetchApiOptions = {
    url: "",
    method: "GET",
    headers: { "Content-Type": "application/json"},
    queryStrings: {},
    body: null,
    isResponseJson: true,
    timeout: 5000
}

export type FetchResult<T> = {
    data: T | null,
    isSuccess: boolean,
    httpStatusCode: number
    isTimeout: boolean
    error: Error | null
    errorMessage: string
    serverErrorMessage: string
}

export async function fetchApi<T>(options: Partial<IFetchApiOptions>): Promise<FetchResult<T>>
{
    const finalOptions = { ...defaultFetchApiOptions, ...options }
    const controller = new AbortController()
    const signal = controller.signal

    let result: FetchResult<T> = {
        data: null,
        isSuccess: false,
        httpStatusCode: 0,
        isTimeout: false,
        error: null,
        errorMessage: "",
        serverErrorMessage: ""
    }

    let timeoutId
    if (finalOptions.timeout > 0) {
        timeoutId = setTimeout(() => controller.abort("timeout"), finalOptions.timeout)
    }

    try
    {
        if (finalOptions.queryStrings && Object.keys(finalOptions.queryStrings).length > 0){
            let queryString = new URLSearchParams(finalOptions.queryStrings).toString()
            finalOptions.url += `?${queryString}`
        }

        const response = await fetch(finalOptions.url, {
            method: finalOptions.method,
            headers: finalOptions.headers,
            body: finalOptions.body,
            signal: signal
        })

        if (!response) {
            result.error = new Error("No response")
            result.errorMessage = "No response"
            return result
        }

        result.httpStatusCode = response.status

        if (!response.ok) {
            result.error = new Error(`HTTP error! Status: ${response.status}`)
            result.errorMessage = `HTTP error! Status: ${response.status}`
            result.serverErrorMessage = await response.text()
            return result
        }

        try {
            const data = await response.json()
            result.data = data as T
            result.isSuccess = true
        }
        catch (error: any) {
            result.error = error
            result.errorMessage = error.message
        }

        return result
    }
    catch (error: any)
    {
        if ((error.name ?? "") === "AbortError") {
            result.isTimeout = true
            result.error = error
            result.errorMessage = "Request timeout"
            return result
        }

        result.error = error
        result.errorMessage = error.message?.toString() ?? "Unknown error"
    }
    finally
    {
        if (timeoutId) {
            clearTimeout(timeoutId)
        }

        return result
    }    
}

