/**
 * AutoQuerySchema - renders an AutoQuery Schema as a working CRUD UI: results grid with
 * column filters and sorting, paging, query preferences, and Create/Edit/Delete forms.
 *
 *     <AutoQuerySchema :auth="auth" :schema="schema" />
 *     <AutoQuerySchema :auth="auth">{ "name":"Booking", "model":{...}, "query":{...} }</AutoQuerySchema>
 *
 * The body form takes the schema as pasted JSON, so a server-rendered page can drop its
 * placeholder straight in: <AutoQuerySchema :auth="auth">${Schema}</AutoQuerySchema>. Use it when
 * the host has no other need for the schema; prefer :schema when it does (a title, say), or
 * the JSON ends up serialised into the page twice. Being pasted into markup, the JSON must
 * not contain `{{`, a backtick or ${ - the template compiler and JS template literals claim those.
 *
 * Deliberately renders no page chrome - no title, subtitle, back link or dark mode toggle -
 * so the host page owns its own layout. `requirementText()` and `subtitle()` are exported for
 * a host that wants to render the schema's auth requirements or description itself.
 *
 * Dependencies
 *   vue                     3.x
 *   vue-router              REQUIRED at runtime: all state (filters, sort, paging, which row is
 *                           being edited) lives in the query string, read through useRoute() and
 *                           written with useRouter(). The host must app.use(router) - any router
 *                           whose current route matches the page will do, e.g. a catch-all.
 *   @servicestack/vue       ModalDialog, ErrorSummary, ConfirmDelete and the
 *                           Text/Select/Checkbox/Textarea/Tag/File inputs, plus useAuth().
 *                           The host must app.use(ServiceStackVue) so these resolve.
 *   @servicestack/client    mapGet(), and JsonServiceClient if one isn't passed in.
 *   ./JsonSchemaForm.mjs    renders nested objects and arrays of objects
 *   ./SchemaResults.mjs     the results grid and everything that queries it
 *   ./SchemaLookup.mjs      picks [Ref] rows, fetching the referenced Model's schema
 *   ./useSchemas.mjs        fetches /auto/{Model}.json and /schema/{Request}.json on demand
 *
 * Needs NO App metadata: everything it renders comes from this schema, and any referenced
 * Model's schema is fetched on demand.
 *
 * Notes
 *   - Passing `auth` signs that session into useAuth(), which is app-wide state, so every
 *     component using canAccess() sees it. Pass null when signed out.
 *   - The schema is read once on mount. To switch models, re-mount with a different :key.
 *   - Query preferences persist to localStorage under `auto:prefs:{Model}`.
 */

import { ref, computed, watch, onMounted, provide, useSlots } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuth } from "@servicestack/vue"
import { JsonServiceClient, mapGet } from "@servicestack/client"
import JsonSchemaForm from "./JsonSchemaForm.mjs"
import SchemaResults, { resolvePath } from "./SchemaResults.mjs"
import SchemaLookup from "./SchemaLookup.mjs"

// Params the grid owns; everything else in the query string is an AutoQuery filter
const RESERVED = ['skip', 'edit', 'new', 'orderBy']

// --- schema helpers --------------------------------------------------------

const pathArgs = path => Array.from(path.matchAll(/\{(\w+)\}/g)).map(m => m[1])

const propsOf = schema => Object.entries(schema?.properties || {}).map(([name, prop]) => ({ name, prop }))

// A description only earns a subtitle when it says more than the title already does
const subtitle = schema => schema?.description !== schema?.title ? schema?.description : null

// The schema's auth block uses the same names as MetadataOperationType, so useAuth().canAccess() reads it as-is
const toOp = schema => schema ? { request: { name: schema.title }, ...(schema.auth || {}) } : null

const isEmpty = (value, prop = null) => {
    if (value === undefined || value === null || value === '') return true
    if (Array.isArray(value) && value.length === 0) return true
    const isNumberProp = prop?.type === 'integer' || prop?.type === 'number'
    if (isNumberProp || typeof value === 'number') {
        const num = Number(value)
        if (Number.isFinite(num) && num === 0) return true
    }
    return false
}

/**
 * Did the user change this field? Compares values as the input showed them, and calls
 * anything it can't compare confidently changed: re-sending an unchanged value is harmless,
 * dropping an edit isn't.
 */
