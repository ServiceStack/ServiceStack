import { ref, reactive, computed, inject, watch, onMounted } from 'vue'
import { humanize } from '@servicestack/client'

const approvalCache = reactive({})
const approvalLoads = new Map()

function clone(value) {
    return value == null ? value : JSON.parse(JSON.stringify(value))
}

function normalizeSchemaValue(schema = {}, value) {
    if (schema.type === 'array' && Array.isArray(value))
        return value.map(x => normalizeSchemaValue(schema.items || {}, x))
    if (schema.type !== 'object' || !value || typeof value !== 'object' || Array.isArray(value))
        return clone(value)

    const properties = schema.properties || {}
    const names = Object.keys(properties)
    const to = {}
    for (const [key, childValue] of Object.entries(value)) {
        const canonicalName = names.find(name => name.toLowerCase() === key.toLowerCase()) || key
        to[canonicalName] = normalizeSchemaValue(properties[canonicalName] || {}, childValue)
    }
    return to
}

function initialValue(schema = {}) {
    if (schema.default !== undefined) return clone(schema.default)
    if (schema.type === 'object') return {}
    if (schema.type === 'array') return []
    if (schema.type === 'boolean') return false
    if (schema.type === 'integer' || schema.type === 'number') return null
    return ''
}

function hasValue(value) {
    return value !== undefined && value !== null && value !== ''
}

async function loadApprovals(ctx, threadId, force = false) {
    if (!threadId) return []
    if (!force && approvalCache[threadId]) return approvalCache[threadId]
    if (approvalLoads.has(threadId)) return approvalLoads.get(threadId)
    const task = (async () => {
        const api = await ctx.scope('api_tools').getJson(`/approvals/${threadId}`)
        if (api.error) throw api.error
        approvalCache[threadId] = api.response || []
        return approvalCache[threadId]
    })().finally(() => approvalLoads.delete(threadId))
    approvalLoads.set(threadId, task)
    return task
}

function replaceApproval(row) {
    const rows = approvalCache[row.threadId] || (approvalCache[row.threadId] = [])
    const index = rows.findIndex(x => x.id === row.id)
    if (index >= 0) rows[index] = row
    else rows.push(row)
}

function validate(schema, value, path = 'Arguments') {
    const errors = []
    if (!schema) return errors
    if (schema.enum && hasValue(value) && !schema.enum.includes(value))
        errors.push(`${path} must be one of: ${schema.enum.join(', ')}`)
    if (!hasValue(value)) return errors
    if (schema.type === 'object') {
        if (typeof value !== 'object' || Array.isArray(value)) return [`${path} must be an object`]
        for (const name of schema.required || []) {
            if (!hasValue(value[name])) errors.push(`${path}.${name} is required`)
        }
        for (const [name, child] of Object.entries(schema.properties || {}))
            errors.push(...validate(child, value[name], `${path}.${name}`))
    } else if (schema.type === 'array') {
        if (!Array.isArray(value)) return [`${path} must be an array`]
        value.forEach((item, i) => errors.push(...validate(schema.items || {}, item, `${path}[${i}]`)))
    } else if (schema.type === 'boolean' && typeof value !== 'boolean') {
        errors.push(`${path} must be true or false`)
    } else if (schema.type === 'integer' && !Number.isInteger(Number(value))) {
        errors.push(`${path} must be an integer`)
    } else if (schema.type === 'number' && Number.isNaN(Number(value))) {
        errors.push(`${path} must be a number`)
    }
    return errors
}

