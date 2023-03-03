import { inject } from "vue"

export const ApiCode = {
    template:/*html*/`
      <Code :op="routes.op" />
    `,
    setup() {
        const routes = inject('routes')
        return { routes }
    }
}
