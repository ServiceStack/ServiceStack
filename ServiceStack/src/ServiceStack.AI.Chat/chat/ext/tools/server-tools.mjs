import { inject, computed, ref, onMounted, watch } from "vue"

export const ServerTool = {
    template: `
        <div v-if="tool" class="border border-gray-200 dark:border-gray-700/80 rounded-xl mb-4 overflow-hidden transition-all duration-300 shadow-sm hover:shadow-md bg-white dark:bg-gray-900/40">
            <!-- Header Area -->
            <div class="flex items-center justify-between p-4 cursor-pointer select-none hover:bg-gray-50/50 dark:hover:bg-gray-800/10 transition-colors"
                 @click="toggleExpand">
                <div class="flex gap-3 min-w-0">
                    <!-- Custom Toggle Switch -->
                    <label class="relative inline-flex cursor-pointer" @click.stop>
                        <input type="checkbox" v-model="selected" class="sr-only peer">
                        <div class="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all dark:border-gray-600 peer-checked:bg-blue-600"></div>
                    </label>
                    
                    <div class="min-w-0">
                        <div class="flex items-center gap-2 flex-wrap">
                            <span class="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate">
                                {{ tool.title }}
                            </span>
                            <span v-if="selected" class="px-1.5 py-0.5 text-[9px] rounded-full bg-green-100 dark:bg-green-950/40 text-green-700 dark:text-green-400 font-medium">
                                Active
                            </span>
                        </div>
                    </div>
                </div>
                
                <div class="flex items-center gap-2">
                    <!-- Chevron Down / Up -->
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" 
                         class="w-5 h-5 text-gray-400 dark:text-gray-500 transition-transform duration-200" 
                         :class="{ 'rotate-180': isExpanded }">
                        <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                    </svg>
                </div>
            </div>
            
            <!-- Description -->
            <div class="px-4 pb-3 border-b border-gray-100 dark:border-gray-800/40">
                <p class="text-xs text-gray-500 dark:text-gray-400 font-normal leading-relaxed">
                    {{ tool.description }}
                </p>
            </div>

            <!-- Parameter Config Form UI (expanded if isExpanded) -->
            <div v-show="isExpanded" class="p-4 bg-gray-50/30 dark:bg-gray-900/10 border-t border-gray-100 dark:border-gray-800/40 transition-all duration-300">
                <!-- Render Groups -->
                <div v-for="group in groups" :key="group.id" class="mb-6 last:mb-0">
                    <h4 class="text-[11px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-3 pb-1 border-b border-gray-100 dark:border-gray-800">
                        {{ group.label }}
                    </h4>
                    
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <template v-for="fieldPath in group.fields" :key="fieldPath">
                            <div v-if="shouldShowField(getFieldSchema(fieldPath))" 
                                 class="flex flex-col gap-1.5"
                                 :class="{ 'col-span-1 md:col-span-2': isFullWidth(getFieldSchema(fieldPath)) }">
                                
                                <div class="flex items-center justify-between">
                                    <label class="text-xs font-medium text-gray-700 dark:text-gray-300">
                                        {{ getFieldSchema(fieldPath)?.title }}
                                        <span v-if="isRequired(fieldPath)" class="text-red-500 ml-0.5">*</span>
                                    </label>
                                    <span v-if="getFieldSchema(fieldPath)?.ui?.help" class="text-[10px] text-gray-400 dark:text-gray-500">
                                        {{ getFieldSchema(fieldPath)?.ui?.help }}
                                    </span>
                                </div>

                                <!-- WIDGET RENDERING -->
                                
                                <!-- 1. select -->
                                <div v-if="getFieldSchema(fieldPath)?.ui?.widget === 'select'">
                                    <select :value="getParameterByPath(fieldPath)"
                                            @change="updateParameterByPath(fieldPath, $event.target.value)"
                                            class="w-full rounded-md px-3 py-1.5 text-xs focus:ring-1 focus:ring-blue-500"
                                            :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                                        <option :value="undefined" disabled>Select...</option>
                                        <option v-for="opt in getFieldSchema(fieldPath)?.enum" :key="opt" :value="opt">
                                            {{ opt }}
                                        </option>
                                    </select>
                                </div>

                                <!-- 2. radio -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'radio'" class="flex flex-wrap gap-2">
                                    <label v-for="opt in getFieldSchema(fieldPath)?.enum" :key="opt" 
                                           class="flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-xs cursor-pointer transition-colors"
                                           :class="[getParameterByPath(fieldPath) === opt 
                                               ? 'bg-blue-50 dark:bg-blue-950/30 border-blue-400 text-blue-700 dark:text-blue-300 font-medium' 
                                               : 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-gray-300 dark:hover:bg-gray-700 dark:hover:border-gray-600']">
                                        <input type="radio" 
                                               :name="tool.$id + '-' + fieldPath" 
                                               :value="opt"
                                               :checked="getParameterByPath(fieldPath) === opt"
                                               @click.prevent="toggleRadio(fieldPath, opt)"
                                               class="sr-only" />
                                        <span>{{ opt }}</span>
                                        <span v-if="getFieldSchema(fieldPath)?.ui?.enumDescriptions?.[opt]" 
                                              class="text-[10px] text-gray-400 font-normal">
                                            - {{ getFieldSchema(fieldPath)?.ui?.enumDescriptions[opt] }}
                                        </span>
                                    </label>
                                </div>

                                <!-- 3. toggle -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'toggle'">
                                    <label class="relative inline-flex items-center cursor-pointer">
                                        <input type="checkbox"
                                               :checked="!!getParameterByPath(fieldPath)"
                                               @change="updateParameterByPath(fieldPath, $event.target.checked)"
                                               class="sr-only peer" />
                                        <div class="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all dark:border-gray-600 peer-checked:bg-blue-600"></div>
                                        <span class="ml-2 text-xs text-gray-600 dark:text-gray-400">
                                            {{ getParameterByPath(fieldPath) ? 'Enabled' : 'Disabled' }}
                                        </span>
                                    </label>
                                </div>

                                <!-- 4. number -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'number'">
                                    <input type="number"
                                           :value="getParameterByPath(fieldPath)"
                                           @input="updateParameterByPath(fieldPath, $event.target.value === '' ? undefined : Number($event.target.value))"
                                           :min="getFieldSchema(fieldPath)?.minimum"
                                           :max="getFieldSchema(fieldPath)?.maximum"
                                           class="w-full px-3 py-1.5 text-xs rounded-md focus:ring-1 focus:ring-blue-500"
                                           :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                                           :placeholder="getFieldSchema(fieldPath)?.ui?.placeholder || 'e.g. 5'" />
                                </div>

                                <!-- 5. slider -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'slider'" class="flex items-center gap-4">
                                    <input type="range"
                                           :value="getParameterByPath(fieldPath)"
                                           @input="updateParameterByPath(fieldPath, Number($event.target.value))"
                                           :min="getFieldSchema(fieldPath)?.minimum ?? 0"
                                           :max="getFieldSchema(fieldPath)?.maximum ?? 100"
                                           :step="getFieldSchema(fieldPath)?.ui?.step ?? 1"
                                           class="flex-1 accent-blue-600 dark:accent-blue-500 h-1 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer" />
                                    <span class="text-xs font-mono text-gray-600 dark:text-gray-400 w-8 text-right font-semibold">
                                        {{ getParameterByPath(fieldPath) }}
                                    </span>
                                </div>

                                <!-- 6. textarea -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'textarea'">
                                    <textarea :value="getParameterByPath(fieldPath)"
                                              @input="updateParameterByPath(fieldPath, $event.target.value)"
                                              :rows="getFieldSchema(fieldPath)?.ui?.rows || 3"
                                              class="w-full px-3 py-1.5 text-xs rounded-md focus:ring-1 focus:ring-blue-500 font-mono"
                                              :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                                              :placeholder="getFieldSchema(fieldPath)?.ui?.placeholder"></textarea>
                                </div>

                                <!-- 7. combobox / timezone-select -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'combobox' || getFieldSchema(fieldPath)?.ui?.widget === 'timezone-select'">
                                    <input type="text"
                                           :value="getParameterByPath(fieldPath)"
                                           @input="updateParameterByPath(fieldPath, $event.target.value)"
                                           :list="tool.$id + '-' + fieldPath + '-list'"
                                           class="w-full px-3 py-1.5 text-xs rounded-md focus:ring-1 focus:ring-blue-500"
                                           :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                                           :placeholder="getFieldSchema(fieldPath)?.ui?.placeholder || 'Type or select option...'" />
                                    <datalist :id="tool.$id + '-' + fieldPath + '-list'">
                                        <option v-for="ex in getFieldSchema(fieldPath)?.examples" :key="ex" :value="ex"></option>
                                    </datalist>
                                </div>

                                <!-- 8. tag-input -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'tag-input'" class="flex flex-col gap-2">
                                    <div class="flex flex-wrap gap-1.5">
                                        <span v-for="tag in (getParameterByPath(fieldPath) || [])" :key="tag" 
                                              class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-gray-100 dark:bg-gray-800 text-[11px] text-gray-700 dark:text-gray-300 font-medium">
                                            <span>{{ tag }}</span>
                                            <button type="button" @click="removeTag(fieldPath, tag)" 
                                                    class="hover:text-red-500 transition-colors focus:outline-none">&times;</button>
                                        </span>
                                    </div>
                                    <input type="text"
                                           v-model="localInputs[fieldPath]"
                                           @keydown.enter.prevent="addLocalTag(fieldPath)"
                                           class="w-full px-3 py-1.5 text-xs rounded-md focus:ring-1 focus:ring-blue-500"
                                           :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                                           :placeholder="getFieldSchema(fieldPath)?.ui?.placeholder || 'Type value and press Enter...'" />
                                </div>

                                <!-- 9. tool-list -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'tool-list'" class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                                    <div v-for="toolType in getToolListEnum(getFieldSchema(fieldPath))" :key="toolType"
                                         @click="toggleToolType(fieldPath, toolType)"
                                         class="flex items-center gap-2 p-2 rounded-lg border text-xs cursor-pointer select-none transition-all duration-200"
                                         :class="[hasToolType(fieldPath, toolType)
                                             ? 'bg-blue-50 dark:bg-blue-950/20 border-blue-300 text-blue-800 dark:text-blue-300'
                                             : 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50/50 dark:hover:bg-gray-800/30 text-gray-600 dark:text-gray-400']">
                                        <input type="checkbox"
                                               :checked="hasToolType(fieldPath, toolType)"
                                               :class="[$styles.borderInput, $styles.textInput, $styles.checkbox]" />
                                        <span class="font-mono text-[11px]">{{ toolType }}</span>
                                    </div>
                                </div>

                                <!-- 10. fieldset -->
                                <div v-else-if="getFieldSchema(fieldPath)?.ui?.widget === 'fieldset'" 
                                     class="border border-gray-200 dark:border-gray-700/80 rounded-lg p-3 bg-gray-50/50 dark:bg-gray-800/40">
                                    <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
                                        <div v-for="(subProp, subName) in getFieldSchema(fieldPath)?.properties" :key="subName" class="flex flex-col gap-1">
                                            <label class="text-[9px] uppercase font-bold text-gray-400 dark:text-gray-500 tracking-wider">
                                                {{ subProp.title || subName }}
                                            </label>
                                            <input type="text"
                                                   :value="getParameterByPath(fieldPath + '.' + subName)"
                                                   @input="updateParameterByPath(fieldPath + '.' + subName, $event.target.value)"
                                                   class="w-full px-2 py-1 text-xs border rounded focus:ring-1 focus:ring-blue-500"
                                                   :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                                                   :placeholder="subProp.description || ''" />
                                        </div>
                                    </div>
                                </div>

                                <!-- Fallback input -->
                                <div v-else>
                                    <input type="text"
                                           :value="getParameterByPath(fieldPath)"
                                           @input="updateParameterByPath(fieldPath, $event.target.value)"
                                           class="w-full px-3 py-1.5 text-xs rounded-md focus:ring-1 focus:ring-blue-500"
                                           :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                                </div>

                                <!-- Description -->
                                <p class="text-[10px] text-gray-400 dark:text-gray-500 px-1 leading-normal">
                                    {{ getFieldSchema(fieldPath)?.description }}
                                </p>
                            </div>
                        </template>
                    </div>
                </div>
            </div>
        </div>
    `,
    props: {
        ext: Object,
        tool: Object
    },
    setup(props) {
        const ctx = inject('ctx')
        const ext = props.ext

        const toolId = computed(() => props.tool?.$id)
        const userExpanded = ref(false)
        const localInputs = ref({})

        const serverTools = computed(() => {
            if (!ext.prefs.serverTools) {
                ext.setPrefs({ serverTools: {} })
            }
            return ext.prefs.serverTools
        })

        const selected = computed({
            get() {
                return !!serverTools.value[toolId.value]?.selected
            },
            set(val) {
                const current = serverTools.value[toolId.value] || { selected: false, config: getDefaultConfig(props.tool) }
                current.selected = val
                ext.setPrefs({
                    serverTools: {
                        ...serverTools.value,
                        [toolId.value]: current
                    }
                })
            }
        })

        watch(selected, (newVal) => {
            userExpanded.value = newVal
        }, { immediate: true })

        const isExpanded = computed(() => {
            return selected.value && userExpanded.value
        })

        const toggleExpand = () => {
            if (!selected.value) {
                selected.value = true
                userExpanded.value = true
            } else {
                userExpanded.value = !userExpanded.value
            }
        }

        const groups = computed(() => {
            if (props.tool?.ui?.groups) {
                return props.tool.ui.groups
            }
            const fields = []
            const paramsSchema = props.tool?.properties?.parameters
            if (paramsSchema && paramsSchema.properties) {
                Object.keys(paramsSchema.properties).forEach(key => {
                    fields.push(`parameters.${key}`)
                })
            }
            return [
                {
                    id: "all",
                    label: "Parameters",
                    fields
                }
            ]
        })

        const isNestedParameter = (path) => {
            if (!path || typeof path !== 'string') return false
            if (path.startsWith("parameters.")) return true
            const firstPart = path.split('.')[0]
            return !!props.tool?.properties?.parameters?.properties?.[firstPart]
        }

        function getDefaultConfig(schema) {
            const config = {}
            const typeProp = schema.properties?.type
            if (typeProp) {
                config.type = typeProp.const !== undefined ? typeProp.const : typeProp.default
            }
            if (!config.type) {
                config.type = schema.$id
            }

            const nameProp = schema.properties?.name
            if (nameProp) {
                config.name = nameProp.const !== undefined ? nameProp.const : nameProp.default
            }

            const paramsSchema = schema.properties?.parameters
            if (paramsSchema && paramsSchema.properties) {
                config.parameters = {}
                Object.entries(paramsSchema.properties).forEach(([key, prop]) => {
                    if (prop.default !== undefined) {
                        config.parameters[key] = prop.default
                    }
                })
            } else {
                Object.entries(schema.properties || {}).forEach(([key, prop]) => {
                    if (key === 'type' || key === 'name') return
                    if (prop.default !== undefined) {
                        config[key] = prop.default
                    } else if (prop.const !== undefined) {
                        config[key] = prop.const
                    }
                })
            }
            return config
        }

        const getFieldSchema = (fieldPath) => {
            if (!fieldPath || typeof fieldPath !== 'string') return null
            let cleanPath = fieldPath
            let currentSchema = props.tool

            if (isNestedParameter(fieldPath)) {
                if (fieldPath.startsWith("parameters.")) {
                    cleanPath = fieldPath.substring("parameters.".length)
                }
                currentSchema = props.tool?.properties?.parameters
            }

            const parts = cleanPath.split('.')
            for (let i = 0; i < parts.length; i++) {
                const part = parts[i]
                if (!currentSchema) return null
                if (currentSchema.properties && currentSchema.properties[part]) {
                    currentSchema = currentSchema.properties[part]
                } else {
                    return null
                }
            }
            return currentSchema
        }

        const getParameterByPath = (path) => {
            if (!path || typeof path !== 'string') return undefined
            const current = serverTools.value[toolId.value]
            if (!current?.config) {
                return getDefaultFromSchema(path)
            }

            let cleanPath = path
            let ref = current.config

            if (isNestedParameter(path)) {
                if (path.startsWith("parameters.")) {
                    cleanPath = path.substring("parameters.".length)
                }
                ref = current.config.parameters
            }

            const parts = cleanPath.split('.')
            for (const part of parts) {
                if (ref === undefined || ref === null) return undefined
                ref = ref[part]
            }
            return ref !== undefined ? ref : getDefaultFromSchema(path)
        }

        const getDefaultFromSchema = (path) => {
            const schema = getFieldSchema(path)
            return schema?.default ?? schema?.const
        }

        const updateParameterByPath = (path, value) => {
            if (!path || typeof path !== 'string') return
            const current = serverTools.value[toolId.value] || { selected: false, config: getDefaultConfig(props.tool) }

            let cleanPath = path
            let ref = current.config

            if (isNestedParameter(path)) {
                if (path.startsWith("parameters.")) {
                    cleanPath = path.substring("parameters.".length)
                }
                if (!current.config.parameters) {
                    current.config.parameters = {}
                }
                ref = current.config.parameters
            }

            const parts = cleanPath.split('.')
            for (let i = 0; i < parts.length - 1; i++) {
                const part = parts[i]
                if (!ref[part] || typeof ref[part] !== 'object') {
                    ref[part] = {}
                }
                ref = ref[part]
            }

            const lastPart = parts[parts.length - 1]
            if (value === undefined) {
                delete ref[lastPart]
            } else {
                ref[lastPart] = value
            }

            ext.setPrefs({
                serverTools: {
                    ...serverTools.value,
                    [toolId.value]: JSON.parse(JSON.stringify(current))
                }
            })
        }

        const shouldShowField = (fieldSchema) => {
            if (!fieldSchema) return false
            if (!fieldSchema.ui?.showWhen) return true
            const { field, in: allowedValues } = fieldSchema.ui.showWhen
            if (!Array.isArray(allowedValues)) return true
            const val = getParameterByPath(field)
            return allowedValues.includes(val)
        }

        const isRequired = (fieldPath) => {
            if (!fieldPath || typeof fieldPath !== 'string') return false
            const parts = fieldPath.split('.')
            const lastPart = parts[parts.length - 1]
            if (isNestedParameter(fieldPath)) {
                return !!props.tool?.properties?.parameters?.required?.includes(lastPart)
            }
            return !!props.tool?.required?.includes(lastPart)
        }

        const isFullWidth = (schema) => {
            if (!schema) return false
            const w = schema.ui?.widget
            return w === 'textarea' || w === 'tool-list' || w === 'fieldset' || schema.type === 'array'
        }

        const getToolListEnum = (schema) => {
            return schema?.items?.properties?.type?.enum || []
        }

        const addTag = (path, tag) => {
            if (!tag || !tag.trim()) return
            const currentTags = getParameterByPath(path) || []
            if (!currentTags.includes(tag.trim())) {
                updateParameterByPath(path, [...currentTags, tag.trim()])
            }
        }

        const removeTag = (path, tag) => {
            const currentTags = getParameterByPath(path) || []
            updateParameterByPath(path, currentTags.filter(t => t !== tag))
        }

        const addLocalTag = (path) => {
            const val = localInputs.value[path]
            if (val && val.trim()) {
                addTag(path, val.trim())
                localInputs.value[path] = ""
            }
        }

        const toggleToolType = (path, toolType) => {
            const currentTools = getParameterByPath(path) || []
            const index = currentTools.findIndex(t => t.type === toolType)
            if (index > -1) {
                updateParameterByPath(path, currentTools.filter((_, i) => i !== index))
            } else {
                updateParameterByPath(path, [...currentTools, { type: toolType }])
            }
        }

        const hasToolType = (path, toolType) => {
            const currentTools = getParameterByPath(path) || []
            return currentTools.some(t => t.type === toolType)
        }

        const toggleRadio = (path, opt) => {
            const current = getParameterByPath(path)
            if (current === opt) {
                updateParameterByPath(path, undefined)
            } else {
                updateParameterByPath(path, opt)
            }
        }

        return {
            selected,
            isExpanded,
            toggleExpand,
            groups,
            localInputs,
            getFieldSchema,
            getParameterByPath,
            updateParameterByPath,
            shouldShowField,
            isRequired,
            isFullWidth,
            getToolListEnum,
            removeTag,
            addLocalTag,
            toggleToolType,
            hasToolType,
            toggleRadio
        }
    }
}