const SchemaField = {
    name: 'SchemaField',
    props: {
        schema: { type: Object, default: () => ({}) },
        modelValue: null,
        label: String,
        required: Boolean,
        nested: Boolean,
    },
    emits: ['update:modelValue'],
    template: `
        <div class="min-w-0">
            <label v-if="schema.type !== 'boolean'" class="block text-xs font-medium mb-1">
                {{ label }} <span v-if="required" class="text-red-500">*</span>
            </label>
            <p v-if="schema.description" class="text-[11px] mb-1.5" :class="$styles.muted">{{ schema.description }}</p>

            <select v-if="schema.enum" :value="modelValue ?? ''" @change="setValue($event.target.value)"
                class="w-full rounded-md border px-2.5 py-2 text-sm bg-transparent" :class="$styles.chromeBorder">
                <option value="">Select…</option>
                <option v-for="value in schema.enum" :key="value" :value="value">{{ value }}</option>
            </select>

            <label v-else-if="schema.type === 'boolean'" class="flex items-center gap-2 text-sm py-1">
                <input type="checkbox" :checked="modelValue === true" @change="setValue($event.target.checked)"
                    class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
                <span>{{ label }} <span v-if="required" class="text-red-500">*</span></span>
            </label>

            <input v-else-if="schema.type === 'integer' || schema.type === 'number'"
                :type="'number'" :step="schema.type === 'integer' ? '1' : 'any'" :value="modelValue ?? ''"
                @input="setNumber($event.target.value)"
                class="w-full rounded-md border px-2.5 py-2 text-sm bg-transparent" :class="$styles.chromeBorder" />

            <div v-else-if="schema.type === 'array'" class="space-y-2">
                <div v-for="(item, i) in arrayValue" :key="i" class="rounded-md border p-2" :class="$styles.chromeBorder">
                    <div class="flex items-start gap-2">
                        <div class="flex-1 min-w-0">
                            <SchemaField :schema="schema.items || {}" :model-value="item" :label="label + ' ' + (i + 1)"
                                nested @update:model-value="setArrayItem(i, $event)" />
                        </div>
                        <button type="button" @click="removeArrayItem(i)" title="Remove"
                            class="text-red-500 hover:text-red-700 px-1">×</button>
                    </div>
                </div>
                <button type="button" @click="addArrayItem"
                    class="text-xs rounded-md border px-2.5 py-1.5 hover:bg-gray-50 dark:hover:bg-gray-800" :class="$styles.chromeBorder">
                    Add {{ label }}
                </button>
            </div>

            <div v-else-if="schema.type === 'object' && schema.properties" class="rounded-md border p-3 space-y-3" :class="$styles.chromeBorder">
                <SchemaField v-for="(child, name) in schema.properties" :key="name" :schema="child"
                    :model-value="objectValue[name]" :label="humanize(name)"
                    :required="(schema.required || []).includes(name)" nested
                    @update:model-value="setObjectValue(name, $event)" />
            </div>

            <textarea v-else-if="schema.type === 'object'" rows="4" :value="jsonValue"
                @change="setJson($event.target.value)"
                class="w-full rounded-md border px-2.5 py-2 text-xs font-mono bg-transparent" :class="$styles.chromeBorder"></textarea>

            <textarea v-else-if="multiline" rows="3" :value="modelValue ?? ''" @input="setValue($event.target.value)"
                class="w-full rounded-md border px-2.5 py-2 text-sm bg-transparent" :class="$styles.chromeBorder"></textarea>

            <input v-else :type="inputType" :value="modelValue ?? ''" @input="setValue($event.target.value)"
                class="w-full rounded-md border px-2.5 py-2 text-sm bg-transparent" :class="$styles.chromeBorder" />
        </div>
    `,
    setup(props, { emit }) {
        const arrayValue = computed(() => Array.isArray(props.modelValue) ? props.modelValue : [])
        const objectValue = computed(() => props.modelValue && typeof props.modelValue === 'object' && !Array.isArray(props.modelValue)
            ? props.modelValue : {})
        const jsonValue = computed(() => JSON.stringify(objectValue.value, null, 2))
        const multiline = computed(() => /description|notes|content|body|message|prompt/i.test(props.label || ''))
        const inputType = computed(() => props.schema.format === 'date-time' ? 'datetime-local'
            : props.schema.format === 'date' ? 'date'
            : props.schema.format === 'uri' ? 'url'
            : 'text')
        const setValue = value => emit('update:modelValue', value)
        const setNumber = value => emit('update:modelValue', value === '' ? null : Number(value))
        const setObjectValue = (name, value) => emit('update:modelValue', { ...objectValue.value, [name]: value })
        const setArrayItem = (index, value) => {
            const to = [...arrayValue.value]
            to[index] = value
            emit('update:modelValue', to)
        }
        const addArrayItem = () => emit('update:modelValue', [...arrayValue.value, initialValue(props.schema.items || {})])
        const removeArrayItem = index => emit('update:modelValue', arrayValue.value.filter((_, i) => i !== index))
        const setJson = value => {
            try { emit('update:modelValue', JSON.parse(value || '{}')) } catch { /* keep last valid value */ }
        }
        return { arrayValue, objectValue, jsonValue, multiline, inputType, humanize, setValue, setNumber,
            setObjectValue, setArrayItem, addArrayItem, removeArrayItem, setJson }
    }
}

