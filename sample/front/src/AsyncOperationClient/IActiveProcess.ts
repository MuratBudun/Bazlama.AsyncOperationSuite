import { AsyncOperationStatus } from "./AsyncOperationStatus"

export interface IActiveProcess {
    createdAt: Date
    runningTimeMs: number
    ownerId: string
    operationId: string
    payloadId: string
    payloadType: string
    operationName: string
    operationDescription: string
    status: AsyncOperationStatus
}
