import { computed, inject, onMounted, onUnmounted, ref, nextTick, watch } from "vue"
import {
    ApiResult, map, $1
} from "@servicestack/client"
import { useClient, css, useUtils } from "@servicestack/vue"
import { keydown } from "app"
import { AdminRedis, AdminRedisResponse } from "dtos"
import { hasItems, indentJson, prettyJson, scrub } from "core"

const formClass = 'shadow overflow-hidden sm:rounded-md bg-white max-w-screen-lg'
const gridClass = 'grid grid-cols-12 gap-6'
const rowClass = 'col-span-12 sm:col-span-6'

const redisTypes = { string:'String', list:'List', set:'Set', zset:'Sorted Set', hash:'Hash' }

const NewKey = {
    template:/*html*/`
<form @submit.prevent="submit" :class="formClass">
<div class="relative px-4 py-5 bg-white sm:p-6">
  <CloseButton @close="done" />
  <div class="text-xl text-gray-900 text-center mb-4">Create {{ redisTypes[type] }}</div>
  <ErrorSummary :status="api.error" />
  <div class="flex flex-wrap sm:flex-nowrap">
    <div class="flex-grow">
      <fieldset :class="gridClass">
        <div class="col-span-12 xl:col-span-6">
          <TextInput id="key" label="Key" v-model="modelValue.key" :class="['zset','hash'].indexOf(type) === -1 ? 'col-span-12' : rowClass"  />
        </div>
        <div v-if="type==='zset'" class="col-span-12 xl:col-span-6">
          <TextInput id="score" label="Score" type="number" step="any" v-model="modelValue.score" :class="rowClass"  />
        </div>
        <div v-if="type==='hash'" class="col-span-12 xl:col-span-6">
          <TextInput id="field" v-model="modelValue.field" :class="rowClass"  />
        </div>
        <div class="col-span-12">
          <TextareaInput id="value" v-model="modelValue.value" :class="rowClass" :spellcheck="false" />
        </div>
      </fieldset>
    </div>
  </div>
</div>
<div class="mt-4 px-4 py-3 bg-gray-50 sm:px-6">
  <div class="flex justify-end">
    <button type="button" @click="done" class="bg-white py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">Cancel</button>
    <button type="submit" :disabled="!modelValue.key" class="ml-3 inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 disabled:bg-indigo-400 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
      Create
    </button>
  </div>
</div>
</form>
    `,
    props:['db','type'],
    emits:['done'],
    setup(props, { emit }) {
        const client = useClient()
        let newKey = () => ({ key:'', field:'', score:0, value:'' })
        const modelValue = ref(newKey())
        const api = ref(new ApiResult())
        
        async function submit() {
            let type = props.type
            let id = modelValue.value.key

            let request = new AdminRedis({ db: parseInt(props.db) })

            if (type === 'string') {
                request.args = ['SET', id, modelValue.value.value]
            } else if (type === 'list') {
                request.args = ['LPUSH', id, modelValue.value.value]
            } else if (type === 'set') {
                request.args = ['SADD', id, modelValue.value.value]
            } else if (type === 'zset') {
                request.args = ['ZADD', id, modelValue.value.score, modelValue.value.value]
            } else if (type === 'hash') {
                request.args = ['HSET', id, modelValue.value.field, modelValue.value.value]
            }

            api.value = await client.api(request, { id, type, jsconfig: 'eccn' })
            if (api.value.succeeded) {
                modelValue.value.type = type
                emit('done', modelValue.value)
                modelValue.value = newKey()
            }
        }
        const errorSummary = computed(() => api.value.summaryMessage())
        
        function done() {
            emit('done', modelValue.value)
        }
        
        return {
            redisTypes,
            modelValue,
            api,
            errorSummary,
            formClass,
            gridClass,
            rowClass,
            submit,
            done,
        }
    }
}

