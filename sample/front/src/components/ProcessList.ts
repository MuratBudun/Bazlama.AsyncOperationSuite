import { BazlamaWebComponent, ShadowRootMode } from "bazlama-web-component"
import AsyncOperationClient from "../AsyncOperationClient/AsyncOperationClient"
import { IActiveProcess } from "../AsyncOperationClient/IActiveProcess"

export default class ProcessList extends BazlamaWebComponent {
    private client = new AsyncOperationClient()
    private refreshInterval: number = 1000
    private isStarted: boolean = false
    private table?: HTMLTableElement | null

    private activeProcesses: IActiveProcess[] = []

    constructor() {
        super(ShadowRootMode.None)
    }

    async getActiveProcesses() {
        return
        if (!this.isStarted) return

        const activeProcesses = await this.client.getActiveProcesses()

        if (activeProcesses.isSuccess) {
            this.activeProcesses = activeProcesses.data ?? []
            this.renderActiveProcesses()
        }

        setTimeout(() => {
            this.getActiveProcesses()
        }, this.refreshInterval)
    }

    findActiveProcessItemElement(operationId: string): Element | null {
        return this.root?.querySelector(`process-item[operation-id="${operationId}"]`) as Element | null
    }

    createActiveProcessItemElement(process: IActiveProcess): Element {
        const processItem = document.createElement("process-item")
        processItem.setAttribute("operation-id", process.operationId)
        processItem.setAttribute("operation-name", process.operationName)
        processItem.setAttribute("operation-status", this.client.getStatusText(process.status))
        processItem.setAttribute("operation-description", process.operationDescription)
        processItem.setAttribute("operation-progress", process.runningTimeMs.toString())

        return processItem
    }

    renderActiveProcesses() {
        if (!this.table) return
        const tbody = this.table.querySelector("tbody")
        if (!tbody) return

        for (const process of this.activeProcesses ?? []) {
            const existingProcessItem = this.findActiveProcessItemElement(process.operationId)
            if (existingProcessItem) {
                existingProcessItem.setAttribute("operation-status", this.client.getStatusText(process.status))
                existingProcessItem.setAttribute("operation-progress", process.runningTimeMs.toString())
                continue
            }

            const newActiveProcessesElement = this.createActiveProcessItemElement(process);
            const tr = document.createElement("tr")
            const td = document.createElement("td")
            td.className = "p-0"
            td.appendChild(newActiveProcessesElement)
            tr.appendChild(td)
            tbody.insertAdjacentElement("afterbegin", tr)
        }
    }

    connectedCallback() {
        super.connectedCallback()
        this.table = this.root?.querySelector("table")
        this.client.init()
        this.isStarted = true
        //this.getActiveProcesses()
    }

    disconnectedCallback() {
        this.isStarted = false
    }

    override getRenderTemplate() {
        return `
          <table class="table table-pin-rows">
            <tbody>
            </tbody>
          </table>
       `
    }
}

window.customElements.define("process-list", ProcessList)
