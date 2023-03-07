import { inject, computed } from "vue"
import { humanize } from "@servicestack/client"

export function install(app) {
    let apis = {
        QueryTodos:  'Query Todos, returns all Todos by default',
        CreateTodo:  'Create a Todo',
        UpdateTodo:  'Update a Todo',
        DeleteTodo:  'Delete Todo by Id',
        DeleteTodos: 'Delete multiple Todos by Ids',
    }
    let apiNames = Object.keys(apis)
    const TodosDocs = {
        template:/*html*/`
        <div class="mx-auto max-w-screen-md text-center py-8">
            <h2 class="text-center text-3xl">{{humanize(op.request.name)}}</h2>
            <p class="text-gray-500 text-lg my-3">{{apis[op.request.name]}}</p>
            <div class="flex justify-center text-left">
                <table>
                    <caption class="mt-3 text-lg font-normal">Other Todo APIs</caption>
                    <tr v-for="(info,name) in otherApis">
                        <th class="text-right font-medium pr-3">
                            <a v-href="{ op:name }" class="text-blue-800">{{humanize(name)}}</a>
                        </th>
                        <td class="text-gray-500">{{info}}</td>
                    </tr>
                </table>
            </div>
        </div>`,
        setup() {
            const store = inject('store')
            const op = computed(() => store.op)
            const otherApis = computed(() => apiNames.filter(x => x !== store.op.request.name)
                 .reduce((acc,x) => { acc[x] = apis[x]; return acc }, {}))
            return { 
                op,
                apis,
                otherApis,
                humanize,
            }
        }
    }
    const components = apiNames.reduce((acc, x) => { acc[x + 'Docs'] = TodosDocs; return acc }, {})
    app.components(components)
}
