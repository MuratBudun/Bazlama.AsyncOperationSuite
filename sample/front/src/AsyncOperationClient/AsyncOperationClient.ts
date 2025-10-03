import { defaultFetchApiOptions, fetchApi, FetchResult, IFetchApiOptions } from "./fetchApi"
import { IActiveProcess } from "./IActiveProcess"
import { IRegistredPayload } from "./IRegistredPayload"


export default class AsyncOperationClient {
    public apiBaseUrl: string
    public registredAsyncOperationPayloads: IRegistredPayload[] = []

    constructor(apiBaseUrl?: string) {
        this.apiBaseUrl = apiBaseUrl ?? "/api/aos"
        if (this.apiBaseUrl.endsWith("/")) {
            this.apiBaseUrl = this.apiBaseUrl.slice(0, -1)
        }
    }

    getStatusText(status: number) {
        switch (status) {
            case 0: return "Pending"
            case 1: return "Running"
            case 2: return "Completed"
            case 3: return "Failed"
            case 4: return "Canceled"
            default: return "Unknown"
        }
    }

    async init() {
        const result = await this.getRegisteredPayloadTypes()
        if (result.isSuccess) {
            this.registredAsyncOperationPayloads = result.data ?? []
        }
    }

    async getRegisteredPayloadTypes(): Promise<FetchResult<IRegistredPayload[]>> {
        const result = await fetchApi<IRegistredPayload[]>({ url : `${this.apiBaseUrl}/registered-types`})
        return result
    }

    async getActiveProcesses(): Promise<FetchResult<IActiveProcess[]>>
    {
        const result = await fetchApi<IActiveProcess[]>({ url : `${this.apiBaseUrl}/active-processes`})
        return result
    }    

    async Publish(
        payloadType: string, 
        payload: any, 
        waitForQueueSpace = false, 
        waitForPayloadSlotSpace = false)
    {
        const payloadObject = { payloadType: payloadType, ...payload }
        const payloadString = JSON.stringify(payloadObject)

        const fetchApiOptions: IFetchApiOptions = {...defaultFetchApiOptions, ...{
            url: this.apiBaseUrl + "/publish",
            method: 'POST',
            queryStrings: {
                payloadType: payloadType,
                waitForQueueSpace: waitForQueueSpace ? "true" : "false",
                waitForPayloadSlotSpace: waitForPayloadSlotSpace ? "true" : "false"
            },
            body: payloadString
        }}

        const result = await fetchApi(fetchApiOptions)
        return result
    }    
}
