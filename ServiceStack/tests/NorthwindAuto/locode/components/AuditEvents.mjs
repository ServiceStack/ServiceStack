import { inject, onMounted, ref } from "vue"
import { useClient, useFormatters } from "@servicestack/vue"
import { GetCrudEvents } from "dtos"

export const AuditEvents = {
    template:/*html*/`
  <div v-if="events.length">
      <div class="flex justify-center">
        <button type="button" @click="open=!open"
                class="px-1 py-1.5 group text-gray-700 font-medium flex items-center" aria-expanded="false">
          <svg class="flex-none w-5 h-5 mr-2 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M3 3v5h5"/><path d="M3.05 13A9 9 0 1 0 6 5.3L3 8"/><path d="M12 7v5l4 2"/></g>
          </svg>
          <span class="mr-1">
            {{eventsCount}} Audit {{ eventsCount === 1 ? 'Event' : 'Events' }}
          </span>
          <svg v-if="!open"
               class="h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          <svg v-else
               class="h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M5 10a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
      <div v-if="open" class="flex">
        <ul v-if="events.length" class="divide-y divide-gray-200 border-t w-full">
          <li v-for="x in events" :key="x.id" class="py-4 cursor-pointer" @click="toggle(x.id)">
            <div class="flex justify-between">
              <div class="pl-4 uppercase inline-block w-20">{{ x.eventType }}</div>
              <div>
                <span :title="'User ' + x.userAuthId">{{ x.userAuthName }}</span>
              </div>
              <div>
                <span :title="formatDate(x.eventDate)">{{ relativeTime(x.eventDate) }}</span>
                <div class="ml-3 inline-block w-5 align-middle">
                  <svg v-if="!expanded(x.id)" class="h-5 w-5 flex-none text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
                  </svg>
                </div>
              </div>
            </div>
            <div class="flex space-x-3">
              <div class="flex-1 flex flex-col">
                <div v-if="expanded(x.id)" class="p-4">
                  <table>
                    <tr>
                      <td class="font-medium">API</td>
                      <td>{{ x.requestType }}</td>
                    </tr>
                    <tr>
                      <td class="pr-4 font-medium">User Id</td>
                      <td>{{ x.userAuthId }}</td>
                    </tr>
                    <tr>
                      <td class="pr-4 font-medium">Date</td>
                      <td>{{ formatDate(x.eventDate) }}</td>
                    </tr>
                    <tr>
                      <td class="pr-4 font-medium">IP</td>
                      <td>{{ x.remoteIp }}</td>
                    </tr>
                  </table>
                </div>
              </div>
              <div class="flex-1">
                <div v-if="expanded(x.id)" class="p-4">
                  <PreviewFormat :value="JSON.parse(x.requestBody)" />
                </div>
              </div>
            </div>
          </li>
        </ul>
      </div>
  </div>
    `,
    props:['type','id'],
    setup(props) {
        const store = inject('store')
        const server = inject('server')
        const client = useClient()

        const open = ref(false)
        const eventsCount = ref(0)
        const events = ref([])
        const expand = ref({})
        const { formatDate, relativeTime } = useFormatters()
        function expanded(id) {
            return !!expand.value[id]
        }
        function toggle(id) {
            expand.value[id] = !expand.value[id]
        }

        onMounted(async () => {
            const request = new GetCrudEvents({
                modelId: props.id,
                model: props.type,
                include: 'total',
                orderBy: '-Id'
            })
            const api = await client.api(request)
            eventsCount.value = 0
            events.value = []
            if (api.succeeded) {
                eventsCount.value = api.response.total
                events.value = api.response.results
            }
        })
        
        return { open, events, eventsCount, formatDate, relativeTime, expanded, toggle }
    }
}