export const ApiApprovalForm = {
    components: { SchemaField },
    props: { approval: { type: Object, required: true } },
    emits: ['resolved'],
    template: `
        <form @submit.prevent="approve" class="p-3 space-y-4">
            <div class="rounded-md border px-3 py-2" :class="warningClass">
                <div class="flex items-center justify-between gap-3">
                    <div>
                        <div class="text-sm font-semibold">{{ approval.apiName }}</div>
                        <div v-if="showDescription" class="text-xs mt-0.5">{{ approval.description }}</div>
                    </div>
                    <span class="text-[10px] font-semibold uppercase tracking-wide">{{ approval.safety }}</span>
                </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                <SchemaField v-for="name in primaryFields" :key="name" :schema="properties[name]"
                    :model-value="args[name]" :label="humanize(name)" :required="required.includes(name)"
                    @update:model-value="setArg(name, $event)" />
            </div>

            <div v-if="optionalFields.length">
                <button type="button" @click="showOptional = !showOptional" class="text-xs font-medium" :class="$styles.link">
                    {{ showOptional ? 'Hide' : 'More' }} options ({{ optionalFields.length }})
                </button>
                <div v-if="showOptional" class="grid grid-cols-1 md:grid-cols-2 gap-3 mt-3">
                    <SchemaField v-for="name in optionalFields" :key="name" :schema="properties[name]"
                        :model-value="args[name]" :label="humanize(name)"
                        @update:model-value="setArg(name, $event)" />
                </div>
            </div>

            <div v-if="errors.length" class="rounded-md bg-red-50 dark:bg-red-950/20 text-red-700 dark:text-red-300 px-3 py-2 text-xs">
                <div v-for="error in errors" :key="error">{{ error }}</div>
            </div>
            <div class="flex justify-end gap-2 pt-1">
                <button type="button" @click="reject" :disabled="submitting"
                    class="rounded-md border px-3 py-2 text-sm disabled:opacity-50" :class="$styles.chromeBorder">Reject</button>
                <button type="submit" :disabled="submitting"
                    class="rounded-md px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
                    :class="approval.safety === 'destructive' ? 'bg-red-600 hover:bg-red-700' : 'bg-indigo-600 hover:bg-indigo-700'">
                    {{ submitting ? 'Executing…' : approval.safety === 'destructive' ? 'Confirm destructive action' : 'Confirm & execute' }}
                </button>
            </div>
        </form>
    `,
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const args = reactive(normalizeSchemaValue(props.approval.schema, props.approval.proposedArgs || {}))
        const showOptional = ref(false)
        const submitting = ref(false)
        const errors = ref([])
        const properties = computed(() => props.approval.schema?.properties || {})
        const required = computed(() => props.approval.schema?.required || [])
        const showDescription = computed(() => {
            const title = String(props.approval.apiName || '').trim().toLowerCase()
            const description = String(props.approval.description || '').trim()
            return description.length > 0 && description.toLowerCase() !== title
        })
        const primaryFields = computed(() => Object.keys(properties.value)
            .filter(name => required.value.includes(name) || hasValue(args[name])))
        const optionalFields = computed(() => Object.keys(properties.value).filter(name => !primaryFields.value.includes(name)))
        const warningClass = computed(() => props.approval.safety === 'destructive'
            ? 'border-red-300 dark:border-red-800 bg-red-50 dark:bg-red-950/20 text-red-800 dark:text-red-200'
            : 'border-amber-300 dark:border-amber-900/40 bg-amber-50 dark:bg-amber-950/20 text-amber-800 dark:text-amber-200')
        const setArg = (name, value) => { args[name] = value }
        const approve = async () => {
            errors.value = validate(props.approval.schema || {}, args)
            if (errors.value.length) return
            submitting.value = true
            try {
                const api = await ctx.scope('api_tools').postJson(`/approvals/${props.approval.id}/approve`, { args: clone(args) })
                if (api.error) throw api.error
                replaceApproval(api.response)
                emit('resolved', api.response)
            } catch (e) {
                errors.value = [e.message || String(e)]
            } finally {
                submitting.value = false
            }
        }
        const reject = async () => {
            submitting.value = true
            try {
                const api = await ctx.scope('api_tools').postJson(`/approvals/${props.approval.id}/reject`, {})
                if (api.error) throw api.error
                replaceApproval(api.response)
                emit('resolved', api.response)
            } catch (e) {
                errors.value = [e.message || String(e)]
            } finally {
                submitting.value = false
            }
        }
        return { args, properties, required, showDescription, primaryFields, optionalFields, showOptional, submitting,
            humanize,
            errors, warningClass, setArg, approve, reject }
    }
}

