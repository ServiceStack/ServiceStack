import { computed, inject } from "vue"
export const AuthenticateDocs = {
    template:`
      <div :class="cls('mx-auto mb-3 p-2')">
          <h2 class="text-3xl text-center my-3">Authenticate API</h2>
          <p class="w-full sm:w-[560px] md:w-[896px] text-gray-500 my-3 text-center">
            The <em>Authenticate</em> API enables Authentication with
            <a class="svg-external text-blue-800" target="_blank" href="https://docs.servicestack.net/auth">ServiceStack
              Auth</a> Providers.
            <br>
            Here are some videos to help get you up to speed quickly
          </p>
          <div class="sm:hidden mb-3">
            <label for="tabs" class="sr-only">Select a tab</label>
            <select id="tabs" name="tabs"
                    class="block w-full focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 rounded-md"
                    @input="changeTab">
              <option v-for="(tab,name) in tabs" :value="tab" :selected="routes.doc==tab">{{ name }}</option>
            </select>
          </div>
          <div class="hidden sm:block">
            <div class="border-b border-gray-200 sm:mb-3">
              <nav class="-mb-px flex" aria-label="Tabs">
                <!-- Current: "border-indigo-500 text-indigo-600", Default: "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300" -->
                <button v-for="(tab,name) in tabs" type="button" v-href="{ doc:tab }"
                        :class="[tab == routes.doc ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 
                          'w-1/3 py-4 px-1 text-center border-b-2 font-medium text-sm']">
                  <span>{{ name }}</span>
                </button>
              </nav>
            </div>
          </div>
          <div>
            <div v-if="routes.doc == ''">
              <iframe :class="clsVideo" src="https://www.youtube.com/embed/XKq7TkZAzeg" allow="autoplay; encrypted-media"
                      allowfullscreen></iframe>
            </div>
            <div v-else-if="routes.doc === 'jwt'">
              <iframe :class="clsVideo" src="https://www.youtube.com/embed/NTCUT7atoLo" allow="autoplay; encrypted-media"
                      allowfullscreen></iframe>
            </div>
            <div v-else-if="routes.doc === 'oauth'">
              <iframe :class="clsVideo" src="https://www.youtube.com/embed/aQqF3Sf2fco" allow="autoplay; encrypted-media"
                      allowfullscreen></iframe>
            </div>
          </div>
      </div>
    `,
    setup() {
        let tabs = {
            'Auth Fundamentals':'',
            'JWT Auth':'jwt',
            'OAuth Providers':'oauth',
        }
        const routes = inject('routes')
        
        function cls(cls) { 
            return 'w-full sm:w-[560px] md:w-[896px]' + (cls ? ' ' + cls : '') 
        }
        const clsVideo = computed(() => cls('h-[315px] sm:h-[315px] md:h-[526px] border-0'))
        function changeTab(e) { routes.to({ doc: e.target.value }) }
        
        return {
            routes,
            tabs,
            cls,
            clsVideo,
            changeTab,
        }
    }
}