export const Redis = {
    components: { NewKey },
    template:`
<section v-if="!plugin">
  <div class="p-4 max-w-3xl">
    <Alert type="info">Redis Admin UI is not enabled</Alert>
    <div class="my-4">
      <div>
        <p>
            The <b>AdminRedisFeature</b> plugin needs to be configured with your App
            <a href="https://docs.servicestack.net/admin-ui-redis" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
               Learn more <span aria-hidden="true">&rarr;</span>
            </a>
        </p>
      </div>
    </div>
    <div>
        <p class="text-sm text-gray-700 mb-2">Quick start:</p>
        <CopyLine text="npx add-in redis" />
    </div>
  </div>
</section>
<section v-else>
<div>
  <div class="sm:hidden">
    <label for="redis-tabs" class="sr-only">Select a tab</label>
    <select id="redis-tabs"
            class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md"
            @change="routes.to({ tab: $event.target.value })">
      <option v-for="(tab,name) in tabs" :selected="routes.tab == tab" :value="tab">{{ name }}</option>
    </select>
  </div>
  <div class="hidden sm:block">
    <div class="border-b border-gray-200">
      <nav class="-mb-px flex space-x-8" aria-label="Tabs">
        <a v-for="(tab,name) in tabs" v-href="{ tab }"
           :class="[routes.tab == tab ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm']">
          {{ name }}
        </a>
      </nav>
    </div>
  </div>
</div>

<ErrorSummary :status="api.error" />
<div v-if="routes.tab == ''">
  <div class="p-4 flex">
    <h4 v-if="modifiableConnection" v-href="{ edit: routes.edit == 'connection' ? '' : 'connection' }"
        title="Change Redis Connection"
        class="pr-4 font-medium text-3xl text-blue-600 hover:text-blue-800 cursor-pointer select-none">
      {{ connectionString }}</h4>
    <h4 v-else class="pr-4 font-medium text-3xl">{{ connectionString }}</h4>
    <div v-if="connectionString">
      <select v-model="db" title="Redis Database"
              class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md">
        <option v-for="db in plugin.databases" :selected="response && response.db == db" :value="db">{{ db }}</option>
      </select>
    </div>
  </div>
  <div v-if="modifiableConnection && routes.edit === 'connection'" class="mb-8">
    <form ref="form" @submit.prevent="reconnect" autocomplete="off"
          class="shadow overflow-hidden sm:rounded-md bg-white max-w-screen-md">
      <div class="relative px-4 py-5 bg-white sm:p-6">
        <CloseButton @close="routes.to({ edit:'' })" title="Close" />
        <div class="text-xl text-gray-900 text-center mb-4">Change Connection</div>
        <ErrorSummary :status="apiSave.error" />
        <div class="flex flex-1 flex-col justify-between">
            <div class="divide-y divide-gray-200 px-4 sm:px-6">
              <div class="space-y-6 pt-6 pb-5">
                <fieldset :class="gridClass">
                  <div class="col-span-12 xl:col-span-6">
                    <TextInput id="host" v-model="editEndpoint.host" :status="apiSave" />
                  </div>
                  <div class="col-span-12 xl:col-span-6">
                    <TextInput id="port" type="number" v-model="editEndpoint.port" :status="apiSave" />
                  </div>
                  <div class="col-span-12 xl:col-span-6">
                    <TextInput id="username" placeholder="ACL Username"  v-model="editEndpoint.username" :status="apiSave" />
                  </div>
                  <div class="col-span-12 xl:col-span-6">
                    <TextInput id="password" type="password" placeholder="AUTH Password" v-model="editEndpoint.password" :status="apiSave" autocomplete="current password" />
                  </div>
                  <div class="col-span-12 xl:col-span-6">
                    <CheckboxInput id="ssl" label="Use SSL?" v-model="editEndpoint.ssl" :status="apiSave" />
                  </div>
                </fieldset>
              </div>
            </div>
        </div>
      </div>
      <div class="mt-4 px-4 py-3 bg-gray-50 sm:px-6">
        <div class="flex justify-between items-center">
          <div class="flex">
            <Loading v-if="apiSave.loading" class="mb-0 mr-2" />
            <div v-if="apiSave.loading" class="text-gray-500">reconnecting...</div>
          </div>
          <div>
            <SecondaryButton :disabled="apiSave.loading" v-href="{ edit:'' }" class="mr-2">Cancel</SecondaryButton>
            <PrimaryButton :disabled="apiSave.loading">Change</PrimaryButton>
          </div>
        </div>
      </div>
    </form>
  </div>
  <HtmlFormat :value="response?.info" />
</div>
<div v-else-if="routes.tab === 'search'">
  <div class="relative my-4">
    <div class="bg-white py-1.5 px-3.5">
      <svg class="absolute ml-2.5 mt-2 h-4 w-4 text-gray-500" fill="currentColor" xmlns="http://www.w3.org/2000/svg"
           viewBox="0 0 24 24">
        <path
            d="M16.32 14.9l5.39 5.4a1 1 0 0 1-1.42 1.4l-5.38-5.38a8 8 0 1 1 1.41-1.41zM10 16a6 6 0 1 0 0-12 6 6 0 0 0 0 12z"></path>
      </svg>
      <input type="search" v-model="query" placeholder="Search..." @keyup="update"
             class="border rounded-full overflow-hidden flex w-full py-1 pl-8 border-gray-200">
    </div>
  </div>

  <div class="flex">
    <div class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
      <div class="py-2 align-middle inline-block sm:px-6 lg:px-8">
        <div v-if="hasItems(results)" class="md:shadow border-b border-gray-200 md:rounded-lg">
          <table class="divide-y divide-gray-200">
            <thead class="bg-gray-50">
            <tr>
              <th v-for="k in searchResultKeys"
                  class="cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                <div class="flex">
                  <span class="mr-1 select-none">{{ k }}</span>
                </div>
              </th>
            </tr>
            </thead>
            <tbody>
            <tr v-for="(row,index) in results" :key="row.id" @click="toggle(row)"
                :class="['cursor-pointer', expanded(row.id) ? 'bg-indigo-100' : (index % 2 === 0 ? 'bg-white' : 'bg-gray-50') + ' hover:bg-yellow-50']">
              <td v-for="k in searchResultKeys" :key="k" class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                <span :title="row[k]">{{ row[k] }}</span>
              </td>
            </tr>
            </tbody>
          </table>
        </div>
        <div v-else-if="api && api.completed">
          <h3 class="p-2">No Results</h3>
        </div>
      </div>
    </div>
    <div v-if="selected" class="pl-4 flex-1">
      <div>
        <div v-if="item" class="p-2 relative w-full">
            <span class="relative z-0 inline-flex shadow-sm rounded-md">
              <a v-for="(tab,name) in {Pretty:'',Preview:'preview',Edit:'edit'}" v-href="{ body:tab }"
                 :class="[{ Pretty:'rounded-l-md',Preview:'-ml-px',Edit:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                {{ name }}
              </a>
            </span>
          <div v-if="routes.body == ''" class="pt-2 icon-outer" style="min-height:2.5rem">
            <CopyIcon class="absolute right-4" :text="prettyJson(itemValue)" />
            <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="prettyJson(itemValue)"></code></pre>
          </div>
          <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
            <div v-if="itemType==='list'||itemType==='set'">
              <div class="md:shadow border-b border-gray-200 md:rounded-lg">
                <table>
                  <tr v-for="(row,index) in itemValue" :key="index"
                      :class="index % 2 === 0 ? 'bg-white' : 'bg-gray-50'">
                    <td class="px-6 py-4 text-sm text-gray-900">{{ row }}</td>
                  </tr>
                </table>
              </div>
            </div>
            <div v-else-if="itemType==='zset'">
              <div class="md:shadow border-b border-gray-200 md:rounded-lg">
                <table class="divide-y divide-gray-200">
                  <tr v-for="(value,key,index) in itemValue" :key="index"
                      :class="index % 2 === 0 ? 'bg-white' : 'bg-gray-50'">
                    <td class="px-6 py-4 text-sm text-gray-900">{{ key }}</td>
                    <td class="px-6 py-4 text-sm text-gray-900">{{ value }}</td>
                  </tr>
                </table>
              </div>
            </div>
            <div v-else-if="itemType==='string' && !isComplexJson(item.result.text)" class="p-4">{{ item.result.text }} </div>
            <HtmlFormat :value="scrub(Object.assign({},itemValue))" />
          </div>
          <div v-else-if="routes.body === 'edit'" class="pt-2">
            <ErrorSummary v-if="apiSave.error" :status="apiSave.error" />
            <div v-else-if="success" class="rounded-md bg-green-50 p-4 mb-2">
              <div class="flex">
                <div class="flex-shrink-0">
                  <svg class="h-5 w-5 text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                  </svg>
                </div>
                <div class="ml-3">
                  <p class="text-sm font-medium text-green-800">Successfully saved</p>
                </div>
                <div class="ml-auto pl-3">
                  <div class="-mx-1.5 -my-1.5">
                    <button type="button" @click="success=false" class="inline-flex bg-green-50 rounded-md p-1.5 text-green-500 hover:bg-green-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-green-50 focus:ring-green-600">
                      <span class="sr-only">Dismiss</span>
                      <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="itemType==='string'" class="flex flex-col">
              <textarea ref="txtEdit" :class="inputClass('','h-40')" @keyup="resizeEdit" @focus="resizeEdit" spellcheck="false"
                        v-model="itemEdit"></textarea>
              <div class="mt-2 flex justify-between">
                <div>
                  <OutlineButton v-if="isComplexJson(itemEdit)" title="Pretty Print" @click="prettyPrint" class="px-2">
                    <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
                      <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" d="M16 4c-2 0-5 1-5 5v9c0 3-5 5-5 5s5 2 5 5v11c0 4 3 5 5 5M32 4c2 0 5 1 5 5v9c0 3 5 5 5 5s-5 2-5 5v11c0 4-3 5-5 5"/>
                    </svg>
                  </OutlineButton>
                </div>
                <div class="flex items-center">
                  <ConfirmDelete @delete="del" class="mr-2" />
                  <OutlineButton :disabled="itemEdit === itemOrig" @click="save" class="mr-2">Save</OutlineButton>
                </div>
              </div>
            </div>
            <div v-else-if="itemType==='list' || itemType==='set'" class="flex flex-col">
              <div class="flex">
                <div class="md:shadow border-b border-gray-200 md:rounded-lg">
                  <table>
                    <tr v-for="(row,index) in itemValue" :key="index"
                        :class="index % 2 === 0 ? 'bg-white' : 'bg-gray-50'">
                      <td class="px-6 py-4 text-sm text-gray-900">{{ row }}</td>
                      <td class="pr-2 pt-2 w-6">
                        <button type="button" @click="delItem(row)" title="Remove"
                                class="flex-shrink-0 rounded-full p-1 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                          <svg class="w-6 h-6 text-gray-500 hover:text-gray-900" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" role="img" width="1em" height="1em" preserveAspectRatio="xMidYMid meet" viewBox="0 0 24 24">
                            <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.172 14.828L12.001 12m2.828-2.828L12.001 12m0 0L9.172 9.172M12.001 12l2.828 2.828M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2S2 6.477 2 12s4.477 10 10 10Z"/>
                          </svg>
                        </button>
                      </td>
                    </tr>
                  </table>
                </div>
              </div>
              <form @submit.prevent="save" class="mt-2 flex items-end">
                <div class="mr-2">
                  <input type="text" v-model="itemEdit" placeholder="Value" aria-invalid="false"
                         autocomplete="new-password" autofocus
                         :class="inputClass()">
                </div>
                <div>
                  <button type="submit" :disabled="itemEdit === ''"
                          class="relative inline-flex items-center px-4 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium text-gray-700 disabled:text-gray-400 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
                    Add
                  </button>
                </div>
              </form>
            </div>
            <div v-else-if="itemType==='zset' || itemType==='hash'" class="flex flex-col">

              <div class="flex">
                <div class="md:shadow border-b border-gray-200 md:rounded-lg">
                  <table>
                    <tr v-for="(value,key,index) in itemValue" :key="index"
                        :class="index % 2 === 0 ? 'bg-white' : 'bg-gray-50'">
                      <td class="px-6 py-4 text-sm text-gray-900">{{ key }}</td>
                      <td class="px-6 py-4 text-sm text-gray-900">
                        <HtmlFormat v-if="isComplexJson(value)" :value="tryJsonParse(value)" />
                        <div v-else>{{ value }}</div>
                      </td>
                      <td class="pr-2 pt-2 w-6">
                        <button type="button" @click="delItem(key)" title="Remove"
                                class="flex-shrink-0 rounded-full p-1 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                          <svg class="w-6 h-6 text-gray-500 hover:text-gray-900" xmlns="http://www.w3.org/2000/svg"
                               aria-hidden="true" role="img" width="1em" height="1em"
                               preserveAspectRatio="xMidYMid meet" viewBox="0 0 24 24">
                            <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                  stroke-width="1.5"
                                  d="M9.172 14.828L12.001 12m2.828-2.828L12.001 12m0 0L9.172 9.172M12.001 12l2.828 2.828M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2S2 6.477 2 12s4.477 10 10 10Z"/>
                          </svg>
                        </button>
                      </td>
                    </tr>
                  </table>
                </div>
              </div>
              <form @submit.prevent="save" class="mt-2 flex items-end">
                <div class="flex">
                  <input v-if="itemType==='hash'" type="text" v-model="itemField" placeholder="Field"
                         aria-invalid="false" autocomplete="new-password" autofocus :class="inputClass('','mr-2')">
                  <input type="text" v-model="itemEdit" placeholder="Value" aria-invalid="false"
                         autocomplete="new-password" :class="inputClass('','mr-2')">
                  <input v-if="itemType==='zset'" type="number" step="any" v-model="itemScore" placeholder="Score"
                         aria-invalid="false" :class="inputClass('','mr-2')">
                </div>
                <div>
                  <button type="submit" :disabled="itemEdit === ''"
                          class="relative inline-flex items-center px-4 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium text-gray-700 disabled:text-gray-400 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
                    Add
                  </button>
                </div>
              </form>

            </div>

          </div>
        </div>
      </div>

      <div v-if="relatedResults.length > 1" class="mt-8 p-4 max-w-sm rounded overflow-hidden shadow">
        <div class="flex flex-col">
          <div class="-ml-2 -mt-2 flex flex-wrap items-baseline">
            <h3 class="ml-2 mt-2 text-lg leading-6 font-medium text-gray-900">Related Results</h3>
            <p class="ml-2 mt-1 text-sm text-gray-500 truncate">for {{ relatedQuery }}</p>
          </div>
          <nav class="mt-2 space-y-1">
            <a v-for="x in relatedResults" v-href="{ show: x.id }"
               :class="[routes.show === x.id ? 'bg-gray-100 text-gray-900' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900', 'flex items-center px-3 py-2 text-sm font-medium rounded-md']">
              <span class="truncate"> {{ x.id }} </span>
            </a>
          </nav>
        </div>
      </div>

    </div>
    <div v-else class="pl-4 flex-1 max-w-screen-md">

      <div class="z-10 relative inline-block text-left">
        <div>
          <OutlineButton id="menu-button" aria-expanded="true" aria-haspopup="true" @click="showNew=!showNew">
            New
            <svg class="-mr-1 ml-2 h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
            </svg>
          </OutlineButton>
        </div>
        <div :class="['origin-top-right absolute left-0 mt-2 w-56 rounded-md shadow-lg bg-white ring-1 ring-black/5 focus:outline-none',transition1]"
             :style="{ display: showNew ? 'block' : 'none' }"
             role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
          <div class="py-1" role="none">
            <a v-for="(name,type) in redisTypes" v-href="{ new:type }" @click="showNew=false"
               class="text-gray-700 hover:text-gray-900 hover:bg-gray-100 block px-4 py-2 text-sm" role="menuitem"
               tabindex="-1">{{ name }}</a>
          </div>
        </div>
      </div>

      <NewKey v-if="routes.new" class="mt-4" :db="db" :type="routes.new" @done="handleDone" />

    </div>
  </div>
</div>
<div v-else-if="routes.tab === 'command'" class="flex flex-col mt-4">

  <form @submit.prevent="exec" class="flex">
    <input type="text" v-model="command" placeholder="Command..." class="w-full rounded-md">
    <span class="ml-2 relative z-0 inline-flex shadow-sm rounded-md">
      <OutlineButton type="submit">Go</OutlineButton>
    </span>
  </form>

  <div v-if="hasItems(callLog)">
    <div class="my-2 text-center">
      <button @click="callLog.length=0">clear history</button>
    </div>
    <div class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
      <div class="py-2 align-middle inline-block sm:px-6 lg:px-8 w-full">
        <div class="md:shadow border-b border-gray-200 md:rounded-lg">
          <table class="divide-y divide-gray-200 w-full">
            <tr v-for="(c, index) in callLog" :key="index"
                :class="c.error ? 'bg-red-100' : (index % 2 === 0 ? 'bg-white' : 'bg-gray-50')">
              <td class="px-6 py-4 text-sm text-gray-900 font-medium whitespace-nowrap w-6 align-top">
                <span>{{ c.request.db }}&gt;</span>
                <span v-if="c.request.query">query</span>
                <span v-else-if="hasItems(c.request.args)">command</span>
                <span v-else>unknown</span>
              </td>
              <td class="px-6 py-4 text-sm text-gray-900 align-top truncate">
                <div class="grid">
                  <span v-if="c.request.query" :title="c.request.query">{{ c.request.query }}</span>
                  <span v-else-if="hasItems(c.request.args)" :title="join(c.request.args)"
                        @click="command=join(c.request.args)"
                        class="whitespace-nowrap truncate cursor-pointer">{{ join(c.request.args) }}</span>
                  <span v-else>{{ c.request }}</span>
                  <span v-if="c.error"><b>error:</b> {{ c.error && c.error.message }}</span>
                  <div v-else-if="c.value" class="mt-2 whitespace-nowrap truncate">
                    <span v-if="typeof c.value == 'string' && !isComplexJson(c.value)"
                          :title="c.value">{{ c.value }}</span>
                    <span v-else-if="arrayValue(c.value)" :title="arrayValue(c.value)">{{ arrayValue(c.value) }}</span>
                    <HtmlFormat v-else :value="isComplexJson(c.value) ? tryJsonParse(c.value) : c.value" :title="c.result && c.result.text || ''" />
                  </div>
                </div>
              </td>
            </tr>
          </table>
        </div>
      </div>
    </div>
  </div>
</div>

</section>
    `,
    setup() {

        const store = inject('store')
        const routes = inject('routes')
        const server = inject('server')
        const client = inject('client')

        const tabs = { 'Info':'', 'Search':'search', 'Command':'command' }
        const relatedResults = computed(() => apiRelated.value?.response?.searchResults.sort((x,y) => x.id > y.id ? 1 : -1) || [])
        const connectionString = computed(() => endpoint.value.host ? `${endpoint.value.host}:${endpoint.value.port}` : '')
        const modifiableConnection = computed(() => plugin?.modifiableConnection)
        const itemType = computed(() => item.value?.type || '')
        const itemValue = computed(() => item.value?.value || '')

        const db = ref(0)
        const query = ref('')
        const command = ref('')
        const itemEdit = ref('')
        const itemOrig = ref('')
        const itemField = ref('')
        const itemScore = ref(0)

        const relatedQuery = ref('')
        const item = ref()
        const success = ref(false)
        const callLog = ref([])

        const apiCall = ref(0)
        const reconnectedAt = ref(0)

        /** @type {Ref<ApiResult<AdminRedisResponse>>} */
        const api = ref(new ApiResult())
        /** @type {Ref<ApiResult<AdminRedisResponse>>} */
        const apiSave = ref(new ApiResult())
        /** @type {Ref<ApiResult<AdminRedisResponse>>} */
        const apiRelated = ref(new ApiResult())
        
        const showNew = ref(false)
        const rule1 = {
            entering: { cls:'transition ease-out duration-100', from:'transform opacity-0 scale-95', to:'transform opacity-100 scale-100'},
            leaving:  { cls:'transition ease-in duration-75', from:'transform opacity-100 scale-100', to:'transform opacity-0 scale-95' }
        }
        
        const { transition } = useUtils()
        
        const transition1 = ref('')
        watch(showNew, () => {
            transition(rule1, transition1, showNew.value)
        })


        /** @param {string} cmd
         *  @returns {string[]}
         */
        let parseCommand = cmd => {
            let args = []
            let lastPos = 0
            for (let i = 0; i < cmd.length; i++) {
                let c = cmd[i]
                if (c === '{' || c === '[' || c === '"') {
                    break //stop splitting args if value is complex type
                }
                if (c === ' ') {
                    let arg = cmd.substring(lastPos, i)
                    args.push(arg)
                    lastPos = i + 1
                }
            }
            args.push(cmd.substring(lastPos))
            return args
        }
        /** @param {RedisText} r
         *  @returns {*}
         */
        let asObject = r => {
            if (!r)
                return null
            if (r.children && r.children.length > 0) {
                let to = []
                for (let i = 0, len = r.children.length; i < len; i++) {
                    let child = r.children[i]
                    let value = child.text || child.children.map(asObject)
                    to.push(value)
                }
                return to
            }
            return r.text
        }
        /** @param {RedisText} r
         *  @returns {*}
         */
        let asList = r => {
            let children = r && r.children || [];
            let to = children.map(x => x.text);
            return to;
        }
        /** @param {RedisText} r
         *  @returns {*}
         */
        let asKeyValues = r => {
            let list = asList(r)
            let to = {}
            for (let i = 0; i < list.length; i += 2) {
                let key = list[i]
                let val = list[i + 1]
                to[key] = val
            }
            return to
        }
        /** @param {string} x
         *  @returns {string}
         */
        let toQuery = x => !x ? '*'
            : x.indexOf('*') >= 0 || x.indexOf('?') >= 0 || (x.indexOf('[') >= 0 && x.indexOf(']') >= 0)
                ? x
                : x + '*'
        let tryJsonParse = s => {
            try {
                if (typeof s == 'string')
                    return JSON.parse(s)
            } catch (e) {}
            return s
        }
        let join = a => a ? a.join(' ') : ''
        let searchResultKeys = ['id','type','ttl','size']
        let linkFields = 'id,skip'.split(',')
        let plugin = server.plugins.adminRedis
        let cloneEndpoint = endpoint => Object.assign({}, { host:'localhost',port:6379,ssl:false,username:'',password:'' }, endpoint)

        const endpoint = ref(cloneEndpoint(plugin?.endpoint))
        const editEndpoint = ref(cloneEndpoint(plugin?.endpoint))
        
        /** @param {*?} args
         * @returns {AdminRedis} */
        function createRequest(args) {
            return new AdminRedis(Object.assign({ db: parseInt(db.value) }, args))
        }
        
        function reset() {
            success.value = false
            apiSave.value = ref(new ApiResult())
            setItemEdit('')
        }
        
        async function update() {
            reset()

            if (routes.show) {
                let type = selected.value?.type
                if (type) {
                    load(routes.show, type)
                } else {
                    const api = await client.api(createRequest({ args: ['TYPE',routes.show] }), { id:routes.show, jsconfig: 'eccn' })
                    if (api.succeeded) {
                        let type = api.response?.result.text
                        if (type)
                            load(routes.show, type)
                    }
                }
            }

            let request = createRequest()
            if (routes.tab === '')
                request.args = ['INFO']
            else if (routes.tab === 'search')
                request.query = toQuery(query.value)
            else
                return

            let apiCallNo = ++apiCall.value
            api.value = await client.api(request, { jsconfig: 'eccn' })

            /** If reconnected since failed request ignore error */
            if (api.value.error && reconnectedAt.value > apiCallNo) return
            let res = api.value.response
            if (res) {
                if (res.db != null) {
                    db.value = res.db
                }
                setEndpoint(res.endpoint)
            }
        }
        
        async function exec() {
            reset()

            let request = createRequest()
            if (command.value !== '')
                request.args = parseCommand(command.value)

            api.value = await send(request)
            if (api.value.response?.db != null) {
                db.value = api.value.response?.db
            } 
            if (api.value.succeeded) {
                command.value = ''
            }
        }

        /** @param {AdminRedis} request
         * @param {{id:string|*,type:string|*}?} opt
         * @returns {*}
         */
        async function send(request, opt) {

            console.debug('send', request, opt)
            const api = await client.api(request, { jsconfig: 'eccn' })

            if (!opt) opt = { id:null, type:null }
            let { id, type } = opt
            let result = api.response?.result
            let error = api.error || null
            let value = null

            if (api.response && hasItems(api.response.searchResults)) {
                value = api.response.searchResults.map(x => x.id)
            }
            else if (result) {
                if (!hasItems(result.children)) {
                    value = result.text
                }
                else {
                    value = result.children.some(x => hasItems(x.children))
                        ? asObject(result)
                        : asList(result)
                }
            }

            let item = { id, type, request, error, result, value }
            callLog.value.unshift(item)

            return api
        }
        
        function arrayValue(value) { return value && value.join && value.join(', ') || '' }
        function expanded(id) { return selected.value?.id === id }
        function toggle(row) {
            routes.to({ show: routes.show === row.id ? '' : row.id })
        }
        
        const txtEdit = ref()
        
        function resizeEdit() {
            /**@type {HTMLTextAreaElement} */
            let e = txtEdit.value
            if (!e) return
            e.style.height = '1px'
            e.style.height = Math.max(10 + e.scrollHeight, 160) + 'px'
        }
        function prettyPrint() {
            itemEdit.value = indentJson(JSON.parse(itemEdit.value))
            nextTick(resizeEdit)
        }
        
        function handleDone(model) {
            routes.to({ new:'' })
            if (model && model.key) {
                routes.to({ show: model.key })
            }
        }
        
        async function save() {
            if (!item.value) return
            let { id, type } = item.value
            success.value = false
            let value = itemEdit.value

            let request = createRequest()
            if (type === 'string') {
                request.args = ['SET', id, value]
            } else if (type === 'list') {
                request.args = ['LPUSH', id, value]
            } else if (type === 'set') {
                request.args = ['SADD', id, value]
            } else if (type === 'zset') {
                request.args = ['ZADD', id, itemScore.value, value]
            } else if (type === 'hash') {
                request.args = ['HSET', id, itemField.value, value]
            } else {
                return
            }

            apiSave.value = await send(request, { id, type })
            if (apiSave.value.succeeded) {
                success.value = true
                item.value = null
                await load(id, type)
            }
        }
        
        async function del() {
            let { id, type } = item.value
            let request = createRequest({ args: ['DEL', id] })
            apiSave.value = await send(request, { id, type })
            if (apiSave.value.succeeded) {
                success.value = true
                item.value = null
                routes.to({ show:'' })
            }
        }
        
        async function delItem(value) {
            if (!item.value) return
            let { id, type } = item.value

            let request = createRequest()
            if (type === 'list') {
                request.args = ['LREM', id, 1, value]
            } else if (type === 'set') {
                request.args = ['SREM', id, value]
            } else if (type === 'zset') {
                request.args = ['ZREM', id, value]
            } else if (type === 'hash') {
                request.args = ['HDEL', id, value]
            } else {
                return
            }

            apiSave.value = await send(request, { id, type })
            if (apiSave.value.succeeded) {
                item.value = null
                load(id, type)
            }
        }
        
        function setItemEdit(value) {
            itemEdit.value = value || ''
            itemOrig.value = itemEdit.value
            itemField.value = ''
            itemScore.value = 0
        }
        
        /** @param {string} s */
        function isComplexJson(s) {
            return typeof s == 'string' && map(s.trim(), x => x.startsWith('{') || x.startsWith('['))
        }
        
        async function call(args, opt, f) {
            let request = createRequest({ args })
            const r = await client.api(request, { jsconfig: 'eccn' })

            let result = r.response?.result
            let value = result && f(result) || null
            let error = r.error || null
            setItemEdit(result && result.text)
            let { id, type } = opt
            let newItem = { id, type, request, value, error, result }
            callLog.value.unshift(newItem)
            item.value = newItem
            nextTick(() => map($1('[autofocus]'), x => x.focus()))

            let sepPos = id.lastIndexOf(':')
            if (sepPos === -1) id.lastIndexOf('/')
            if (sepPos !== -1) {
                let parent = id.substring(0, sepPos + 1)
                relatedQuery.value = parent + '*'
                apiRelated.value = await send(createRequest({ query: relatedQuery.value }))
            } else {
                apiRelated.value = null
                relatedQuery.value = ''
            }
        }
        
        function load(id, type) {
            if (id === (item.value?.id)) {
                setItemEdit(item.value?.result?.text)
                return
            }
            if (type === 'string') {
                return call(['GET', id], { id, type }, r => tryJsonParse(r.text))
            } else if (type === 'list') {
                return call(['LRANGE', id, '0', '-1'], { id, type }, r => asList(r))
            } else if (type === 'set') {
                return call(['SMEMBERS', id], { id, type }, r => asList(r))
            } else if (type === 'zset') {
                return call(['ZRANGE', id, '0', '-1', 'WITHSCORES'], { id, type }, r => asKeyValues(r))
            } else if (type === 'hash') {
                return call(['HGETALL', id], { id, type }, r => asKeyValues(r))
            }
        }
        
        async function reconnect() {
            let request = new AdminRedis({ reconnect: editEndpoint.value })
            apiSave.value = new ApiResult({ loading: true })
            apiSave.value = await send(request)
            reconnectedAt.value = ++apiCall.value
            if (apiSave.value.succeeded) {
                setEndpoint(apiSave.value.response.endpoint)
                routes.to({ edit:'' })
            }
        }
        
        function setEndpoint(newEndpoint) {
            if (newEndpoint) {
                endpoint.value = newEndpoint
                editEndpoint.value = cloneEndpoint(newEndpoint)
            }
        }
        
        function inputClass(invalid, cls) {
            return [css.input.base, invalid ? css.input.invalid : css.input.valid, cls].join(' ')
        }

        const errorSummary = computed(() => api.value.summaryMessage())
        
        const response = computed(() => api.value.response)
        const results = computed(() => api.value.response?.searchResults || [])
        const total = computed(() => api.value.response?.total)

        const selected = computed(() => routes.show && results.value.find(x => x.id == routes.show))

        function href(links) {
            return Object.assign(linkFields.reduce((acc,x) => { acc[x] = ''; return acc }, {}), links)
        }
        function clearFilters() {
            routes.to(href({show:''}))
        }

        const take = ref(server.plugins.requestLogs?.defaultLimit ?? 100)
        const canPrev = computed(() => routes.skip > 0)
        const canNext = computed(() => results.value.length >= take.value)
        function nextSkip(skip) {
            skip += (parseInt(routes.skip, 10) || 0)
            if (typeof total.value == 'number') {
                const lastPage = Math.floor(total.value / take.value) * take.value
                if (skip > lastPage) return lastPage
            }
            if (skip < 0) return 0
            return skip
        }
        function handleKeyDown(e) {
            keydown(e, { canPrev, canNext, nextSkip, take, results, selected, clearFilters })
        }

        let sub = null
        onMounted(async () => {
            document.addEventListener('keydown', handleKeyDown)
            sub = app.subscribe('route:nav', update)
            await update()
        })

        onUnmounted(() => {
            document.removeEventListener('keydown', handleKeyDown)
            app.unsubscribe(sub)
        })

        return {
            store, 
            routes,
            tabs,
            plugin,

            join,
            tryJsonParse,
            redisTypes,
            searchResultKeys,

            db,
            query,
            command,
            itemEdit,
            itemOrig,
            itemField,
            itemScore,
            api,
            apiSave,
            apiRelated,
            relatedQuery,
            item,
            success,
            callLog,

            endpoint,
            editEndpoint,

            formClass,
            gridClass,
            rowClass,

            response,
            results,

            relatedResults,
            errorSummary,
            selected,

            connectionString,
            modifiableConnection,

            itemType,
            itemValue,
            arrayValue,

            expanded,
            toggle,

            resizeEdit,
            prettyPrint,
            handleDone,
            save,
            del,
            delItem,
            setItemEdit,

            isComplexJson,
            call,
            createRequest,
            reset,
            load,

            apiCall,
            reconnectedAt,
            reconnect,
            setEndpoint,
            update,
            exec,
            send,
            hasItems,
            scrub,
            prettyJson,
            inputClass,
            showNew,
            transition1,
            txtEdit,
        }
    }
}
