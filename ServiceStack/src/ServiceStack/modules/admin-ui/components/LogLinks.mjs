import { inject, onMounted, ref, watch} from "vue"
import { toDate } from "@servicestack/client"
import { useClient, useFormatters } from "@servicestack/vue"
import { GetAnalyticsInfo } from "dtos"
const LogLinks = {
    template:`
    <div v-if="links.length" class="ml-2">
      <nav class="-mb-px flex space-x-4 flex-wrap">
        <a v-href="{ $page:'logging', month:routes.month, $clear:true, ...filter }" :title="title + ' Request Logs'"
           class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
          <svg class=" text-gray-400 group-hover:text-indigo-500 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4"></path>
          </svg>
          <span>All</span>
        </a>
        <a v-for="link in links" :href="link.href" :title="link.label + ' Request Logs'"
           class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
          {{link.label}}
          <span class="ml-2 hidden rounded-full bg-gray-100 group-hover:bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-gray-900 hover:text-indigo-600 md:inline-block">{{link.count}}</span>
        </a>
      </nav>
      <nav v-if="lastLog" class="flex flex-wrap gap-x-2">
        <span v-href="{ $page:'logging', month:routes.month, ...filter, orderBy:'-id', $clear:true }"
              :title="'Last Request on ' + formatDate(lastLog.dateTime)"
              class="group cursor-pointer flex items-center whitespace-nowrap px-1 text-sm font-medium text-gray-500 hover:text-indigo-600">
          <span>Last</span> 
          <span class="px-1">·</span>
          <span>{{relativeTimeFromDate(toDate(lastLog.dateTime))}}</span>
        </span>
        <span v-if="lastLog.op && !filter.op" v-href="{ $page:'logging', month:routes.month, operationName:lastLog.op, ...filter, orderBy:'-id', $clear:true }" :title="lastLog.op + ' API'"
              class="group cursor-pointer flex items-center whitespace-nowrap px-1 text-sm font-medium text-gray-500 hover:text-indigo-600">
            <svg xmlns='http://www.w3.org/2000/svg' class="w-4 h-4" viewBox='0 0 56 56'><path fill='currentColor' d='m11.875 18.438l2.156-2.813c5.672 3.633 11.907 11.79 11.907 17.414v13.453c0 1.617.726 2.485 2.062 2.485c1.313 0 2.04-.868 2.04-2.485V33.04c0-5.555 6.116-13.617 11.812-17.344l1.992 2.72c.984 1.358 2.18 1.054 2.672-.47l2.953-8.625c.422-1.265-.258-2.156-1.547-2.133l-9.14.141c-1.571.024-2.227 1.055-1.266 2.39l1.898 2.602c-5.508 3.867-10.687 10.758-11.39 14.297h-.07c-.704-3.562-5.86-10.43-11.368-14.297l1.828-2.39c1.031-1.313.398-2.367-1.172-2.438l-9.14-.398c-1.29-.07-1.993.797-1.618 2.086l2.72 8.695c.468 1.547 1.64 1.898 2.671.563'/></svg>          
          {{lastLog.op}}
        </span>
        <span v-if="lastLog.userId && !filter.userId" v-href="{ $page:'logging', month:routes.month, userId:lastLog.userId, ...filter, orderBy:'-id', $clear:true }" title="User"
              class="group cursor-pointer flex items-center whitespace-nowrap px-1 text-sm font-medium text-gray-500 hover:text-indigo-600">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z"></path></svg>
          {{lastLog.userName ?? substringWithEllipsis(lastLog.userId, 8)}}
        </span>
        <span v-if="lastLog.apiKey && !filter.apiKey" v-href="{ $page:'logging', month:routes.month, apiKey:lastLog.apiKey, ...filter, orderBy:'-id', $clear:true }" title="API Key"
              class="group cursor-pointer flex items-center whitespace-nowrap px-1 text-sm font-medium text-gray-500 hover:text-indigo-600">
          {{hiddenApiKey(lastLog.apiKey)}}
        </span>
        <span v-if="lastLog.ip && !filter.ip" v-href="{ $page:'logging', month:routes.month, ip:lastLog.ip, ...filter, orderBy:'-id', $clear:true }" title="IP Address"
              class="group cursor-pointer flex items-center whitespace-nowrap px-1 text-sm font-medium text-gray-500 hover:text-indigo-600">
          IP {{lastLog.ip}}
        </span>
        <span v-if="lastLog.browser" class="inline-flex items-center rounded-md bg-gray-50 px-2 py-1 text-xs font-medium text-gray-600 ring-1 ring-inset ring-gray-500/10">{{ lastLog.browser }}</span>
        <span v-if="lastLog.device" class="inline-flex items-center rounded-md bg-gray-50 px-2 py-1 text-xs font-medium text-gray-600 ring-1 ring-inset ring-gray-500/10">{{ lastLog.device }}</span>
        <span v-if="lastLog.bot" class="inline-flex items-center rounded-md bg-gray-50 px-2 py-1 text-xs font-medium text-gray-600 ring-1 ring-inset ring-gray-500/10">{{ lastLog.bot }}</span>
      </nav>
    </div>
    `,
    props: {
        title: String,
        links: Array,
        filter: Object,
    },
    setup(props) {
        const routes = inject('routes')
        const client = useClient()
        const lastLog = ref()
        
        const { relativeTimeFromDate, formatDate } = useFormatters()
        async function update() {
            const api = await client.api(new GetAnalyticsInfo({
                month: routes.month ? `${routes.month}-01` : undefined,
                ...props.filter
            }))
            lastLog.value = api.response?.result
        }
        onMounted(update)
        watch(() => props.links, update)
        function substringWithEllipsis(s, len) {
            return s.length > len ? s.substring(0, len - 3) + '...' : s
        }
        function hiddenApiKey(apiKey) {
            return apiKey.substring(0,3) + '***' + apiKey.substring(apiKey.length-3)
        }
        return {
            routes,
            lastLog,
            hiddenApiKey,
            substringWithEllipsis,
            toDate,
            relativeTimeFromDate,
            formatDate,
        }
    }
}
