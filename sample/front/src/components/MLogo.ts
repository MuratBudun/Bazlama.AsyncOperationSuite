import { BazlamaPropertyBuilder, BazlamaWebComponent, CustomElement, Attribute, ShadowRootMode, ChangeHooks } from "bazlama-web-component"
import { IPropertyChangeHandlers } from "bazlama-web-component/dist/component/BazlamaWebComponent"

@CustomElement("m-logo")
export default class MLogo extends BazlamaWebComponent {
    @Attribute("scale-x", true)
    public scaleX: number = 30

    @ChangeHooks([MLogo.drawFunction])
    @Attribute("scale-y", true)
    public scaleY: number = 30

    public showAxis: boolean = false
    public axisColor: string = "black"
    public axisLineWidth: number = 1

    public lineColor: string = "black"
    public lineWidth: number = 2

    constructor() {
        super(ShadowRootMode.None)

        this.InitProperties(this)

        new ResizeObserver(() => {
            this.render()
        }).observe(this)
    }

    static CreatePropertyHooks(): IPropertyChangeHandlers {
        return {
            scaleX: [MLogo.drawFunction]
        }
    }

    static CreatePropertyDefines() {
        return [
            /*
            new BazlamaPropertyBuilder("scaleX", "number")
                .setAttribute("scale-x", true)
                .setChangeHooks(this.drawFunction)
                .build(),

            new BazlamaPropertyBuilder("scaleY", "number")
                .setAttribute("scale-y", true)
                .setChangeHooks(this.drawFunction)
                .build(),
            */

            new BazlamaPropertyBuilder("showAxis", "boolean")
                .setAttribute("show-axis", true)
                .setChangeHooks(this.drawFunction)
                .build(),

            new BazlamaPropertyBuilder("axisColor", "string")
                .setAttribute("axis-color", true)
                .setChangeHooks(this.drawFunction)
                .build(),

            new BazlamaPropertyBuilder("axisLineWidth", "number")
                .setAttribute("axis-line-width", true)
                .setChangeHooks(this.drawFunction)
                .build(),

            new BazlamaPropertyBuilder("lineColor", "string")
                .setAttribute("line-color", true)
                .setChangeHooks(this.drawFunction)
                .build(),

            new BazlamaPropertyBuilder("lineWidth", "number")
                .setAttribute("line-width", true)
                .setChangeHooks(this.drawFunction)
                .build(),
        ]
    }

    static drawFunction = (element: BazlamaWebComponent) => {
        (element as MLogo).drawFunction()
    }

    afterRender(): void {
        this.drawFunction()
    }

    drawFunction() {
        const canvas = this.root?.querySelector("#canvas") as HTMLCanvasElement
        if (!canvas) return

        const ctx = canvas.getContext("2d")
        if (!ctx) return

        const canvasWidth = canvas.width
        const canvasHeight = canvas.height

        const scaleX = this.scaleX || 30
        const scaleY = this.scaleY || 30

        ctx.clearRect(0, 0, canvasWidth, canvasHeight)

        const centerX = canvasWidth / 2
        const centerY = canvasHeight / 2

        if (this.showAxis === true) {
            ctx.beginPath()
            ctx.moveTo(0, centerY)
            ctx.lineTo(canvas.width, centerY)
            ctx.moveTo(centerX, 0)
            ctx.lineTo(centerX, canvas.height)
            ctx.strokeStyle = this.axisColor
            ctx.lineWidth = this.axisLineWidth
            ctx.stroke()
        }

        ctx.beginPath()
        ctx.strokeStyle = this.lineColor

        for (let x = -centerX / scaleX; x <= centerX / scaleX; x += 0.01) {
            const y = Math.cos(x) + Math.sin(x * x)
            const canvasX = centerX + x * scaleX
            const canvasY = centerY - y * scaleY

            if (x === -centerX / scaleX!) {
                ctx.moveTo(canvasX, canvasY) // İlk noktaya taşın
            } else {
                ctx.lineTo(canvasX, canvasY) // Çizgiyi mevcut noktadan devam ettir
            }
        }

        ctx.lineWidth = this.lineWidth
        ctx.stroke()
    }

    getRenderTemplate(): string {
        return `
            <canvas id="canvas" width="${this.clientWidth}" height="${this.clientHeight}"></canvas>
        `
    }
}