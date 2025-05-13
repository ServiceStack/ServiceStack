import { inject, onMounted, ref } from "vue"
import { on } from "@servicestack/client"
import { useAuth } from "@servicestack/vue"
export const AuthNav = {
    template:/*html*/`
      <div class="ml-3 mt-1 relative bg-gray-50">
          <div v-if="store.auth">
            <div>
              <button type="button" v-href="{ $page:'' }"
                      class="cursor-pointer max-w-xs bg-white rounded-full flex items-center text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 lg:p-2 lg:rounded-md lg:hover:bg-gray-50"
                      aria-expanded="false" aria-haspopup="true">
                <img v-if="store.authProfileUrl" class="h-8 w-8 rounded-full text-gray-700" :src="store.authProfileUrl" :onerror="'this.src=' + JSON.stringify(store.userIconUri)" alt="">
                <span class="hidden mx-3 text-gray-700 text-sm font-medium xl:block">
                    {{ displayName }}
                </span>
              </button>
            </div>
          </div>
          <div v-else>
            <a class="max-w-xs bg-white rounded-full flex items-center text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 lg:p-2 lg:rounded-md lg:hover:bg-gray-50"
               id="user-signin-button" aria-expanded="false" aria-haspopup="true" v-href="{ $page:'' }">
              <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                <path d="M9.586 11L7.05 8.464L8.464 7.05l4.95 4.95l-4.95 4.95l-1.414-1.414L9.586 13H3v-2h6.586zM11 3h8c1.1 0 2 .9 2 2v14c0 1.1-.9 2-2 2h-8v-2h8V5h-8V3z" fill="currentColor" fill-rule="evenodd"/>
              </svg>
              <span class="hidden ml-3 text-gray-700 text-sm font-medium lg:block">Sign In</span>
            </a>
          </div>
      </div>
    `,
    setup(props) {
        const store = inject('store')
        const routes = inject('routes')
        const showPopup = ref(false)
        const { signOut } = useAuth()
        
        async function logout() {
            globalThis.AUTH = store.auth = null
            await signOut()
            routes.to({ op:'', provider:'', skip:'', preview:'', new:'', edit:'' })
        }
        
        onMounted(() => {
            on(document.body, {
                click: e => showPopup.value = false
            })
        })
        
        return {
            store,
            get displayName() {
                let auth = store.auth
                return auth ? auth.displayName || auth.firstName || auth.userName || auth.email : null
            },
            showPopup,
            logout,
        }
        
    }
}
