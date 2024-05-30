import { inject } from "vue"
import { humanify } from "@servicestack/client"

export const SortableColumn = {
    template:/*html*/`
        <div class="cursor-pointer flex items-center" @click="toggle()">
          <span>{{ alias ?? humanify(name) }}</span>
          <svg class="w-4 h-4" v-if="routes.sort===name" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"><g fill="none"><path d="M8.998 4.71L6.354 7.354a.5.5 0 1 1-.708-.707L9.115 3.18A.499.499 0 0 1 9.498 3H9.5a.5.5 0 0 1 .354.147l.01.01l3.49 3.49a.5.5 0 1 1-.707.707l-2.65-2.649V16.5a.5.5 0 0 1-1 0V4.71z" fill="currentColor" /></g></svg>
          <svg class="w-4 h-4" v-else-if="routes.sort==='-'+name" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"><g fill="none"><path d="M10.002 15.29l2.645-2.644a.5.5 0 0 1 .707.707L9.886 16.82a.5.5 0 0 1-.384.179h-.001a.5.5 0 0 1-.354-.147l-.01-.01l-3.49-3.49a.5.5 0 1 1 .707-.707l2.648 2.649V3.5a.5.5 0 0 1 1 0v11.79z" fill="currentColor" /></g></svg>
        </div>
    `,
    props: {
        name:String,
        alias:String
    },
    setup(props) {
        const routes = inject('routes')
        const nameDesc = `-${props.name}`

        function toggle() {
            const sort = routes.sort === props.name
                ? nameDesc
                : routes.sort === nameDesc
                    ? ''
                    : props.name
            routes.to({ sort })
        }
        return { routes, toggle, humanify }
    }
}
export default SortableColumn
