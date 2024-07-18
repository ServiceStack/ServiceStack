import { ref, computed, onMounted, inject } from "vue"
import { ApiResult, toDate } from "@servicestack/client"
import { useClient, useUtils, useFormatters, useMetadata, css } from "@servicestack/vue"
import { QueryUserApiKeys, CreateUserApiKey, UpdateUserApiKey, DeleteUserApiKey } from "./dtos.mjs"

function arraysAreEqual(a, b) {
    if (!a || !b) return false
    return a.length === b.length && a.every((v, i) => v === b[i])
}

const CopyIcon = {
    template:`
      <div @click="copy(text)">
          <div class="cursor-pointer select-none p-1 rounded-md border block border-gray-200 bg-white hover:bg-gray-50">
            <svg v-if="copied" class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-gray-500" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg>
          </div>
      </div>
    `,
    props:['text'],
    setup(props) {
        const { copyText } = useUtils()
        const copied = ref(false)

        function copy(text) {
            copied.value = true
            copyText(text)
            setTimeout(() => copied.value = false, 3000)
        }

        return { copied, copy, }
    }
}

const CreateApiKeyForm = {
    components: { CopyIcon },
    template:`
      <div>
        <ModalDialog v-if="apiKey" size-class="w-96" @done="done">
          <div class="bg-white dark:bg-black px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div class="">
              <div class="mt-3 text-center sm:mt-0 sm:mx-4 sm:text-left">
                <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">New API Key</h3>
                <div class="pb-4">
                  <div class="space-y-6 pt-6 pb-5">
                    <div class="flex">
                      <TextInput id="apikey" type="text" v-model="apiKey" label="" @focus="$event.target.select()" readonly
                                 help="Make sure to copy your new API Key now as it wont be available later" />
                      <CopyIcon :text="apiKey" class="mt-1 ml-1" />
                    </div>
                  </div>
                  <div>
                    <PrimaryButton @click="done" class="w-full">Close</PrimaryButton>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </ModalDialog>
        <form v-else @submit="submit" :class="css.card.panelClass">
          <input type="submit" class="hidden">
          <div class="bg-white dark:bg-black relative">
            <CloseButton class="sm:block" @close="$emit('done')" />
            <div class="">
              <div class="pt-3">
                <ErrorSummary v-if="errorSummary" class="mb-3" :errorSummary="errorSummary" />
                <div class="p-4">
                  <fieldset>
                    <div class="grid grid-cols-6 gap-6">
                      <div class="col-span-6 sm:col-span-3">
                        <TextInput id="name" v-model="request.name" required placeholder="Name of this API Key" />
                      </div>
                      <div class="col-span-6 sm:col-span-3">
                        <SelectInput id="expiresIn" v-model="expiresIn" :entries="info.expiresIn" />
                      </div>
                      <div v-if="!info.hide.includes('RestrictTo')" class="col-span-6">
                        <TagInput id="restrictTo" label="Restrict to APIs" v-model="request.restrictTo" :allowableValues="apiKeyApis" />
                      </div>
                      <div v-if="info.scopes.length" class="col-span-6">
                        <div class="mb-2">
                          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Scopes</label>
                        </div>
                        <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                          <CheckboxInput v-for="scope in info.scopes" :id="scope" :label="scope" v-model="scopes[scope]" />
                        </div>
                      </div>
                      <div v-if="info.features.length" class="col-span-6">
                        <div class="mb-2">
                          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Features</label>
                        </div>
                        <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                          <CheckboxInput v-for="feature in info.features" :id="feature" :label="feature" v-model="features[feature]" />
                        </div>
                      </div>
                      <div v-if="!info.hide.includes('Notes')" class="col-span-6">
                        <TextareaInput id="notes" v-model="request.notes" placeholder="Optional Notes about this API Key" class="h-24" />
                      </div>
                    </div>
                  </fieldset>
                </div>
              </div>
            </div>
          </div>
          <div class="px-4 py-3 bg-gray-50 text-right sm:px-6 flex justify-between">
            <div>
              <SecondaryButton @click="$emit('done')">Cancel</SecondaryButton>
            </div>
            <div>
              <PrimaryButton>Create API Key</PrimaryButton>
            </div>
          </div>
        </form>
      </div>
    `,
    props: {
        refId: Number,
        refIdStr: String,
        info: Object,
    },
    emits:['done'],
    setup(props, { emit }) {
        const client = useClient()
        const request = ref(new CreateUserApiKey({
            refId: props.refId,
            refIdStr: props.refIdStr,
        }))
        const apiKey = ref('')
        const expiresIn = ref('')
        const scopes = ref({})
        const features = ref({})
        const api = ref(new ApiResult())
        const errorSummary = computed(() => api.value.summaryMessage())
        const apiKeyApis = computed(() => props.info.requestTypes)
        
        async function submit(e) {
            e.preventDefault()
            if (expiresIn.value) {
                const days = parseInt(expiresIn.value)
                if (days > 0) {
                    const date = new Date()
                    date.setDate(date.getDate() + days)
                    request.value.expiryDate = date
                }
            }
            Object.keys(scopes.value).forEach(k => {
                if (scopes.value[k]) {
                    request.value.scopes ??= []
                    request.value.scopes.push(k)
                }
            })
            Object.keys(features.value).forEach(k => {
                if (features.value[k]) {
                    request.value.features ??= []
                    request.value.features.push(k)
                }
            })
            api.value = await client.api(request.value)
            apiKey.value = api.value.response?.result ?? ''
        }
        
        function done() {
            emit('done')
        }
        
        return { request, expiresIn, scopes, features, api, errorSummary, css, apiKey, apiKeyApis, done, submit }
    }
}

