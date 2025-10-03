import {
    BazlamaPropertyBuilder,
    BazlamaWebComponent,
    ShadowRootMode,
    useCustomHook,
    useElementStyleFromInteger,
    useElementText,
} from "bazlama-web-component"

type ProcessStatus = "Pending" | "Running" | "Completed" | "Failed" | "Canceled"

export default class ProcessItem extends BazlamaWebComponent {
    constructor() {
        super(ShadowRootMode.None)
    }
    /*
    static get Properties() {
        return [
            new BazlamaPropertyBuilder<string>("operationId", "N/A")
                .setAttribute("operation-id", true)
                .setChangeHooks(useElementText("#p-id"))
                .build(),

            new BazlamaPropertyBuilder<string>("operationName", "N/A")
                .setAttribute("operation-name", true)
                .setChangeHooks(useElementText("#p-name"))
                .build(),

            new BazlamaPropertyBuilder<string>("operationDescription", "N/A")
                .setAttribute("operation-description", true)
                .setChangeHooks(useElementText("#p-description"))
                .build(),

            new BazlamaPropertyBuilder<number>("operationTime", 0)
                .setAttribute("operation-time", true)
                .setChangeHooks(useElementText("#p-time"))
                .build(),

            new BazlamaPropertyBuilder<number>("operationProgress", 0)
                .setAttribute("operation-progress", true)
                .setChangeHooks(
                    useElementText("#p-progress", "", "%"),
                    useElementStyleFromInteger("#p-progress", "value")
                )
                .build(),

            new BazlamaPropertyBuilder<ProcessStatus>("operationStatus", "Pending")
                .setAttribute("operation-status", true)
                .setChangeHooks(
                    useElementText(".badge"),
                    useCustomHook(".badge", (target: Element, value: ProcessStatus, _, element) => {
                        const cancelBtn = element.root?.querySelector("button")
                        const progress = element.root?.querySelector("#p-progress")

                        cancelBtn?.classList.remove(
                            "btn-primary",
                            "btn-accent",
                            "btn-success",
                            "btn-error",
                            "btn-warning"
                        )
                        cancelBtn?.classList.add("invisible")

                        progress?.classList.remove(
                            "text-primary",
                            "text-accent",
                            "text-success",
                            "text-error",
                            "text-warning"
                        )

                        target.classList.remove(
                            "badge-primary",
                            "badge-accent",
                            "badge-success",
                            "badge-error",
                            "badge-warning"
                        )
                        switch (value) {
                            case "Pending":
                                target.classList.add("badge-primary")
                                cancelBtn?.classList.add("btn-primary")
                                cancelBtn?.classList.remove("invisible")
                                progress?.classList.add("text-primary")
                                break
                            case "Running":
                                target.classList.add("badge-accent")
                                cancelBtn?.classList.add("btn-accent")
                                cancelBtn?.classList.remove("invisible")
                                progress?.classList.add("text-accent")
                                break
                            case "Completed":
                                target.classList.add("badge-success")
                                progress?.classList.add("text-success")
                                break
                            case "Failed":
                                target.classList.add("badge-error")
                                progress?.classList.add("text-error")
                                break
                            case "Canceled":
                                target.classList.add("badge-warning")
                                progress?.classList.add("text-warning")
                                break
                        }
                        target.textContent = value
                    })
                )
                .build(),
        ]
    }
    */
    override getRenderTemplate() {
        return `        
        <div class="min-h-[9.3rem] stack w-full p-0">
            <div class="h-full p-3 bg-gray-700/50 flex flex-col items-start">
                <div class="w-full grow">
                    <div class="w-full gap-4 flex items-end justify-between">
                        <h2 id="p-name" 
                            class="w-36 overflow-hidden text-ellipsis text-nowrap text-m font-bold text-gray-800 dark:text-white">
                        </h2>
                        <button class="btn btn-xs btn-accent">Cancel</button>
                    </div>
                    <h2 id="p-id" 
                        class="pt-2 text-gray-800 dark:text-gray-400">
                    </h2>
                    <h2 id="p-description"
                        class="w-44 overflow-hidden text-ellipsis text-nowrap text-gray-800 dark:text-gray-400">
                    </h2>
                    <div class="w-full flex gap-2">
                        <span class="text-gray-400 dark:text-gray-300">Execute: </span>
                        <span class="text-gray-200 dark:text-gray-100">
                            <span id="p-time"></span> ms
                        </span>
                    </div>
                </div>
                <div class="w-full flex items-end justify-between">
                    <div class="p-3 badge badge-outline badge-accent"></div>
                </div>
            </div>
            <div class="pb-2 pt-4 w-full flex justify-end justify-items-center">
                <div id="p-progress" 
                    class="radial-progress text-primary" style="--value:100; --size:4rem;" role="progressbar"/>
            </div>
        </div>
        `
    }
}

window.customElements.define("process-item", ProcessItem)