function isDirty(value, original, prop) {
    if (value === original) return false
    if (value == null || original == null) return true
    // an untouched complex value is still the row's own reference, caught by === above.
    // Once edited it's a re-keyed copy, which can't be compared cheaply - so treat it as changed
    if (typeof value === 'object' || typeof original === 'object') return true
    // a date-time input only shows minutes, so compare what was actually on screen
    if (prop?.format === 'date-time') return String(value).slice(0, 16) !== String(original).slice(0, 16)
    return String(value) !== String(original)
}

/**
 * Everything needed to call an API, from its schema and the form's values: the resolved URL,
 * the method and the payload that goes on the wire. Split out of send() so a UI can show
 * exactly what it is about to send without a second copy of these rules to drift from.
 */
function buildRequest(schema, data, formEl, { original = null, primaryKey = null } = {}) {
    const method = schema.method || 'POST'
    const args = pathArgs(schema.$id)
    let url = resolvePath(schema.$id, data)

    // A Patch API only writes the fields it's sent, so it takes just what changed. An emptied
    // field would otherwise be silently ignored, so clearing one means naming it in `reset`,
    // which the server reads as a comma separated list (Keywords.reset) and nulls out.
    const isPatch = schema.operation === 'Patch' && original != null
    const reset = []

    const body = {}
    for (const name of Object.keys(schema.properties || {})) {
        if (args.includes(name)) continue // already in the route
        const value = mapGet(data, name)
        const was = isPatch ? mapGet(original, name) : undefined
        const prop = schema.properties[name]

        if (!isEmpty(value, prop)) {
            // the primary key identifies the row, so it goes even when it hasn't changed
            const send = !isPatch || name === primaryKey || isDirty(value, was, prop)
            if (send) body[name] = value
        } else if (isPatch && !isEmpty(was, prop)) {
            reset.push(name)
        }
    }
    if (reset.length) url += (url.includes('?') ? '&' : '?') + new URLSearchParams({ reset: reset.join(',') })

    const headers = { Accept: 'application/json' }
    const fileInputs = formEl && method !== 'GET' && method !== 'DELETE'
        ? [...formEl.querySelectorAll('input[type=file]')].filter(x => x.files?.length)
        : []
    let payload = null

    if (method === 'GET' || method === 'DELETE') {
        const qs = new URLSearchParams()
        for (const [k, v] of Object.entries(body)) qs.append(k, Array.isArray(v) ? v.join(',') : v)
        if ([...qs].length) url += (url.includes('?') ? '&' : '?') + qs
    } else if (fileInputs.length) {
        // Uploads have to go up as multipart, everything else is fine as JSON
        const formData = new FormData()
        const uploading = fileInputs.map(x => x.name)
        for (const [k, v] of Object.entries(body)) {
            // the picked file replaces the existing path, don't send both under the one name
            if (uploading.includes(k)) continue
            formData.append(k, Array.isArray(v) ? v.join(',') : v)
        }
        for (const el of fileInputs) {
            for (const file of el.files) formData.append(el.name, file)
        }
        payload = formData   // no Content-Type: the browser adds the multipart boundary
    } else {
        headers['Content-Type'] = 'application/json'
        payload = JSON.stringify(body)
    }

    return { method, url, headers, body, payload, uploads: fileInputs.map(x => x.name) }
}

/**
 * @param schema  the API schema to call
 * @param data    the edited values
 * @param formEl  the <form>, so any picked files can be sent as multipart
 * @param original    the row the form started from, for the Patch handling above
 * @param primaryKey  always sent on a Patch, since it's what identifies the row
 */
async function send(schema, data, formEl, opts = {}) {
    const { method, url, headers, payload } = buildRequest(schema, data, formEl, opts)
    const res = await fetch(url, { method, headers, body: payload ?? undefined })
    const text = await res.text()
    const json = text ? JSON.parse(text) : null
    if (!res.ok)
        throw mapGet(json || {}, 'responseStatus') || { message: `${res.status} ${res.statusText}`, errors: [] }
    return json
}

/** Re-key data to the property names the schema declares, matching case-insensitively */
function toSchemaCase(value, schema) {
    if (Array.isArray(value)) return value.map(x => toSchemaCase(x, schema?.items))
    if (!value || typeof value !== 'object' || !schema?.properties) return value

    const names = Object.keys(schema.properties)
    const to = {}
    for (const [key, item] of Object.entries(value)) {
        const name = names.find(x => x.toLowerCase() === key.toLowerCase()) ?? key
        to[name] = toSchemaCase(item, schema.properties[name])
    }
    return to
}

