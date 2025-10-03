import './style.css'
import "./components/MLogo.ts"
import "./AsyncOperationClient/AsyncOperationClient.ts"


/*
const client = new AsyncOperationClient()


console.log("Client initializing")
await client.init()
client.registredAsyncOperationPayloads.forEach((payload) => {
    console.log(payload)
})   
console.log("Client initialized")

console.log("Publishing payload")
const publishResult = await client.Publish("DelayOperationPayload", { 
    DelaySeconds: 1,
    StepCount: 480
})
console.log(publishResult)
console.log("Payload published")

// async function getActiveProcesses() {
//     const activeProcesses = await client.getActiveProcesses()
    
//     for (const process of activeProcesses.data ?? []) {
//         console.log(process)
//     }

//     setTimeout(() => {
//         getActiveProcesses()
//     }, 1000);
// }

// getActiveProcesses()

*/