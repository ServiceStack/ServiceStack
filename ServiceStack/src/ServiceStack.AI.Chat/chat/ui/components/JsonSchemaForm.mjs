/**
 * JsonSchemaForm - renders a JSON Schema as an editable form.
 *
 *     <JsonSchemaForm :schema="schema" v-model="data" :status="responseStatus" @change="save" />
 *     <JsonSchemaForm v-model="data">{ "type":"object", "properties":{ ... } }</JsonSchemaForm>
 *
 * Depends only on vue and @servicestack/client so it can move to @servicestack/vue as-is.
 * See README.md for the supported keywords, extensions and limits.
 */

import { computed, ref, watchEffect, provide, inject, useSlots } from 'vue'
import { humanize } from '@servicestack/client'

const CONTEXT = Symbol('JsonSchemaForm')

export const isPlainObject = v => v !== null && typeof v === 'object' && !Array.isArray(v)

// ---------------------------------------------------------------------------- schema resolution

const pointer = ref_ =>
    ref_
        .replace(/^#\//, '')
        .split('/')
        .map(p => decodeURIComponent(p.replace(/~1/g, '/').replace(/~0/g, '~')))

/** Follow same-document $refs. Recursive schemas resolve to the target without expanding forever. */
function deref(schema, root, seen) {
    if (!schema?.$ref) return schema
    if (seen.has(schema.$ref)) return { ...schema, $recursive: true, $ref: undefined }
    seen.add(schema.$ref)
    let target = root
    for (const part of pointer(schema.$ref)) target = target?.[part]
    if (!target) return schema
    const { $ref, ...rest } = schema
    return deref({ ...target, ...rest }, root, seen)
}

/** Flatten allOf into one schema so the form sees a single set of properties */
function mergeAllOf(schema, root) {
    if (!schema?.allOf?.length) return schema
    const { allOf, ...base } = schema
    return allOf.reduce((acc, part) => {
        const sub = resolveSchema(part, root)
        return {
            ...acc,
            ...sub,
            properties: { ...(acc.properties ?? {}), ...(sub.properties ?? {}) },
            required: [...new Set([...(acc.required ?? []), ...(sub.required ?? [])])],
        }
    }, base)
}

const resolved = new WeakMap()

/** $ref + allOf + OpenAPI `nullable` resolved into a plain schema, memoised per schema object */
export function resolveSchema(schema, root) {
    if (!schema || typeof schema !== 'object') return {}
    const rootSchema = root ?? schema
    let cache = resolved.get(rootSchema)
    if (!cache) resolved.set(rootSchema, (cache = new WeakMap()))
    if (cache.has(schema)) return cache.get(schema)

    let out = mergeAllOf(deref(schema, rootSchema, new Set()), rootSchema)
    if (out.nullable && !Array.isArray(out.type) && out.type) out = { ...out, type: [out.type, 'null'] }
    cache.set(schema, out)
    return out
}

/** `type` may be a union like ["string","null"] - the first non-null entry drives the widget */
export function typeOf(schema, value) {
    const declared = Array.isArray(schema?.type) ? schema.type.find(t => t !== 'null') : schema?.type
    if (declared) return declared
    if (schema?.properties || schema?.additionalProperties) return 'object'
    if (schema?.items || schema?.prefixItems) return 'array'
    if (schema?.const !== undefined) return typeof schema.const === 'number' ? 'number' : typeof schema.const
    if (Array.isArray(value)) return 'array'
    if (isPlainObject(value)) return 'object'
    if (typeof value === 'number') return 'number'
    if (typeof value === 'boolean') return 'boolean'
    return 'string'
}

export const isNullable = schema =>
    !!schema?.nullable || (Array.isArray(schema?.type) && schema.type.includes('null'))

/** [{ value, label }] when the schema constrains a field to a fixed set, else null */
export function choicesOf(schema) {
    if (schema?.enum) {
        const names = schema['x-enumNames'] ?? schema.enumNames
        return schema.enum.map((value, i) => ({ value, label: names?.[i] ?? String(value) }))
    }
    if (schema?.const !== undefined) return [{ value: schema.const, label: String(schema.const) }]
    const branches = schema?.oneOf ?? schema?.anyOf
    if (branches?.length && branches.every(b => b.const !== undefined)) {
        return branches.map(b => ({ value: b.const, label: b.title ?? String(b.const) }))
    }
    return null
}

/** oneOf/anyOf branches that need a variant picker (const-only branches are a select instead) */
export function variantsOf(schema) {
    const branches = schema?.oneOf ?? schema?.anyOf
    if (!branches?.length || branches.every(b => b.const !== undefined)) return null
    return branches
}

/** Which variant the current value looks most like - discriminator consts win, then required, then props */
export function bestVariant(branches, value, root) {
    let best = 0
    let bestScore = -1
    branches.forEach((branch, i) => {
        const s = resolveSchema(branch, root)
        let score = 0
        if (isPlainObject(value)) {
            for (const [key, prop] of Object.entries(s.properties ?? {})) {
                if (value[key] !== undefined) score += 1
                if (prop.const !== undefined && value[key] === prop.const) score += 10
            }
            for (const key of s.required ?? []) if (value[key] !== undefined) score += 2
        } else if (value !== undefined && typeOf(s) === typeOf({}, value)) {
            score += 1
        }
        if (score > bestScore) {
            bestScore = score
            best = i
        }
    })
    return best
}

/** A value matching the schema, for a new array item or a property the data is missing */
export function blankFor(schema, root, seen = new Set()) {
    const s = resolveSchema(schema, root)
    if (s.default !== undefined) return structuredClone(s.default)
    if (s.const !== undefined) return s.const
    const choices = choicesOf(s)
    if (choices) return choices[0].value
    const variants = variantsOf(s)
    if (variants) return blankFor(variants[0], root, seen)
    const type = typeOf(s)
    if (type === 'object' || type === 'array') {
        // only containers can cycle - leaf schemas are often shared between fields
        if (seen.has(s)) return null // recursive schema: stop here, the user can expand it
        seen.add(s)
    }
    switch (type) {
        case 'object': {
            const out = {}
            for (const [key, prop] of Object.entries(s.properties ?? {})) out[key] = blankFor(prop, root, seen)
            return out
        }
        case 'array':
            return (s.prefixItems ?? []).map(p => blankFor(p, root, seen))
        case 'integer':
        case 'number':
            return 0
        case 'boolean':
            return false
        case 'null':
            return null
        default:
            return ''
    }
}

// ---------------------------------------------------------------------------- widgets

const INPUT_TYPES = {
    date: 'date',
    'date-time': 'datetime-local',
    time: 'time',
    month: 'month',
    week: 'week',
    email: 'email',
    uri: 'url',
    url: 'url',
    password: 'password',
    color: 'color',
    tel: 'tel',
    search: 'search',
    uuid: 'text',
}

export function widgetOf(schema, value) {
    const forced = schema?.['x-widget']
    if (forced === 'hidden') return 'hidden'
    if (forced) return forced === 'select' || forced === 'radio' ? forced : forced
    if (choicesOf(schema)) return 'select'
    const type = typeOf(schema, value)
    if (type === 'object') return 'object'
    if (type === 'array') return 'array'
    if (type === 'boolean') return 'checkbox'
    if (type === 'integer' || type === 'number') return 'number'
    if (schema?.format === 'textarea') return 'textarea'
    if (INPUT_TYPES[schema?.format]) return 'input'
    if (typeof value === 'string' && (value.length > 80 || value.includes('\n'))) return 'textarea'
    return 'input'
}

// ---------------------------------------------------------------------------- errors

/** items[0].qty -> items.0.qty, so the different notations compare equal */
const normalizePath = path => String(path ?? '').replace(/\[(\d+)\]/g, '.$1').toLowerCase()

/**
 * First ResponseError for this path. An unqualified fieldName (`qty`) matches the leaf only when the
 * schema has exactly one field with that name, so ambiguous names never light up several inputs.
 */
export function fieldError(status, path, leafCounts) {
    const errors = status?.errors ?? status?.Errors
    if (!errors?.length || !path) return null
    const full = normalizePath(path)
    const leaf = full.split('.').pop()
    const match = errors.find(e => {
        const name = normalizePath(e.fieldName ?? e.FieldName)
        if (name === full) return true
        return !name.includes('.') && name === leaf && (leafCounts?.get(leaf) ?? 2) === 1
    })
    return match ? (match.message ?? match.Message ?? match.errorCode ?? match.ErrorCode) : null
}

/** How many fields in the schema share each leaf name, for the ambiguity check above */
export function leafNameCounts(schema, root, seen = new Set(), counts = new Map()) {
    const s = resolveSchema(schema, root ?? schema)
    if (!s.properties && !s.items && !s.prefixItems && !s.oneOf && !s.anyOf) return counts
    if (seen.has(s)) return counts // only containers recurse, so this only guards cycles
    seen.add(s)
    for (const [key, prop] of Object.entries(s.properties ?? {})) {
        const name = key.toLowerCase()
        counts.set(name, (counts.get(name) ?? 0) + 1)
        leafNameCounts(prop, root ?? schema, seen, counts)
    }
    for (const branch of s.oneOf ?? s.anyOf ?? []) leafNameCounts(branch, root ?? schema, seen, counts)
    for (const item of [s.items, ...(s.prefixItems ?? [])]) {
        if (item) leafNameCounts(item, root ?? schema, seen, counts)
    }
    return counts
}

const err = (fieldName, errorCode, message) => ({ fieldName, errorCode, message })
const isEmpty = v => v === undefined || v === null || v === '' || (Array.isArray(v) && v.length === 0)

/**
 * Pragmatic client-side validation of the keywords the form renders, returning ResponseStatus errors
 * whose fieldName matches the rendered field paths.
 */
export function validateValue(schema, value, root, path = '', label = '', out = []) {
    const s = resolveSchema(schema, root)
    const name = label || (path ? path.split('.').pop() : 'value')
    const type = typeOf(s, value)

    if (s.const !== undefined && value !== s.const) {
        out.push(err(path, 'Const', `${name} must be ${s.const}`))
    }
    const choices = choicesOf(s)
    if (choices && !isEmpty(value) && !choices.some(c => c.value === value)) {
        out.push(err(path, 'Enum', `${name} must be one of ${choices.map(c => c.label).join(', ')}`))
    }

    if (type === 'object' && isPlainObject(value)) {
        for (const key of s.required ?? []) {
            const prop = resolveSchema(s.properties?.[key], root)
            if (isEmpty(value[key])) {
                out.push(err(path ? `${path}.${key}` : key, 'NotEmpty', `${prop.title || humanize(key)} is required`))
            }
        }
        for (const [key, prop] of Object.entries(s.properties ?? {})) {
            if (value[key] !== undefined) {
                validateValue(prop, value[key], root, path ? `${path}.${key}` : key, resolveSchema(prop, root).title || humanize(key), out)
            }
        }
    } else if (type === 'array' && Array.isArray(value)) {
        if (s.minItems != null && value.length < s.minItems) out.push(err(path, 'MinItems', `${name} needs at least ${s.minItems}`))
        if (s.maxItems != null && value.length > s.maxItems) out.push(err(path, 'MaxItems', `${name} allows at most ${s.maxItems}`))
        if (s.uniqueItems && new Set(value.map(v => JSON.stringify(v))).size !== value.length) {
            out.push(err(path, 'UniqueItems', `${name} must not contain duplicates`))
        }
        value.forEach((item, i) => {
            const itemSchema = s.prefixItems?.[i] ?? s.items
            if (itemSchema) validateValue(itemSchema, item, root, `${path}[${i}]`, `${name} ${i + 1}`, out)
        })
    } else if (type === 'string' && typeof value === 'string' && value !== '') {
        if (s.minLength != null && value.length < s.minLength) out.push(err(path, 'MinLength', `${name} must be at least ${s.minLength} characters`))
        if (s.maxLength != null && value.length > s.maxLength) out.push(err(path, 'MaxLength', `${name} must be at most ${s.maxLength} characters`))
        if (s.pattern && !new RegExp(s.pattern).test(value)) out.push(err(path, 'Pattern', `${name} is not in the expected format`))
    } else if ((type === 'number' || type === 'integer') && typeof value === 'number') {
        if (type === 'integer' && !Number.isInteger(value)) out.push(err(path, 'Integer', `${name} must be a whole number`))
        if (s.minimum != null && value < s.minimum) out.push(err(path, 'Minimum', `${name} must be ${s.minimum} or more`))
        if (s.maximum != null && value > s.maximum) out.push(err(path, 'Maximum', `${name} must be ${s.maximum} or less`))
        if (s.exclusiveMinimum != null && value <= s.exclusiveMinimum) out.push(err(path, 'ExclusiveMinimum', `${name} must be greater than ${s.exclusiveMinimum}`))
        if (s.exclusiveMaximum != null && value >= s.exclusiveMaximum) out.push(err(path, 'ExclusiveMaximum', `${name} must be less than ${s.exclusiveMaximum}`))
        if (s.multipleOf && Math.abs(value / s.multipleOf - Math.round(value / s.multipleOf)) > 1e-9) {
            out.push(err(path, 'MultipleOf', `${name} must be a multiple of ${s.multipleOf}`))
        }
    }
    return out
}

// ---------------------------------------------------------------------------- styles

const INPUT_CLASS =
    'block w-full sm:text-sm rounded-md shadow-sm border-gray-300 dark:border-gray-600 dark:text-white ' +
    'dark:bg-gray-900 focus:border-indigo-500 focus:ring-indigo-500 disabled:bg-slate-50 dark:disabled:bg-slate-900 ' +
    'disabled:text-slate-500'
const INPUT_ERROR_CLASS =
    'block w-full sm:text-sm rounded-md shadow-sm border-red-500 text-red-900 dark:text-red-200 ' +
    'dark:bg-gray-900 focus:border-red-500 focus:ring-red-500'
const PANEL_CLASS = 'rounded-md border border-gray-200 dark:border-gray-700 overflow-hidden'
const PANEL_HEADER_CLASS = 'flex items-center gap-2 px-2 py-1.5 bg-gray-50 dark:bg-gray-800/50'
// only divides the header from content that's actually there - a collapsed panel would otherwise
// draw this on top of the panel's own bottom border
const PANEL_HEADER_BORDER_CLASS = 'border-b border-gray-200 dark:border-gray-700'
const SMALL_BTN_CLASS =
    'px-2 py-0.5 text-xs rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 ' +
    'hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-40'
const ICON_BTN_CLASS = 'p-1 rounded text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 disabled:opacity-40'

const CHEVRON =
    '<svg xmlns="http://www.w3.org/2000/svg" class="size-3 transition-transform flex-shrink-0" :class="{ \'-rotate-90\': !expanded }" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>'

// ---------------------------------------------------------------------------- node

const JsonSchemaNode = {
    name: 'JsonSchemaNode',
    template: `
    <div v-if="widget === 'hidden'"></div>

    <!-- array -->
    <div v-else-if="widget === 'array'" :class="panelClass" role="group" :aria-labelledby="id + '-label'">
        <div :class="[headerClass, expanded ? headerBorderClass : '']">
            <button type="button" @click="toggle" class="flex items-center gap-1.5 text-xs font-medium text-gray-600 dark:text-gray-300"
                :id="id + '-label'" :aria-expanded="expanded">
                ${CHEVRON}
                {{ heading }}<span v-if="required" class="text-red-500">*</span>
            </button>
            <span class="text-xs text-gray-500">{{ items.length }}</span>
            <span v-if="schema.deprecated" class="text-xs italic text-gray-400">deprecated</span>
            <div class="flex-1"></div>
            <button v-if="!readOnly && !tuple" type="button" @click="addItem" :disabled="atMax" :class="smallBtnClass"
                :title="atMax ? 'At most ' + schema.maxItems : 'Add'">+ Add</button>
        </div>
        <div v-if="expanded" class="p-2 space-y-2">
            <!-- fixed positions from prefixItems -->
            <div v-for="(entry, i) in tupleEntries" :key="'t' + i">
                <JsonSchemaNode :schema="entry" :model="items" :field="i" :path="path + '[' + i + ']'"
                    :label="itemLabel(i)" />
            </div>
            <div v-for="i in extraIndexes" :key="'i' + i" class="group flex items-start gap-2">
                <div class="flex-1 min-w-0">
                    <JsonSchemaNode :schema="schema.items" :model="items" :field="i" :path="path + '[' + i + ']'"
                        :label="itemLabel(i)" />
                </div>
                <div v-if="!readOnly" class="flex flex-col opacity-0 group-hover:opacity-100">
                    <button type="button" @click="move(i, -1)" :disabled="i === firstExtra" title="Move up" :class="iconBtnClass">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" /></svg>
                    </button>
                    <button type="button" @click="move(i, 1)" :disabled="i === items.length - 1" title="Move down" :class="iconBtnClass">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                    </button>
                    <button type="button" @click="removeItem(i)" :disabled="atMin" title="Remove"
                        class="p-1 rounded text-gray-400 hover:text-red-600 dark:hover:text-red-400 disabled:opacity-40">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                    </button>
                </div>
            </div>
            <div v-if="!items.length" class="px-1 py-2 text-xs italic text-gray-500">No entries yet</div>
            <p v-if="error" class="text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        </div>
    </div>

    <!-- multi select over an enum of values -->
    <div v-else-if="widget === 'checklist'">
        <span class="mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300">
            {{ heading }}<span v-if="required" class="text-red-500">*</span>
        </span>
        <div class="flex flex-wrap gap-x-4 gap-y-1">
            <label v-for="choice in itemChoices" :key="String(choice.value)" class="inline-flex items-center gap-2 text-xs text-gray-700 dark:text-gray-300">
                <input type="checkbox" :checked="items.includes(choice.value)" :disabled="readOnly"
                    @change="toggleChoice(choice.value, $event.target.checked)"
                    class="rounded border-gray-300 dark:border-gray-600 text-indigo-600 focus:ring-indigo-500" />
                {{ choice.label }}
            </label>
        </div>
        <p v-if="error" class="mt-1 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        <p v-else-if="schema.description" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ schema.description }}</p>
    </div>

    <!-- object -->
    <div v-else-if="widget === 'object'" :class="bare ? '' : panelClass" :role="bare ? null : 'group'"
        :aria-labelledby="bare ? null : id + '-label'">
        <!-- bare still needs the variant picker, which normally lives in the header -->
        <div v-if="bare && variants" class="mb-2 flex justify-end">
            <select :value="variant" @change="setVariant(Number($event.target.value))" :disabled="readOnly"
                class="text-xs rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-900 dark:text-white py-0.5">
                <option v-for="(v, i) in variantLabels" :key="i" :value="i">{{ v }}</option>
            </select>
        </div>
        <div v-if="heading && !bare" :class="[headerClass, expanded ? headerBorderClass : '']">
            <button type="button" @click="toggle" class="flex items-center gap-1.5 text-xs font-medium text-gray-600 dark:text-gray-300"
                :id="id + '-label'" :aria-expanded="expanded">
                ${CHEVRON}
                {{ heading }}<span v-if="required" class="text-red-500">*</span>
            </button>
            <span v-if="schema.deprecated" class="text-xs italic text-gray-400">deprecated</span>
            <div class="flex-1"></div>
            <select v-if="variants" :value="variant" @change="setVariant(Number($event.target.value))" :disabled="readOnly"
                class="text-xs rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-900 dark:text-white py-0.5">
                <option v-for="(v, i) in variantLabels" :key="i" :value="i">{{ v }}</option>
            </select>
        </div>
        <div v-if="expanded || bare" class="grid grid-cols-1 md:grid-cols-2 gap-2" :class="bare ? '' : 'p-2'">
            <div v-for="prop in properties" :key="prop.key" :class="{ 'md:col-span-2': prop.wide }">
                <div class="flex items-start gap-1">
                    <div class="flex-1 min-w-0">
                        <JsonSchemaNode :schema="prop.schema" :model="container" :field="prop.key"
                            :path="childPath(prop.key)" :label="prop.label" :required="isRequired(prop.key)" />
                    </div>
                    <button v-if="prop.removable && !readOnly" type="button" @click="removeKey(prop.key)" :title="'Remove ' + prop.key"
                        class="mt-4 p-1 rounded text-gray-400 hover:text-red-600 dark:hover:text-red-400">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                    </button>
                </div>
            </div>
            <div v-if="!properties.length && !allowsNewKeys" class="md:col-span-2 px-1 py-2 text-xs italic text-gray-500">No fields</div>
            <div v-if="allowsNewKeys && !readOnly" class="md:col-span-2 flex items-center gap-2">
                <input v-model="newKey" type="text" placeholder="New property" @keyup.enter="addKey"
                    class="px-2 py-1 text-xs rounded-md shadow-sm border-gray-300 dark:border-gray-600 dark:text-white dark:bg-gray-900" />
                <button type="button" @click="addKey" :disabled="!newKey.trim()" :class="smallBtnClass">Add</button>
            </div>
            <p v-if="error" class="md:col-span-2 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        </div>
    </div>

    <!-- checkbox -->
    <div v-else-if="widget === 'checkbox'">
        <label class="inline-flex items-center gap-2 text-xs font-medium text-gray-700 dark:text-gray-300">
            <input type="checkbox" :checked="!!value" :disabled="readOnly" @change="setValue($event.target.checked)"
                :aria-invalid="!!error" :aria-describedby="describedBy"
                class="rounded border-gray-300 dark:border-gray-600 text-indigo-600 focus:ring-indigo-500" />
            {{ heading }}<span v-if="required" class="text-red-500">*</span>
        </label>
        <p v-if="error" :id="id + '-err'" class="mt-1 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        <p v-else-if="schema.description" :id="id + '-help'" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ schema.description }}</p>
    </div>

    <!-- radio group -->
    <div v-else-if="widget === 'radio'">
        <span class="mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300">
            {{ heading }}<span v-if="required" class="text-red-500">*</span>
        </span>
        <div class="flex flex-wrap gap-x-4 gap-y-1">
            <label v-for="choice in choices" :key="String(choice.value)" class="inline-flex items-center gap-2 text-xs text-gray-700 dark:text-gray-300">
                <input type="radio" :name="id" :value="String(choice.value)" :checked="value === choice.value" :disabled="readOnly"
                    @change="setValue(choice.value)" class="border-gray-300 dark:border-gray-600 text-indigo-600 focus:ring-indigo-500" />
                {{ choice.label }}
            </label>
        </div>
        <p v-if="error" class="mt-1 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        <p v-else-if="schema.description" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ schema.description }}</p>
    </div>

    <!-- leaf -->
    <div v-else>
        <label v-if="heading" :for="id" class="mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300"
            :class="{ 'italic opacity-70': schema.deprecated }">
            {{ heading }}<span v-if="required" class="text-red-500">*</span>
        </label>

        <select v-if="widget === 'select'" :id="id" :value="String(value)" :disabled="readOnly || fixed"
            @change="setValue(coerce($event.target.value))" :aria-invalid="!!error" :aria-describedby="describedBy"
            :class="error ? errorClass : inputClass">
            <option v-if="nullable" :value="String(null)">(none)</option>
            <option v-for="choice in choices" :key="String(choice.value)" :value="String(choice.value)">{{ choice.label }}</option>
        </select>

        <input v-else-if="widget === 'number'" :id="id" type="number" :step="step" :min="schema.minimum ?? schema.exclusiveMinimum"
            :max="schema.maximum ?? schema.exclusiveMaximum" :value="value" :disabled="readOnly"
            @input="setValue($event.target.value === '' ? null : Number($event.target.value))"
            :aria-invalid="!!error" :aria-describedby="describedBy" :class="error ? errorClass : inputClass" />

        <textarea v-else-if="widget === 'textarea'" :id="id" :value="value ?? ''" :disabled="readOnly" rows="3" spellcheck="false"
            :maxlength="schema.maxLength" @input="setValue($event.target.value)"
            :aria-invalid="!!error" :aria-describedby="describedBy" :class="[error ? errorClass : inputClass, 'resize-y']"></textarea>

        <input v-else :id="id" :type="inputType" :value="value ?? ''" :disabled="readOnly"
            :placeholder="schema.examples?.[0] ?? schema.placeholder ?? ''"
            :minlength="schema.minLength" :maxlength="schema.maxLength" :pattern="schema.pattern"
            @input="setValue($event.target.value)" :aria-invalid="!!error" :aria-describedby="describedBy"
            :class="error ? errorClass : inputClass" />

        <p v-if="error" :id="id + '-err'" class="mt-1 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
        <p v-else-if="schema.description" :id="id + '-help'" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ schema.description }}</p>
    </div>`,
    props: {
        schema: { type: Object, default: () => ({}) },
        model: { type: [Object, Array], required: true },
        field: { type: [String, Number], required: true },
        path: { type: String, default: '' },
        label: { type: String, default: '' },
        required: { type: Boolean, default: false },
        /** render the object's fields without the surrounding panel, header and collapse toggle */
        bare: { type: Boolean, default: false },
    },
    setup(props) {
        const ctx = inject(CONTEXT)
        const root = ctx.root
        const newKey = ref('')

        const value = computed(() => props.model[props.field])
        const base = computed(() => resolveSchema(props.schema, root.value))
        const variants = computed(() => variantsOf(base.value))
        const variant = ref(0)
        const schema = computed(() => {
            if (!variants.value) return base.value
            const { oneOf, anyOf, ...rest } = base.value
            return { ...rest, ...resolveSchema(variants.value[variant.value], root.value) }
        })
        watchEffect(() => {
            if (variants.value) variant.value = bestVariant(variants.value, value.value, root.value)
        })

        const widget = computed(() => {
            const w = widgetOf(schema.value, value.value)
            if (w === 'array' && itemChoices.value && schema.value['x-widget'] !== 'list') return 'checklist'
            return w
        })
        const choices = computed(() => choicesOf(schema.value))
        const itemChoices = computed(() =>
            typeOf(schema.value) === 'array' && schema.value.items ? choicesOf(resolveSchema(schema.value.items, root.value)) : null,
        )
        const heading = computed(() => props.label || schema.value.title || '')
        const readOnly = computed(() => ctx.readOnly.value || !!schema.value.readOnly)
        const error = computed(() => fieldError(ctx.status.value, props.path, ctx.leafCounts.value))
        const id = computed(() => 'f-' + (normalizePath(props.path).replace(/\./g, '-') || 'root'))
        const missing = computed(() => value.value === undefined || value.value === null)
        const expanded = ref(!missing.value && !schema.value['x-collapsed'])

        // containers are only created in the data once opened, so rendering never dirties the model
        // and recursive schemas stop unfolding until the user asks for the next level
        watchEffect(() => {
            // a bare object has no toggle, so it's always materialised
            if (!expanded.value && !props.bare) return
            const current = props.model[props.field]
            if (widget.value === 'array' && !Array.isArray(current)) props.model[props.field] = blankFor(schema.value, root.value)
            else if (widget.value === 'object' && !isPlainObject(current)) props.model[props.field] = blankFor(schema.value, root.value)
        })

        const items = computed(() => (Array.isArray(value.value) ? value.value : []))
        const container = computed(() => (isPlainObject(value.value) ? value.value : {}))
        const tuple = computed(() => schema.value.prefixItems ?? (Array.isArray(schema.value.items) ? schema.value.items : null))
        const tupleEntries = computed(() => tuple.value ?? [])
        const firstExtra = computed(() => tupleEntries.value.length)
        const extraIndexes = computed(() =>
            items.value.map((_, i) => i).filter(i => i >= firstExtra.value),
        )
        const atMax = computed(() => schema.value.maxItems != null && items.value.length >= schema.value.maxItems)
        const atMin = computed(() => schema.value.minItems != null && items.value.length <= schema.value.minItems)

        const allowsNewKeys = computed(() => {
            const extra = schema.value.additionalProperties
            return extra === undefined ? !schema.value.properties : extra !== false
        })

        const properties = computed(() => {
            if (widget.value !== 'object') return []
            const declared = Object.entries(schema.value.properties ?? {})
            const extra = Object.keys(container.value)
                .filter(k => !schema.value.properties?.[k])
                .map(k => [k, isPlainObject(schema.value.additionalProperties) ? schema.value.additionalProperties : {}])
            return [...declared, ...extra]
                .map(([key, propSchema]) => {
                    const s = resolveSchema(propSchema, root.value)
                    const type = typeOf(s, container.value[key])
                    const declaredProp = !!schema.value.properties?.[key]
                    return {
                        key,
                        schema: propSchema,
                        order: s['x-order'] ?? 0,
                        // additionalProperties keys are data, not identifiers - show them verbatim
                        label: s.title || (declaredProp ? humanize(key) : key),
                        wide: type === 'object' || type === 'array' || s.format === 'textarea' || s['x-widget'] === 'textarea',
                        removable: !schema.value.properties?.[key],
                        hidden: s['x-widget'] === 'hidden',
                    }
                })
                .filter(p => !p.hidden)
                .sort((a, b) => a.order - b.order)
        })

        function setValue(v) {
            props.model[props.field] = v
            ctx.onChange()
        }
        function ensureArray() {
            if (!Array.isArray(props.model[props.field])) props.model[props.field] = []
            return props.model[props.field]
        }

        return {
            newKey, value, schema, widget, choices, itemChoices, heading, readOnly, error, id, expanded,
            items, container, properties, allowsNewKeys, tuple, tupleEntries, extraIndexes, firstExtra,
            atMax, atMin, variants,
            variant,
            variantLabels: computed(() =>
                (variants.value ?? []).map((b, i) => resolveSchema(b, root.value).title ?? `Option ${i + 1}`),
            ),
            nullable: computed(() => isNullable(schema.value)),
            fixed: computed(() => schema.value.const !== undefined),
            step: computed(() => (typeOf(schema.value) === 'integer' ? 1 : (schema.value.multipleOf ?? 'any'))),
            inputType: computed(() => INPUT_TYPES[schema.value.format] ?? 'text'),
            describedBy: computed(() => (error.value ? `${id.value}-err` : schema.value.description ? `${id.value}-help` : undefined)),
            panelClass: PANEL_CLASS,
            headerClass: PANEL_HEADER_CLASS,
            headerBorderClass: PANEL_HEADER_BORDER_CLASS,
            smallBtnClass: SMALL_BTN_CLASS,
            iconBtnClass: ICON_BTN_CLASS,
            inputClass: INPUT_CLASS,
            errorClass: INPUT_ERROR_CLASS,
            isRequired: key => (schema.value.required ?? []).includes(key),
            childPath: key => (props.path ? `${props.path}.${key}` : key),
            toggle: () => (expanded.value = !expanded.value),
            itemLabel(i) {
                const fixedEntry = tuple.value?.[i]
                if (fixedEntry) return resolveSchema(fixedEntry, root.value).title ?? `#${i + 1}`
                const itemSchema = resolveSchema(schema.value.items, root.value)
                const titleKey = itemSchema['x-titleKey']
                const item = items.value[i]
                if (titleKey && isPlainObject(item) && item[titleKey]) return `${i + 1}. ${item[titleKey]}`
                if (itemSchema.title) return `${itemSchema.title} ${i + 1}`
                return isPlainObject(item) ? `${props.label || 'Item'} ${i + 1}` : ''
            },
            setVariant(i) {
                variant.value = i
                const next = resolveSchema(variants.value[i], root.value)
                const keep = isPlainObject(value.value) ? value.value : {}
                const merged = blankFor(next, root.value)
                if (isPlainObject(merged)) {
                    for (const key of Object.keys(merged)) {
                        const fixedByBranch = resolveSchema(next.properties?.[key], root.value).const !== undefined
                        if (!fixedByBranch && keep[key] !== undefined) merged[key] = keep[key]
                    }
                }
                setValue(merged)
            },
            coerce(raw) {
                if (raw === String(null)) return null
                const match = choices.value?.find(c => String(c.value) === raw)
                return match ? match.value : raw
            },
            setValue,
            toggleChoice(choice, on) {
                const list = ensureArray()
                const i = list.indexOf(choice)
                if (on && i === -1) list.push(choice)
                else if (!on && i !== -1) list.splice(i, 1)
                ctx.onChange()
            },
            addItem() {
                ensureArray().push(blankFor(schema.value.items, root.value))
                expanded.value = true
                ctx.onChange()
            },
            removeItem(i) {
                props.model[props.field].splice(i, 1)
                ctx.onChange()
            },
            move(i, by) {
                const list = props.model[props.field]
                const to = i + by
                if (to < firstExtra.value || to >= list.length) return
                list.splice(to, 0, list.splice(i, 1)[0])
                ctx.onChange()
            },
            addKey() {
                const key = newKey.value.trim()
                if (!key) return
                const extra = schema.value.additionalProperties
                container.value[key] = isPlainObject(extra) ? blankFor(extra, root.value) : ''
                newKey.value = ''
                ctx.onChange()
            },
            removeKey(key) {
                delete container.value[key]
                ctx.onChange()
            },
        }
    },
}
JsonSchemaNode.components = { JsonSchemaNode }

// ---------------------------------------------------------------------------- root

export const JsonSchemaForm = {
    name: 'JsonSchemaForm',
    components: { JsonSchemaNode },
    template: `
    <div>
        <p v-if="schemaError" class="px-2 py-1.5 text-xs rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 text-red-800 dark:text-red-200">{{ schemaError }}</p>
        <template v-else>
            <!-- with a wrapper the panel header carries the title, so don't print it twice -->
            <h3 v-if="resolvedSchema.title && showTitle && !wrapper" class="mb-1 text-sm font-semibold text-gray-900 dark:text-gray-100">{{ resolvedSchema.title }}</h3>
            <p v-if="resolvedSchema.description && showTitle" class="mb-3 text-xs text-gray-500 dark:text-gray-400">{{ resolvedSchema.description }}</p>
            <p v-if="summary" class="mb-3 px-2 py-1.5 text-xs rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 text-red-800 dark:text-red-200">{{ summary }}</p>
            <JsonSchemaNode :schema="resolvedSchema" :model="rootModel" field="root" :path="''" :label="''" :bare="!wrapper" />
        </template>
    </div>`,
    props: {
        schema: { type: Object, default: null },
        modelValue: { default: undefined },
        /** alias for modelValue, for `:data` style usage */
        data: { default: undefined },
        status: { type: Object, default: null },
        readOnly: { type: Boolean, default: false },
        showTitle: { type: Boolean, default: true },
        /** wrap the whole form in the same collapsible panel nested objects get (off by default) */
        wrapper: { type: Boolean, default: false },
        validateOn: { type: String, default: 'submit' }, // 'submit' | 'change'
    },
    emits: ['update:modelValue', 'change'],
    setup(props, { emit, expose }) {
        const slots = useSlots()
        const schemaError = ref('')
        const clientStatus = ref(null)

        /** the schema may be given as a prop or pasted as the component's body */
        const resolvedSchema = computed(() => {
            if (props.schema) {
                schemaError.value = ''
                return props.schema
            }
            const text = slotText(slots)
            if (!text) {
                schemaError.value = 'No schema: pass :schema or put one in the component body'
                return {}
            }
            try {
                schemaError.value = ''
                return JSON.parse(text)
            } catch (e) {
                schemaError.value = `Schema isn't valid JSON: ${e.message}`
                return {}
            }
        })

        const value = computed(() => props.modelValue ?? props.data ?? {})
        const status = computed(() => props.status ?? clientStatus.value)
        const leafCounts = computed(() => leafNameCounts(resolvedSchema.value))

        function onChange() {
            if (props.validateOn === 'change') clientStatus.value = validate()
            else if (clientStatus.value) clientStatus.value = validate()
            emit('update:modelValue', value.value)
            emit('change', value.value)
        }

        function validate() {
            const errors = validateValue(resolvedSchema.value, value.value, resolvedSchema.value)
            if (!errors.length) return null
            return {
                errorCode: 'ValidationException',
                message: errors.length === 1 ? errors[0].message : `${errors.length} fields need attention`,
                errors,
            }
        }

        provide(CONTEXT, {
            root: resolvedSchema,
            status,
            leafCounts,
            readOnly: computed(() => props.readOnly),
            onChange,
        })

        expose({
            validate: () => (clientStatus.value = validate()),
            reset: () => (clientStatus.value = null),
        })

        return {
            resolvedSchema,
            schemaError,
            rootModel: computed(() => ({ root: value.value })),
            /** the status message, or any field error naming something this schema doesn't render */
            summary: computed(() => {
                const s = status.value
                if (!s) return null
                const errors = s.errors ?? s.Errors ?? []
                if (!errors.length) return s.message ?? s.errorCode ?? null
                const known = leafCounts.value
                const orphan = errors.find(e => {
                    const name = String(e.fieldName ?? e.FieldName ?? '').split(/[.[]/).pop().replace(']', '')
                    return name && !known.has(name.toLowerCase())
                })
                return orphan ? (orphan.message ?? orphan.errorCode) : null
            }),
        }
    },
}

/** Concatenate the text of the default slot, so a schema can be pasted into the component body */
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

export { JsonSchemaNode }
export default JsonSchemaForm