// --- access ----------------------------------------------------------------

const ACTIONS = { query: 'View', create: 'Create', update: 'Edit', delete: 'Delete', save: 'Save' }

// The auth block in plain language, e.g. "Employee role" / "you to be signed in"
function requirementText(auth) {
    const bits = []
    if (auth.requiredRoles) bits.push(plural(auth.requiredRoles, 'role'))
    if (auth.requiresAnyRole) bits.push('any ' + plural(auth.requiresAnyRole, 'role'))
    if (auth.requiredPermissions) bits.push(plural(auth.requiredPermissions, 'permission'))
    if (auth.requiresAnyPermission) bits.push('any ' + plural(auth.requiresAnyPermission, 'permission'))
    if (auth.requiredScopes) bits.push(plural(auth.requiredScopes, 'scope'))
    if (auth.requiresApiKey) bits.push('an API Key')
    if (!bits.length && auth.requiresAuth) bits.push('you to be signed in')
    return bits.join(' and ')
}
const plural = (names, noun) => names.join(', ') + ' ' + noun + (names.length > 1 ? 's' : '')

// --- inputs ----------------------------------------------------------------

// Maps a JSON Schema property to the ServiceStack Vue input that fits it.
// Each input takes the property name as its `id` so it binds its own
// ResponseStatus.Errors[].FieldName message without any wiring.
const SchemaInput = {
    components: { JsonSchemaForm, SchemaLookup },
    props: {
        name: String, prop: Object, schema: Object,
        status: { type: Object, default: null },
        spanClass: { type: String, default: null },
        model: { type: Object, default: null },   // whole form model, for LookupInput
        modelValue: { default: undefined },
    },
    emits: ['update:modelValue'],
    template: `
    <SchemaLookup v-if="lookup" :id="name" :class="span" :label="label" :help="help" :status="status"
                  :prop="prop" :model="model"
                  @update:model-value="$emit('update:modelValue', modelOf($event))" />

    <FileInput v-else-if="isFile" :id="name" :class="span" :label="label" :help="help" :status="status"
               :multiple="isMultiple" :files="isMultiple ? uploadedFiles : undefined"
               :model-value="isMultiple ? undefined : (typeof modelValue === 'string' ? modelValue : '')"
               :accept="accept" />

    <div v-else-if="isComplex" :class="span">
        <!-- rendered bare: an array root brings its own titled panel, an object's fields sit
             flush under the heading — either way the field adds no chrome of its own -->
        <JsonSchemaForm :schema="prop" :model-value="complexValue" :status="scopedStatus"
                        :show-title="prop.type !== 'array'" @change="$emit('update:modelValue', $event)" />
        <p v-if="help" class="mt-2 text-sm text-gray-500">{{ help }}</p>
    </div>

    <div v-else-if="prop.enum" :class="span">
        <SelectInput :id="name" :label="label" :status="status" :entries="entries"
                     :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" />
        <p v-if="help" class="mt-2 text-sm text-gray-500">{{ help }}</p>
    </div>

    <CheckboxInput v-else-if="prop.type === 'boolean'" :id="name" :class="span" :label="label" :help="help"
                   :status="status"
                   :model-value="!!modelValue" @update:model-value="$emit('update:modelValue', $event)" />

    <TextareaInput v-else-if="isTextarea" :id="name" :class="span" :label="label" :help="help"
                   :status="status" :placeholder="placeholder"
                   :model-value="textValue" @update:model-value="$emit('update:modelValue', $event)" />

    <TagInput v-else-if="prop.type === 'array'" :id="name" :class="span" :label="label" :help="help"
              :status="status"
              :model-value="modelValue ?? []" @update:model-value="$emit('update:modelValue', $event)" />

    <TextInput v-else :id="name" :class="span" :type="type" :label="label" :help="help"
               :status="status" :placeholder="placeholder" v-bind="attrs"
               :model-value="textValue" @update:model-value="$emit('update:modelValue', $event)" />`,
    setup(props) {
        const ui = computed(() => props.prop.ui || {})
        const required = computed(() => (props.schema?.required || []).includes(props.name))
        const isTextarea = computed(() => ui.value.widget === 'textarea' || props.prop.type === 'object')

        // SchemaLookup resolves the referenced row from the ref Model's own schema
        const lookup = computed(() => !!props.model && !!ui.value.ref)

        const isFile = computed(() => ui.value.widget === 'file')
        // the accepted extensions now travel with the property, from its [UploadTo] location

        // A nested object, or a list of them (e.g. Player.PhoneNumbers), is a whole
        // sub-form rather than a single input — JsonSchemaForm already renders those
        const isComplex = computed(() => {
            const p = props.prop
            if (p.type === 'object' && p.properties) return true
            return p.type === 'array' && p.items?.type === 'object' && !!p.items.properties
        })

        // JsonSchemaForm edits in place, so it needs one stable object to mutate. It's also
        // re-keyed to the schema's casing: the schema names properties as the C# DTO does
        // (Number) while the API sends camelCase (number), and without this the form renders
        // both — the schema's fields empty, and the data's as free-form additionalProperties.
        let cached = null, source
        const complexValue = computed(() => {
            const value = props.modelValue
            // rebuild only for a genuinely new source, not for the copy we emitted ourselves
            if (value !== source && value !== cached) {
                source = value
                cached = value != null
                    ? toSchemaCase(value, props.prop)
                    : (props.prop.type === 'array' ? [] : {})
            }
            return cached
        })

        // Its field paths are relative to the property, so re-root the server's
        // `PhoneNumbers[0].Number` as `[0].Number` before handing the errors down
        const scopedStatus = computed(() => {
            const prefix = props.name.toLowerCase()
            const errors = (props.status?.errors ?? [])
                .filter(e => String(e.fieldName ?? '').toLowerCase().startsWith(prefix))
                .map(e => ({ ...e, fieldName: e.fieldName.slice(props.name.length) }))
            return errors.length ? { errors } : null
        })
        // a file property that holds a collection (e.g. JobApplication.Attachments) takes many files
        const isMultiple = computed(() => props.prop.type === 'array')

        return {
            isTextarea, lookup, isFile, isMultiple, isComplex, complexValue, scopedStatus,
            accept: computed(() => ui.value.accept),
            // already the { fileName, filePath, contentType, contentLength } shape FileInput wants
            uploadedFiles: computed(() => Array.isArray(props.modelValue) ? props.modelValue : []),
            // LookupInput mutates the model in place and emits it, so pull our value back out
            modelOf: model => mapGet(model, props.name),
            label: computed(() => (props.prop.title || props.name) + (required.value ? ' *' : '')),
            help: computed(() => ui.value.help),
            placeholder: computed(() => ui.value.placeholder),
            span: computed(() => props.spanClass
                ?? (isComplex.value || isTextarea.value || ui.value.fieldCss?.includes('col-span-12')
                    ? 'col-span-12'
                    : 'col-span-12 sm:col-span-6 2xl:col-span-4')),
            // SelectInput has no empty option of its own, so optional enums need one to be unset
            entries: computed(() => (required.value ? [] : [{ key: '', value: '' }]).concat(
                (props.prop.enum || []).map(x => ({ key: x, value: ui.value.enumDescriptions?.[x] || x })))),
            type: computed(() => {
                if (props.prop.type === 'integer' || props.prop.type === 'number') return 'number'
                if (props.prop.format === 'date-time') return 'datetime-local'
                if (props.prop.format === 'email') return 'email'
                if (props.prop.format === 'uri') return 'url'
                if (ui.value.widget === 'password') return 'password'
                return 'text'
            }),
            attrs: computed(() => {
                const to = {}
                if (props.prop.minimum != null) to.min = props.prop.minimum
                if (props.prop.maximum != null) to.max = props.prop.maximum
                if (ui.value.step != null) to.step = ui.value.step
                if (props.prop.maxLength != null) to.maxlength = props.prop.maxLength
                if (props.prop.pattern) to.pattern = props.prop.pattern
                return to
            }),
            textValue: computed(() => {
                const v = props.modelValue
                if (v == null) return ''
                if (props.prop.type === 'object') return JSON.stringify(v, null, 2)
                if (props.prop.format === 'date-time') return String(v).slice(0, 16)
                return v
            }),
        }
    }
}