const EditApiKeyForm = {
    template:`
        <div>
          <form @submit="submit" :class="css.card.panelClass">
            <input type="submit" class="hidden">
            <div class="bg-white dark:bg-black relative">
              <CloseButton class="sm:block" @close="$emit('done')" />
              <div class="">
                <div class="pt-3">
                  <ErrorSummary v-if="errorSummary" class="mb-3" :errorSummary="errorSummary" />
                  <div class="p-4">
                    <fieldset>
                      <div class="grid grid-cols-6 gap-6">
                        <div class="col-span-6 sm:col-span-3">
                          <TextInput id="name" v-model="request.name" required placeholder="Name of this API Key" />
                        </div>
                        <div class="col-span-6 sm:col-span-3">
                          <TextInput id="expiryDate" type="date" v-model="request.expiryDate" />
                        </div>
                        <div v-if="!info.hide.includes('RestrictTo')" class="col-span-6">
                          <TagInput id="restrictTo" label="Restrict to APIs" v-model="request.restrictTo" :allowableValues="apiKeyApis" />
                        </div>
                        <div v-if="info.scopes.length" class="col-span-6">
                          <div class="mb-2">
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Scopes</label>
                          </div>
                          <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                            <CheckboxInput v-for="scope in info.scopes" :id="scope" :label="scope" v-model="scopes[scope]" />
                          </div>
                        </div>
                        <div v-if="info.features.length" class="col-span-6">
                          <div class="mb-2">
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Features</label>
                          </div>
                          <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                            <CheckboxInput v-for="feature in info.features" :id="feature" :label="feature" v-model="features[feature]" />
                          </div>
                        </div>
                        <div v-if="!info.hide.includes('Notes')" class="col-span-6">
                          <TextareaInput id="notes" v-model="request.notes" placeholder="Optional Notes about this API Key" class="h-24" />
                        </div>
                      </div>
                      <div class="mt-2 col-span-6">
                        <div v-if="request.cancelledDate" class="flex items-center">
                            <div class="text-red-500">Disabled on {{formatDate(request.cancelledDate)}}</div>
                            <SecondaryButton @click="submitEnable" class="ml-4">Enable API Key</SecondaryButton>
                        </div>
                        <PrimaryButton v-else @click="submitDisable" color="red" class="mr-2">Disable API Key</PrimaryButton>
                      </div>
                    </fieldset>
                  </div>
                </div>
              </div>
            </div>
            <div class="px-4 py-3 bg-gray-50 text-right sm:px-6 flex justify-between">
              <div>
                <ConfirmDelete @delete="submitDelete" />
              </div>
              <div>
                <SecondaryButton @click="$emit('done')">Close</SecondaryButton>
                <PrimaryButton class="ml-2">Save Changes</PrimaryButton>
              </div>
            </div>
          </form>
        </div>
    `,
    emits:['done'],
    props: {
        id: Number,
        info: Object,
    },
    setup(props, { emit }) {
        const client = useClient()
        const { dateInputFormat } = useUtils()
        const { formatDate } = useFormatters()

        let origValues = {}
        const request = ref(new UpdateUserApiKey())
        const scopes = ref({})
        const features = ref({})
        const api = ref(new ApiResult())
        const errorSummary = computed(() => api.value.summaryMessage())
        const apiKeyApis = computed(() => props.info.requestTypes)

        async function submit(e) {
            e.preventDefault()
            
            const update = new UpdateUserApiKey({ id: props.id })
            
            request.value.scopes = []
            Object.keys(scopes.value).forEach(k => {
                if (scopes.value[k]) {
                    request.value.scopes.push(k)
                }
            })
            request.value.features = []
            Object.keys(features.value).forEach(k => {
                if (features.value[k]) {
                    request.value.features.push(k)
                }
            })

            ;['name','expiryDate','scopes','features','restrictTo','notes'].forEach(k => {
                const value = request.value[k]
                const origValue = origValues[k]
                console.log(k, value, origValue, Array.isArray(value) ? arraysAreEqual(value, origValue) : -1)
                if (value === origValue) return
                if (Array.isArray(value)) {
                    if (!origValue || !arraysAreEqual(value, origValue)) {
                        if (value.length === 0) {
                            update.reset ??= []
                            update.reset.push(k)
                        } else {
                            update[k] = value
                        }
                    }
                }
                else if (value) {
                    update[k] = value
                } else {
                    update.reset ??= []
                    update.reset.push(k)
                }
            })

            api.value = await client.api(update)
            done()
        }

        function done() {
            emit('done')
        }

        async function submitDelete() {
            const apiDelete = await client.api(new DeleteUserApiKey({ id: props.id }))
            done()
        }

        async function submitDisable() {
            const apiDelete = await client.api(new UpdateUserApiKey({
                id: props.id,
                cancelledDate: new Date()
            }))
            done()
        }

        async function submitEnable() {
            const apiDelete = await client.api(new UpdateUserApiKey({
                id: props.id,
                reset: ['cancelledDate']
            }))
            done()
        }
        
        onMounted(async () => {
            const apiQuery = await client.api(new QueryUserApiKeys({ id: props.id }))
            if (apiQuery.succeeded && apiQuery.response.results.length === 1) {
                const result = apiQuery.response.results[0]
                request.value = new UpdateUserApiKey(result)
                for (const scope of request.value.scopes) {
                    scopes.value[scope] = true
                }
                for (const feature of request.value.features) {
                    features.value[feature] = true
                }
                request.value.expiryDate = request.value.expiryDate
                    ? dateInputFormat(toDate(request.value.expiryDate))
                    : null
                origValues = { ...result }
            }
        })
        
        return { css, request, scopes, features, errorSummary, formatDate, apiKeyApis,
            submit, submitDelete, submitDisable, submitEnable }
    }
}