export const ApiToolCallBody = {
    components: { ApiApprovalForm },
    props: { thread: Object, tool: Object, output: Object },
    template: `
        <div v-if="loading" class="p-3 text-xs" :class="$styles.muted">Loading approval…</div>
        <ApiApprovalForm v-else-if="approval?.status === 'pending'" :approval="approval" @resolved="refresh" />
        <div v-else-if="approval?.status === 'executing'" class="p-4 text-sm">
            <div class="font-medium">Executing {{ approval.apiName }}…</div>
            <div class="text-xs mt-1" :class="$styles.muted">The operation has been claimed and cannot be submitted again.</div>
        </div>
        <div v-else-if="approval" class="p-3 space-y-3">
            <div class="flex items-center justify-between gap-3">
                <div class="font-medium text-sm">{{ approval.apiName }}</div>
                <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase" :class="statusClass">{{ statusLabel }}</span>
            </div>
            <div v-if="approval.reason || approval.error" class="text-xs" :class="approval.error ? 'text-red-600 dark:text-red-400' : $styles.muted">
                {{ approval.error || approval.reason }}
            </div>
            <details>
                <summary class="cursor-pointer text-xs font-medium" :class="[$styles.highlighted, $styles.linkHover]">Effective arguments</summary>
                <pre class="mt-2 overflow-auto rounded-md p-2 text-xs" :class="$styles.card" style="color:var(--tw-prose-pre-code)">{{ json(approval.effectiveArgs || approval.proposedArgs) }}</pre>
            </details>
            <details v-if="approval.result !== null && approval.result !== undefined">
                <summary class="cursor-pointer text-xs font-medium" :class="[$styles.highlighted, $styles.linkHover]">API response</summary>
                <pre class="mt-2 max-h-80 overflow-auto rounded-md p-2 text-xs" :class="$styles.card" style="color:var(--tw-prose-pre-code)">{{ json(approval.result) }}</pre>
            </details>
        </div>
        <div v-else class="p-3 space-y-2">
            <div class="text-xs font-medium">{{ callArgs.name || 'API call' }}</div>
            <pre class="overflow-auto rounded-md p-2 text-xs" :class="$styles.card" style="color:var(--tw-prose-pre-code)">{{ json(callArgs.args || {}) }}</pre>
            <pre v-if="output?.content" class="max-h-80 overflow-auto rounded-md p-2 text-xs" :class="$styles.card" style="color:var(--tw-prose-pre-code)">{{ output.content }}</pre>
        </div>
    `,
    setup(props) {
        const ctx = inject('ctx')
        const loading = ref(true)
        const approval = computed(() => (approvalCache[props.thread?.id] || [])
            .find(x => x.toolCallId === props.tool?.id))
        const callArgs = computed(() => ctx.utils.toJsonObject(props.tool?.function?.arguments) || {})
        const statusLabel = computed(() => approval.value?.status === 'completed' ? 'Approved'
            : approval.value?.status || '')
        const statusClass = computed(() => approval.value?.status === 'completed'
            ? 'bg-green-100 text-green-700 dark:bg-green-950/40 dark:text-green-300'
            : approval.value?.status === 'rejected' || approval.value?.status === 'canceled'
                ? 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300'
            : 'bg-red-100 text-red-700 dark:bg-red-950/20 dark:text-red-300')
        const refresh = async () => {
            try { await loadApprovals(ctx, props.thread?.id, true) }
            catch (e) { console.error('Failed to load API approvals', e) }
            finally { loading.value = false }
        }
        watch([() => props.thread?.status, () => props.output], refresh)
        onMounted(refresh)
        const json = value => typeof value === 'string' ? value : JSON.stringify(value, null, 2)
        return { loading, approval, callArgs, statusLabel, statusClass, refresh, json }
    }
}

export default {
    install(ctx) {
        ctx.components({ ApiToolCallBody })
        ctx.setToolCallBodies({
            api_call: {
                component: ApiToolCallBody,
                autoExpand: ({ thread, output }) => !output && thread?.status?.startsWith('Approval required'),
            }
        })
    }
}