// --- component -------------------------------------------------------------

const template = `
<div>
    <SchemaResults ref="results" :schema="Auto" :query="query" @update:query="onQuery"
                   :take="take" :selectable="canOpenRow"
                   @row-selected="rowSelected" @loaded="rows = $event.results">
        <template #toolbar>
            <PrimaryButton v-if="canCreate" @click="openCreate">{{ Auto.create.ui?.submitLabel || 'New' }}</PrimaryButton>
        </template>
    </SchemaResults>

    <ModalDialog v-if="form" id="autoForm" size-class="sm:max-w-3xl 2xl:max-w-6xl sm:w-full" @done="closeForm">
        <form @submit.prevent="submitForm($event)">
            <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                <h3 class="text-base font-semibold">{{ form.schema.title }}</h3>
                <p v-if="formSubtitle" class="text-gray-500 dark:text-gray-400 mt-0.5" v-html="formSubtitle"></p>
            </div>

            <div class="px-6 py-5 max-h-[60vh] overflow-y-auto">
                <ErrorSummary v-if="form.error" :status="form.error" :except="boundFields" class="mb-4" />
                <div class="grid grid-cols-12 gap-4">
                    <SchemaInput v-for="f in formProps" :key="f.name" :name="f.name" :prop="f.prop"
                                 :schema="form.schema" :status="form.error" :model="form.data"
                                 v-model="form.data[f.name]" />
                </div>
            </div>

            <div class="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex items-center gap-2">
                <div v-if="form.key === 'update' && canDelete" class="flex items-center">
                    <ConfirmDelete @delete="deleteRow">{{ Auto.delete.ui?.submitLabel || 'Delete' }}</ConfirmDelete>
                </div>
                <span class="flex-1"></span>
                <SecondaryButton type="button" @click="closeForm">Cancel</SecondaryButton>
                <PrimaryButton v-if="canSubmit" type="submit" :disabled="loading">
                    {{ form.schema.ui?.submitLabel || 'Submit' }}
                </PrimaryButton>
            </div>
        </form>
    </ModalDialog>
</div>`