const ManageUserApiKeys = {
    components: {
        CreateApiKeyForm,
        EditApiKeyForm,
    },
    template:`
        <div class="mt-4">
          <SecondaryButton @click="toggleDialog('CreateApiKeyForm')">
            Create API Key
          </SecondaryButton>
          <CreateApiKeyForm v-if="show==='CreateApiKeyForm'" :info="info" @done="done" class="mt-2" :key="renderKey" />
          <EditApiKeyForm v-else-if="selected" :info="info" :id="selected" @done="done" class="mt-2" :key="renderKey+1" />
        </div>
        <div class="w-full overflow-auto px-1 -ml-1">
            <DataGrid v-if="api.response?.results?.length" :items="api.response.results"
                      @rowSelected="rowSelected" :isSelected="row => selected === row.id"
                      :rowClass="(row,i) => !row.active ? 'cursor-pointer hover:bg-yellow-50 bg-red-100' : css.grid.getTableRowClass('stripedRows', i, selected === row.id, true)"
                      :headerTitles="{visibleKey:'Secret Key',createdDate:'Created',expiryDate:'Expires'}"
                      :selectedColumns="columns">
              <template #createdDate="{createdDate}">
                {{formatDate(createdDate)}}
              </template>
              <template #expiryDate="{expiryDate}">
                {{formatDate(expiryDate)}}
              </template>
              <template #scopes="{scopes}">
                <span v-if="scopes.length" :title="scopes.join('\\n')">
                    {{ scopes.length }} {{ scopes.slice(0, 2).join(', ') + (scopes.length > 2 ? '...' : '') }}
                </span>
              </template>
              <template #features="{features}">
                <span v-if="features.length" :title="features.join('\\n')">
                    {{ features.length }} {{ features.slice(0, 2).join(', ') + (features.length > 2 ? '...' : '') }}
                </span>
              </template>
              <template #lastUsedDate="{lastUsedDate}">
                <span v-if="lastUsedDate">
                    {{relativeTime(lastUsedDate)}}
                </span>
              </template>
            </DataGrid>
        </div>
    `,
    props: {
        user: Object,
        columns: Array,
        info: Object,
    },
    setup(props) {

        const { formatDate, relativeTime } = useFormatters()
        const columns = props.columns ?? "name,visibleKey,createdDate,expiryDate".split(',')
        columns.push('lastUsedDate')
        
        const renderKey = ref(0)
        const api = new ref(new ApiResult())
        const client = useClient()
        const show = ref('')
        const selected = ref()
        
        async function refresh() {
            const request = new QueryUserApiKeys({ orderBy:'-id' })
            api.value = await client.api(request)
        }
        
        async function done() {
            show.value = ''
            selected.value = null
            await refresh()
        }
        
        function toggleDialog(dialog) {
            show.value = show.value === dialog ? '' : dialog
        }
        
        function rowSelected(row) {
            show.value = ''
            selected.value = selected.value === row.id ? null : row.id
            renderKey.value++
        }
        
        onMounted(async () => {
            await refresh()
        })
        
        return { css, renderKey, columns, show, api, toggleDialog, done, formatDate, relativeTime, selected, rowSelected }
    }
}

function install(app) {
    app.components({ CreateApiKeyForm, EditApiKeyForm })
}

export default ManageUserApiKeys