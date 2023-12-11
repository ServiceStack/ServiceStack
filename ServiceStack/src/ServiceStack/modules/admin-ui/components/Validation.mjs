import { computed, inject, onMounted, onUnmounted, ref, nextTick, watch } from "vue"
import { ApiResult, map, uniq } from "@servicestack/client"
import { useClient, useMetadata, useUtils } from "@servicestack/vue"
import { GetValidationRules, ModifyValidationRules, ValidationRule } from "dtos"
const formClass = 'shadow overflow-hidden sm:rounded-md bg-white max-w-screen-lg'
const gridClass = 'grid grid-cols-12 gap-6'
const rowClass = 'col-span-12'
const ApiSelector = {
    template:/*html*/`
      <div>
          <div class="flex flex-wrap">
            <div v-for="x in tags" @click="selectTag(x)"
                 :class="[x === tag ? 'bg-white shadow-inner' : 'bg-indigo-100 border-white', 'mt-0.5 whitespace-nowrap border text-xs inline-flex items-center font-bold leading-4 uppercase px-3 py-0.5 mr-1 text-indigo-800 rounded-full cursor-pointer']">
              {{ x }}
            </div>
          </div>
          <div class="mt-2 max-w-lg">
            <Combobox ref="combo" id="op" label="" v-model="opEntry" :values="opNames" />
          </div>
      </div>
    `,
    setup(props) {
        const app = inject('app')
        const routes = inject('routes')
        const server = inject('server')
        const combo = ref()
        const opEntry = ref()
        const tag = ref()
        let tags = uniq(server.api.operations.flatMap(op => op.tags)).filter(x => !!x)
        const opNames = computed(() => tag.value
            ? server.api.operations.filter(x => x.tags && x.tags.indexOf(tag.value) >= 0).map(op => op.request.name)
            : server.api.operations.map(op => op.request.name))
        
        function selectTag(newTag) {
            tag.value = tag.value === newTag ? null : newTag
            combo.value?.toggle(true)
        }
        
        watch(opEntry, () => {
            const op = opEntry.value?.key
            if (op !== routes.op) {
                routes.to({ op })
            }
        })
        
        function update() {
            opEntry.value = routes.op ? { key: routes.op, value: routes.op } : null
        }
        let sub = null
        onMounted(async () => {
            sub = app.subscribe('route:nav', update)
            update()
        })
        onUnmounted(() => {
            app.unsubscribe(sub)
        })
        
        return { routes, server, opNames, combo, opEntry, tag, tags, selectTag }
    }
}
const EditValidationRule = {
    template:/*html*/`
    <div>
        <form @submit.prevent="submit" autocomplete="off" :class="formClass">
            <div class="relative px-4 py-5 bg-white sm:p-6">
                <CloseButton class="hidden sm:block" @close="done({field})" />
                <ErrorSummary :status="api.error" except="validator,condition,field,errorCode,message,notes" class="p-4" />
                <div>
                    <div class="mb-4">
                        <div class="sm:hidden">
                            <label for="tabs-rule" class="sr-only">Select a tab</label>
                            <select id="tabs-rule" @change="setTypeTab($event.target.value)"
                                    class="block w-full focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 rounded-md">
                                <option value="validator" :selected="typeTab==='validator'">Validator</option>
                                <option value="condition" :selected="typeTab==='condition'">Script</option>
                            </select>
                        </div>
                        <div class="hidden sm:block">
                            <div class="border-b border-gray-200">
                                <nav class="-mb-px flex" aria-label="Tabs">
                                    <button type="button" @click="setTypeTab('validator')"
                                            :class="[typeTab === 'validator' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 'w-1/2 py-4 px-1 text-center border-b-2 font-medium text-sm']"> Validator </button>
                                    <button type="button" @click="setTypeTab('condition')"
                                            :class="[typeTab === 'condition' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 'w-1/2 py-4 px-1 text-center border-b-2 font-medium text-sm']"> Script </button>
                                </nav>
                            </div>
                        </div>
                    </div>
        
                    <div class="flex flex-wrap sm:flex-nowrap">
                        <div class="flex-grow mb-3 sm:mb-0">
                            <fieldset :class="gridClass">
                                <div :class="rowClass">
                                    <TextInput v-if="typeTab==='validator'" id="validator" name="validator" v-model="request.validator"
                                               label="" :placeholder="placeholderValidator" help="Choose from any of the pre-defined validator's below" :spellcheck="false" />
                                    <TextInput v-else-if="typeTab==='condition'" id="condition" name="condition" v-model="request.condition" 
                                               label="" :placeholder="conditionValidator" help="Script Expression that must evaluate to true, see: sharpscript.net" :spellcheck="false" />
                                </div>
                                
                                <div v-if="properties" :class="rowClass">
                                    <SelectInput id="field" v-model="request.field" label="" help="" placeholder="The property this rule applies to" 
                                        :values="properties.map(x => x.name)" />
                                </div>
                                <div :class="rowClass">
                                    <TextInput id="errorCode" v-model="request.errorCode" label="" placeholder="ErrorCode" help="Override with custom error code?" />
                                </div>
                                <div :class="rowClass">
                                    <TextInput id="message" v-model="request.message" label="" placeholder="Error Message" help="Override with custom message?" />
                                </div>
                                <div :class="rowClass">
                                    <TextareaInput id="notes" v-model="request.notes" label="" placeholder="Notes" help="Attach a note to this rule?" />
                                </div>
                            </fieldset>
                        </div>
                    </div>
                </div>
            </div>
            <div class="px-4 py-3 bg-gray-50 text-right sm:px-6 flex justify-between">
                <div>
                    <div v-if="rule && !(loading && !breakpoints.sm)">
                      <ConfirmDelete @delete="submitDelete" />
                    </div>
                </div>
                <Loading v-if="loading" class="m-0" />
                <div class="flex justify-end">
                    <OutlineButton type="button" @click="done({field})" class="mr-2">Close</OutlineButton>
                    <PrimaryButton :disabled="loading || (properties && !request.field || (!request.validator && !request.condition))">
                        {{rule ? 'Edit' : 'Create'}} Rule
                    </PrimaryButton>
                </div>
            </div>
        </form>
        
        <div class="my-8">
            <h4 class="text-xl leading-6 font-medium text-gray-900 mb-3">Quick Select {{isTypeValidator ? 'Type' : 'Property'}} Validator</h4>
        
            <div v-for="x in validators" :key="x.name + x.paramNames" class="mb-2">
                <OutlineButton @click="editValidator(x)">
                    {{fmt(x)}}
                </OutlineButton>
            </div>
        </div>
    </div>
    `,
    props:['type','rule','properties','validators'],
    emits:['done'],
    setup(props, { emit }) {
        const client = useClient()
        let id = id => `${!props.properties ? 'type' : 'prop'}-${id}`
        function done(rule) { emit('done',rule) }
        /** @type {Ref<string|null>} */
        const typeTab = ref('validator')
        const allowDelete = ref(false)
        const loading = ref(false)
        const confirmDelete = ref(false)
        const field = computed(() => props.rule?.field || map(props.properties, p => p[0] && p[0].name) || null)
        const api = ref(new ApiResult())
        const request = ref(new ValidationRule(props.rule))
        const isNew = computed(() => props.rule == null)
        const isTypeValidator = computed(() => props.properties == null)
        const placeholderValidator = computed(() => (isTypeValidator.value ? 'Type' : 'Property') + ' Validator')
        const conditionValidator = computed(() => 'Condition e.g: ' + (isTypeValidator.value
            ? 'dto.Prop1 != dto.Prop2'
            : 'it.isOdd()'))
        const ruleType = computed(() => isTypeValidator.value ? 'type' : 'prop')
        /** @param {string} tab */
        function setTypeTab(tab) {
            typeTab.value = tab
        }
        /** @param {string} tab */
        function activeTypeTab(tab) {
            return typeTab.value === tab
        }
        /** @param {string} sel */
        function focusValidator(sel) {
            let txt = document.querySelector(sel)
            let txtValue = txt ? txt.value : ''
            let hasQuotes = true
            let startPos = txtValue.indexOf("'"), endPos = txtValue.indexOf("'", startPos + 1)
            if (!(startPos >= 0 && endPos >= 0)) {
                hasQuotes = false
                startPos = txtValue.indexOf("{")
                endPos = txtValue.indexOf("}", startPos)
            }
            if (txt && startPos >= 0 && endPos >= 0) {
                txt.selectionStart = hasQuotes ? startPos + 1 : startPos
                txt.selectionEnd = hasQuotes ? endPos : endPos + 1
                txt.focus()
            }
        }
        /** @param {ScriptMethodType} v */
        function editValidator(v) {
            request.value.validator = editfmt(v)
            return nextTick(() => focusValidator(`#${id('validator')}`))
        }
        
        const typesWrapper = {
            'String[]': p => "['" + p + "']",
            'String': p => "'" + p + "'",
        }
        /** @param {string} type
         *  @param {string} p
         */
        function wrap(type, p) {
            const f = typesWrapper[type]
            return f && f(p) || '{' + p + '}'
        }
        /** @param {ScriptMethodType} v */
        function editfmt(v) {
            let paramNames = v.paramNames || []
            return v.name + (paramNames .length > 0 ? `(${paramNames.map((p, i) =>
                wrap(v.paramTypes[i], p)).join(',')})` : '')
        }
        /** @param {ScriptMethodType} v */
        function fmt(v) {
            let paramNames = v.paramNames || []
            return v.name + (paramNames.length > 0 ? `(${paramNames.join(',')})` : '')
        }
        async function submitDelete() {
            api.value = await client.apiVoid(new ModifyValidationRules({ deleteRuleIds: [props.rule.id] }), { jsconfig: 'eccn' })
            if (api.value.succeeded) {
                done(props.rule)
            }
        }
        async function submit() {
            request.value.type = props.type.name 
            api.value = await client.apiVoid(new ModifyValidationRules({
                saveRules: [request.value]
            }), { jsconfig: 'eccn' })
            if (api.value.succeeded) {
                done(request.value)
            }
        }
        
        function update() {
            typeTab.value = request.value.condition && !request.value.validator ? 'condition' : 'validator'
        }
        
        onMounted(update)
        return {
            done,
            request,
            field,
            api,
            typeTab,
            allowDelete,
            loading,
            confirmDelete,
            formClass,
            gridClass,
            rowClass,
            isNew,
            isTypeValidator,
            placeholderValidator,
            conditionValidator,
            id,
            ruleType,
            setTypeTab,
            activeTypeTab,
            focusValidator,
            editValidator,
            typesWrapper,
            wrap,
            editfmt,
            fmt,
            submitDelete,
            submit,
        }
        
    }
}
export const Validation = {
    components: { ApiSelector, EditValidationRule },
    template:/*html*/`
    <section class="">
        <ApiSelector class="mt-4" />
        
        <ErrorSummary />
        
        <main class="mt-8 max-w-screen-xl">
          <div v-if="operation" class="flex flex-wrap">
            <div class="md:flex-1 md:w-1/2 md:p-2 pl-0">
        
              <div class="">
                <div class="flex mb-2">
                  <svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path fill="currentColor" d="M12 2C9.243 2 7 4.243 7 7v2H6c-1.103 0-2 .897-2 2v9c0 1.103.897 2 2 2h12c1.103 0 2-.897 2-2v-9c0-1.103-.897-2-2-2h-1V7c0-2.757-2.243-5-5-5zM9 7c0-1.654 1.346-3 3-3s3 1.346 3 3v2H9V7zm9.002 13H13v-2.278c.595-.347 1-.985 1-1.722c0-1.103-.897-2-2-2s-2 .897-2 2c0 .736.405 1.375 1 1.722V20H6v-9h12l.002 9z"/>
                  </svg>
                  <h3 class="text-xl leading-6 font-medium text-gray-900">
                    Type Validation Rules
                  </h3>
                </div>
        
                <div>
                  <ul role="list" class="divide-y divide-gray-200">
                    <li v-for="x in results.filter(x => x.field == null)" :key="x.id" class="py-4">
                      <EditValidationRule v-if="editTypeRule===x.id" :type="operation.request" :rule="x" :validators="plugin.typeValidators" @done="handleDone" />
                      <div v-else class="flex space-x-3">
                        <div>
                          <button @click="viewTypeForm(x.id)"
                                  class="flex-shrink-0 rounded-full p-1 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500" title="Edit Rule">
                            <svg class="w-6 h-6 text-gray-500 hover:text-gray-900" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g fill="none"><path d="M11 5H6a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-5m-1.414-9.414a2 2 0 1 1 2.828 2.828L11.828 15H9v-2.828l8.586-8.586z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>
                          </button>
                        </div>
                        <div class="flex-1 space-y-1">
                          <div class="flex items-center">
                            <h3 class="text-sm font-medium">{{x.validator ? 'Validator':'Script'}}</h3>
                          </div>
                          <p class="text-sm text-gray-500"><b class="field">{{x.field}}</b>{{x.validator ?? x.condition}}</p>
                        </div>
                      </div>
                    </li>
                  </ul>
        
                  <div class="mt-4">
                    <button v-if="!showTypeForm" @click="viewTypeForm()"
                            class="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-base font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                      Add Type Validation Rule
                    </button>
                    <EditValidationRule v-else-if="editTypeRule==null" :type="operation.request" :validators="plugin.typeValidators" @done="handleDone" />
                  </div>
                </div>
              </div>
        
            </div>
            <div class="md:flex-1 md:w-1/2 md:p-2 pr-0 mt-4 sm:mt-0">
        
              <div v-if="hasProperties" class="">
                <div class="flex mb-2">
                  <svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path fill="currentColor" d="M12 2C9.243 2 7 4.243 7 7v2H6c-1.103 0-2 .897-2 2v9c0 1.103.897 2 2 2h12c1.103 0 2-.897 2-2v-9c0-1.103-.897-2-2-2h-1V7c0-2.757-2.243-5-5-5zM9 7c0-1.654 1.346-3 3-3s3 1.346 3 3v2H9V7zm9.002 13H13v-2.278c.595-.347 1-.985 1-1.722c0-1.103-.897-2-2-2s-2 .897-2 2c0 .736.405 1.375 1 1.722V20H6v-9h12l.002 9z"/>
                  </svg>
                  <h3 class="text-xl leading-6 font-medium text-gray-900">
                    Property Validation Rules
                  </h3>
                </div>
        
                <div>
                  <ul role="list" class="divide-y divide-gray-200">
                    <li v-for="x in results.filter(x => x.field != null)" :key="x.id" class="py-4">
                      <EditValidationRule v-if="editPropertyRule==x.id" :type="operation.request" :rule="x" :properties="opProps" :validators="plugin.propertyValidators" @done="handleDone" />
                      
                      <div v-else class="flex space-x-3">
                        <div>
                          <button @click="viewPropertyForm(x.id)"
                                  class="flex-shrink-0 rounded-full p-1 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500" title="Edit Rule">
                            <svg class="w-6 h-6 text-gray-500 hover:text-gray-900" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g fill="none"><path d="M11 5H6a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-5m-1.414-9.414a2 2 0 1 1 2.828 2.828L11.828 15H9v-2.828l8.586-8.586z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>
                          </button>
                        </div>
                        <div class="flex-1 space-y-1">
                          <div class="flex items-center">
                            <h3 class="text-sm font-medium">{{x.field}} {{x.validator ? 'Validator':'Script'}}</h3>
                          </div>
                          <p class="text-sm text-gray-500">{{x.validator ?? x.condition}}</p>
                        </div>
                      </div>
                    </li>
                  </ul>
        
                  <div class="mt-4">
                    <button v-if="!showPropertyForm" @click="viewPropertyForm()"
                            class="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-base font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                      Add Property Validation Rule
                    </button>
                    <EditValidationRule v-else-if="editPropertyRule==null" :type="operation.request" :properties="opProps" :validators="plugin.propertyValidators" @done="handleDone" />
                  </div>
                </div>
        
              </div>
            </div>
          </div>
        
          <div v-if="dataModelOps.length" class="mt-8">
            <h3 class="text-xl leading-6 font-medium text-gray-900">Quick Jump</h3>
            <div class="flex flex-wrap">
              <div v-for="x in dataModelOps" :key="x.request.name">
                <button v-href="{ op: x.request.name }" :disabled="operation.request.name === x.request.name"
                        :class="[operation.request.name === x.request.name ? 'bg-gray-100 text-gray-700 border-gray-300' : 'border-transparent text-indigo-700 bg-indigo-100 hover:bg-indigo-200', 
                          'mr-2 mt-2 inline-flex items-center px-3 py-2 border text-sm leading-4 font-medium rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500']">
                  {{x.request.name}}
                </button>
              </div>
            </div>
          </div>
        </main>
    
    </section>
    `,
    setup() {
        const routes = inject('routes')
        const server = inject('server')
        const client = useClient()
        
        const { apiOf, findApis, typeProperties } = useMetadata()
        /** @type {Ref<ApiResult<GetValidationRulesResponse>>} */
        const api = ref(new ApiResult())
        const txtFilter = ref('')
        const accessible = ref(true)
        const showTypeForm = ref(false)
        /** @type {Ref<number|null>} */
        const editTypeRule = ref(null)
        const showPropertyForm = ref(false)
        /** @type {Ref<number|null>} */
        const editPropertyRule = ref(null)
        const errorSummary = computed(() => api.value.summaryMessage())
        const results = computed(() => api.value.response?.results || [])
        const plugin = computed(() => server.plugins.validation)
        const loading = computed(() => client.loading.value)
        const operation = computed(() => routes.op ? apiOf(routes.op) : null)
        const opProps = computed(() => typeProperties(operation.value?.request))
        const hasProperties = computed(() => opProps.value.length > 0)
        const dataModelOps = computed(() => map(operation.value?.dataModel, dataModel => findApis({ dataModel })) || [])
        
        /** @param {{field:string,validator?:string,condition?:string}} rule */
        async function handleDone(rule) {
            if (rule.field) {
                showPropertyForm.value = false;
                editPropertyRule.value = null;
            } else {
                showTypeForm.value = false;
                editTypeRule.value = null;
            }
            if (rule.validator || rule.condition) {
                await reset()
            }
        }
        
        /** @param {number|null} ruleId= */
        function viewTypeForm(ruleId) {
            showTypeForm.value = true;
            editTypeRule.value = ruleId;
        }
        
        /** @param {number|null} ruleId= */
        function viewPropertyForm(ruleId) {
            showPropertyForm.value = true;
            editPropertyRule.value = ruleId;
        }
        
        async function reset() {
            api.value = new ApiResult()
            showTypeForm.value = showPropertyForm.value = false
            editTypeRule.value = editPropertyRule.value = null
            if (!operation.value) return
            api.value = await client.api(new GetValidationRules({type: operation.value.request.name}), { jsconfig: 'eccn' }) 
        }
        
        async function update() {
            await reset()
        }
        let sub = null
        onMounted(async () => {
            sub = app.subscribe('route:nav', update)
            await update()
        })
        onUnmounted(() => {
            app.unsubscribe(sub)
        })
        return {
            txtFilter,
            accessible,
            showTypeForm,
            editTypeRule,
            showPropertyForm,
            editPropertyRule,
            loading,
            api,
            errorSummary,
            results,
            plugin,
            operation,
            opProps,
            hasProperties,
            dataModelOps,
            handleDone,
            viewTypeForm,
            viewPropertyForm,
            reset,
            update,
        }
    }
}