/** the schema may be given as a prop or pasted as the component's body */
function slotText(slots) {
    const out = []
    const walk = nodes => {
        for (const node of nodes ?? []) {
            if (typeof node.children === 'string') out.push(node.children)
            else if (Array.isArray(node.children)) walk(node.children)
        }
    }
    try {
        walk(slots.default?.())
    } catch {
        return ''
    }
    return out.join('').trim()
}

const AutoQuerySchema = {
    name: 'AutoQuerySchema',
    components: { SchemaInput, SchemaResults, JsonSchemaForm },
    template,
    props: {
        /** the AutoQuery Schema to render. Optional - falls back to parsing the component's body */
        schema: { type: Object, default: null },
        /** the current session, as an AuthenticateResponse. null when signed out */
        auth: { type: Object, default: null },
        /** an optional JsonServiceClient; one is created and provided if omitted */
        client: { type: Object, default: null },
        /** rows per page, until the user picks their own in Query Preferences */
        take: { type: Number, default: 25 },
    },
    setup(props) {
        const slots = useSlots()
        const Auto = props.schema ?? JSON.parse(slotText(slots) || '{}')
        const TAKE = props.take

        // provided for any descendant that injects a client
        const client = props.client ?? new JsonServiceClient()
        provide('client', client)

        // Signs the host's session into useAuth() so canAccess() can gate actions on the
        // first render, rather than after a round-trip
        if (props.auth) useAuth().signIn(props.auth)

        const { canAccess } = useAuth()

        const route = useRoute()
        const router = useRouter()

        // identifies a row in the URL, e.g. ?edit=1
        const pk = Auto.primaryKey || 'Id'

        const results = ref(null)   // the <SchemaResults> instance, for reload()
        const rows = ref([])        // the page it last loaded, to find the ?edit= row in
        const loading = ref(false)
        const form = ref(null)        // { key, schema, row, data, error }
        const canCreate = computed(() => canAccess(toOp(Auto.create)))
        const canUpdate = computed(() => canAccess(toOp(Auto.update)))
        const canDelete = computed(() => canAccess(toOp(Auto.delete)))
        // Rows open the Edit dialog, so only make them clickable when there's an update API
        const canOpenRow = computed(() => !!Auto.update)
        const canSubmit = computed(() => form.value?.key === 'create' ? canCreate.value : canUpdate.value)

        const formProps = computed(() => propsOf(form.value?.schema))

        // Fields render their own error, so the summary is only for errors none of them claimed.
        // A complex property claims everything under it, e.g. PhoneNumbers[0].Number.
        const boundFields = computed(() => {
            const names = Object.keys(form.value?.schema?.properties ?? {})
            return (form.value?.error?.errors ?? [])
                .map(e => e.fieldName)
                .filter(f => names.some(n => String(f ?? '').toLowerCase().startsWith(n.toLowerCase())))
        })


        // --- URL is the source of truth ------------------------------------

        const urlFilters = computed(() => Object.fromEntries(Object.entries(route.query)
            .filter(([k, v]) => !RESERVED.includes(k) && v != null && v !== '')))
        const orderBy = computed(() => String(route.query.orderBy || ''))
        const skip = computed(() => Math.max(0, parseInt(route.query.skip) || 0))
        const editKey = computed(() => route.query.edit)
        const isNew = computed(() => route.query.new != null)

        /**
         * SchemaResults keeps what's being queried in one object; here that object is the URL,
         * so every filter, sort and page is linkable and survives back/forward and a reload.
         */
        const query = computed(() => ({ filters: urlFilters.value, orderBy: orderBy.value, skip: skip.value }))
        function onQuery(value) {
            // a filter that's gone from the model has to be cleared from the URL, not just skipped
            const patch = Object.fromEntries(Object.keys(urlFilters.value).map(k => [k, undefined]))
            Object.assign(patch, value.filters)
            patch.orderBy = value.orderBy || undefined
            patch.skip = value.skip || undefined
            navigate(patch)
        }

        function navigate(patch) {
            const query = { ...route.query, ...patch }
            for (const k of Object.keys(query)) {
                if (query[k] == null || query[k] === '') delete query[k]
            }
            router.push({ query })
        }

        const openCreate = () => navigate({ new: 1, edit: undefined })
        const rowSelected = row => navigate({ edit: mapGet(row, pk), new: undefined })
        const closeForm = () => {
            if (editKey.value == null && !isNew.value) return  // already closed
            navigate({ edit: undefined, new: undefined })
        }

        // --- data ----------------------------------------------------------

        // On a full page reload the row for ?edit= may not be on this page of results
        async function fetchRow(key) {
            try {
                const qs = new URLSearchParams({ [pk]: key, take: 1 })
                const url = resolvePath(Auto.query.$id, {}) + '?' + qs
                const res = await fetch(url, { headers: { Accept: 'application/json' } })
                if (!res.ok) return null
                return (mapGet(await res.json(), 'results') || [])[0]
            } catch {
                return null
            }
        }

        function openForm(key, row) {
            const schema = Auto[key]
            if (!schema) return false
            const data = {}
            if (row) for (const name of Object.keys(schema.properties || {})) {
                const value = mapGet(row, name)
                if (value !== undefined && value !== null) data[name] = value
            }
            form.value = { key, schema, row, data, error: null }
            return true
        }

        // reflect ?new / ?edit= into the open dialog — covers back/forward and reloads
        async function syncForm() {
            if (isNew.value) {
                if (!openForm('create')) closeForm()
                return
            }
            if (editKey.value == null) { form.value = null; return }
            if (!Auto.update) { closeForm(); return }

            const row = rows.value.find(r => String(mapGet(r, pk)) === String(editKey.value))
                ?? await fetchRow(editKey.value)
            if (row) openForm('update', row)
            else closeForm()
        }

        async function submitForm(e) {
            loading.value = true
            try {
                await send(form.value.schema, form.value.data, e?.target,
                    { original: form.value.row, primaryKey: pk })
                closeForm()
                await results.value?.reload()
            } catch (e) {
                form.value.error = e
            } finally {
                loading.value = false
            }
        }

        async function deleteRow() {
            loading.value = true
            try {
                // the row carries the primary key the delete API routes on
                await send(Auto.delete, { ...form.value.row, ...form.value.data })
                closeForm()
                await results.value?.reload()
            } catch (e) {
                form.value.error = e
            } finally {
                loading.value = false
            }
        }

        watch(() => [editKey.value, isNew.value].join('|'), syncForm)
        // SchemaResults loads on mount; ?edit= is matched against the rows it reports
        watch(rows, syncForm)
        onMounted(syncForm)

        return {
            Auto, results, rows, loading, form, boundFields, formProps,
            query, onQuery, take: TAKE,
            canCreate, canUpdate, canDelete, canOpenRow, canSubmit,
            formSubtitle: computed(() => subtitle(form.value?.schema)),
            openCreate, rowSelected, closeForm, submitForm, deleteRow,
        }
    },
}

export default AutoQuerySchema
export { ACTIONS, requirementText, subtitle }
// reused by schema.html, which renders one API as a form to execute rather than a CRUD UI
export { SchemaInput, buildRequest, send, propsOf, toOp }
