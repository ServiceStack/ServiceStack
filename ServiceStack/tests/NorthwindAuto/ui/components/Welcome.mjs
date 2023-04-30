import { inject } from "vue"

export const Welcome = {
    template:/*html*/`
    <div class="px-4">
        <div v-if="store.debug">
            <div class="mb-5 bg-pink-50 border-l-4 border-pink-400 p-4 pr-6">
                <div class="flex">
                    <div class="flex-shrink-0">
                        <svg class="mt-0.5 w-6 h-6 text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
                            <path d="M29.83 20l.34-2l-5.17-.85V13v-.23l5.06-1.36l-.51-1.93l-4.83 1.29A9 9 0 0 0 20 5V2h-2v2.23a8.81 8.81 0 0 0-4 0V2h-2v3a9 9 0 0 0-4.71 5.82L2.46 9.48L2 11.41l5 1.36V17.15L1.84 18l.32 2L7 19.18a8.9 8.9 0 0 0 .82 3.57l-4.53 4.54l1.42 1.42l4.19-4.2a9 9 0 0 0 14.2 0l4.19 4.2l1.42-1.42l-4.54-4.54a8.9 8.9 0 0 0 .83-3.57zM15 25.92A7 7 0 0 1 9 19v-6h6zM9.29 11a7 7 0 0 1 13.42 0zM23 19a7 7 0 0 1-6 6.92V13h6z" fill="currentColor"/>
                        </svg>
                    </div>
                    <div class="ml-3">
                        <p class="text-lg text-pink-700">Protected APIs only visible whilst in <b>DebugMode</b></p>
                    </div>
                </div>
            </div>
        </div>
        <div v-if="!store.auth">
          <SignIn v-if="server.plugins.auth" title="Sign in to your account" :provider="routes.provider" @login="store.login($event)" />
          <h1 v-else class="text-2xl mb-3">Welcome to {{store.serviceName}}</h1>
        </div>
        <div v-else>
            <div class="rounded-lg bg-white overflow-hidden shadow mb-3">
                <h2 class="sr-only" id="profile-overview-title">Profile Overview</h2>
                <div class="bg-white p-6">
                    <div class="sm:flex sm:items-center sm:justify-between">
                        <div class="sm:flex sm:space-x-5">
                            <div class="flex-shrink-0">
                                <img class="mx-auto max-h-24 max-w-24 rounded-full"
                                     :src="store.authProfileUrl" alt="">
                            </div>
                            <div class="mt-4 sm:mt-0 sm:pt-1 sm:text-left">
                                <p class="text-sm font-medium text-gray-600">Welcome back,</p>
                                <p class="text-xl font-bold text-gray-900 sm:text-2xl mb-2">{{ store.displayName }}</p>
                                <div v-if="store.authRoles.length" class="mb-2 flex flex-wrap">
                                    <span v-for="role in store.authRoles" title="Role"
                                          class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                      {{role}}
                                    </span>
                                </div>
                                <div v-if="store.authPermissions.length" class="mb-2 flex flex-wrap">
                                    <span v-for="perm in store.authPermissions" title="Permission"
                                          class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                                      {{perm}}
                                    </span>
                                </div>
                            </div>
                        </div>
                        <div class="mt-5 flex justify-center sm:mt-0">
                            <button type="button" @click="store.logout()"
                                    class="flex justify-center items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 whitespace-nowrap">
                                Sign Out
                            </button>
                        </div>
                    </div>
                </div>
    
                <div v-if="store.authLinks.length" class="ml-3 mb-3 flex">
                    <a v-for="link in store.authLinks" :href="link.href"
                       class="mr-3 inline-flex items-center px-6 py-3 border border-gray-300 shadow-sm md:text-lg font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                        <Icon v-if="link.icon" :image="link.icon" class="-ml-1 mr-3 h-5 w-5 md:w-6 md:h-6 text-gray-600" :alt="link.label" />
                        {{link.label}}
                    </a>
                </div>
                
            </div>
        </div>
    </div>
    `,
    setup(props) {
        const store = inject('store')
        const routes = inject('routes')
        /** @type {AppMetadata} */
        const server = inject('server')
        
        return { store, routes, server }
    }
}
