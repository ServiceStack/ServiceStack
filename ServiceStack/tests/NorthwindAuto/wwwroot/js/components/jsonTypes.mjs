/**
 * jsonTypes - generate typed classes from a JSON document or a JSON Schema.
 *
 *     generateTypes({ name:'invoice.json', json, language:'csharp' })
 *     generateTypes({ name:'invoice.json', json, schema, language:'csharp' })   // schema wins
 *
 * Deterministic, dependency free and instant - no model required. A JSON example only carries JSON's six
 * types, so passing the matching JSON Schema produces better output: `required` becomes non-nullable,
 * `multipleOf: 0.01` becomes `decimal`, `format` becomes date/uuid types, `enum` becomes a real enum and
 * `description` becomes doc comments.
 */

export const TYPE_LANGUAGES = [
    { id: 'csharp', label: 'C#', ext: '.cs' },
    { id: 'python', label: 'Python', ext: '.py' },
    { id: 'typescript', label: 'TS', ext: '.ts' },
    { id: 'javascript', label: 'JS', ext: '.js' },
]

// ---------------------------------------------------------------------------- naming

const RESERVED = {
    csharp: new Set('abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while'.split(' ')),
    python: new Set('False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield'.split(' ')),
    typescript: new Set('break case catch class const continue debugger default delete do else enum export extends false finally for function if import in instanceof new null return super switch this throw true try typeof var void while with'.split(' ')),
}
RESERVED.javascript = RESERVED.typescript

const words = s =>
    String(s)
        .replace(/(\p{Ll}|\p{N})(\p{Lu})/gu, '$1 $2')
        .split(/[^\p{L}\p{N}]+/u)
        .filter(Boolean)

export const pascal = s => unshadowDigit(words(s).map(w => w[0].toUpperCase() + w.slice(1)).join('')) || 'Value'
export const camel = s => {
    const p = pascal(s)
    return p[0].toLowerCase() + p.slice(1)
}
export const snake = s => unshadowDigit(words(s).map(w => w.toLowerCase()).join('_')) || 'value'

/** items -> Item, addresses -> Address, status -> Status */
export function singular(name) {
    if (/ies$/i.test(name) && name.length > 4) return name.slice(0, -3) + 'y'
    if (/(ss|us|is)$/i.test(name)) return name
    if (/(ches|shes|xes|zes|ses)$/i.test(name)) return name.slice(0, -2)
    if (/s$/i.test(name) && name.length > 2) return name.slice(0, -1)
    return name
}

const isIdentifier = s => /^[\p{L}_$][\p{L}\p{N}_$]*$/u.test(s)
/** '2fa' -> '_2fa': keeps the caller's name instead of falling back to a meaningless one */
const unshadowDigit = s => (/^\p{N}/u.test(s) ? '_' + s : s)

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/
const ISO_DATETIME = /^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?$/
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

// ---------------------------------------------------------------------------- model

const prim = (kind, extra = {}) => ({ kind, ...extra })
const ANY = prim('any')

/**
 * A language neutral model: a list of named types (children first) and the root type's name.
 * Structurally identical objects collapse into one type.
 */
class ModelBuilder {
    constructor() {
        this.types = []
        this.byShape = new Map()
        this.byRef = new Map()
        this.names = new Set()
    }

    /** claim a name before its fields are built, so a recursive $ref can point back at it */
    reserve(preferred, isRoot = false) {
        const name = this.uniqueName(preferred, '', isRoot)
        this.names.add(name)
        return name
    }

    pushObject(name, fields) {
        this.types.push({ kind: 'object', name, fields })
        return prim('ref', { name })
    }

    uniqueName(preferred, shapeKey, isRoot = false) {
        let name = pascal(preferred) || 'Type'
        // the root keeps the name derived from the file - nested types give way to it
        if (!isRoot && name === this.rootName) name += 'Info'
        if (!this.names.has(name)) return name
        // same name, different shape -> suffix rather than silently merging
        for (let i = 2; i < 100; i++) {
            const candidate = `${name}${i}`
            if (!this.names.has(candidate)) return candidate
        }
        return `${name}_${shapeKey.length}`
    }

    addObject(fields, preferredName, isRoot = false) {
        const shapeKey = JSON.stringify(fields.map(f => [f.key, typeKey(f.type), f.optional]))
        const existing = this.byShape.get(shapeKey)
        if (existing) return prim('ref', { name: existing })

        const name = this.uniqueName(preferredName, shapeKey, isRoot)
        this.names.add(name)
        this.byShape.set(shapeKey, name)
        this.types.push({ kind: 'object', name, fields })
        return prim('ref', { name })
    }

    addEnum(values, preferredName, description) {
        const shapeKey = 'enum:' + JSON.stringify(values)
        const existing = this.byShape.get(shapeKey)
        if (existing) return prim('ref', { name: existing })
        const name = this.uniqueName(preferredName, shapeKey)
        this.names.add(name)
        this.byShape.set(shapeKey, name)
        this.types.push({ kind: 'enum', name, values, description })
        return prim('ref', { name })
    }
}

const typeKey = t =>
    t.kind === 'array' ? `[${typeKey(t.of)}]` : t.kind === 'map' ? `{${typeKey(t.of)}}` : t.kind === 'ref' ? t.name : t.kind

// ---------------------------------------------------------------------------- inference: JSON Schema

const schemaTypeOf = s => {
    const t = Array.isArray(s.type) ? s.type.find(x => x !== 'null') : s.type
    if (t) return t
    if (s.properties || s.additionalProperties) return 'object'
    if (s.items || s.prefixItems) return 'array'
    if (s.enum) return typeof s.enum[0] === 'number' ? 'number' : typeof s.enum[0]
    if (s.const !== undefined) return typeof s.const
    return 'string'
}

const schemaNullable = s => !!s.nullable || (Array.isArray(s.type) && s.type.includes('null'))

function derefSchema(schema, root, seen = new Set()) {
    if (!schema?.$ref || seen.has(schema.$ref)) return schema
    seen.add(schema.$ref)
    let target = root
    for (const part of schema.$ref.replace(/^#\//, '').split('/')) {
        target = target?.[decodeURIComponent(part.replace(/~1/g, '/').replace(/~0/g, '~'))]
    }
    if (!target) return schema
    const { $ref, ...rest } = schema
    return derefSchema({ ...target, ...rest }, root, seen)
}

function mergeAllOf(schema, root) {
    if (!schema?.allOf?.length) return schema
    const { allOf, ...base } = schema
    return allOf.reduce((acc, part) => {
        const sub = resolve(part, root)
        return {
            ...acc,
            ...sub,
            properties: { ...(acc.properties ?? {}), ...(sub.properties ?? {}) },
            required: [...new Set([...(acc.required ?? []), ...(sub.required ?? [])])],
        }
    }, base)
}

const resolve = (schema, root) => mergeAllOf(derefSchema(schema ?? {}, root), root)

/** Every value is a name we could safely use as an enum member */
const enumerable = values =>
    values.length > 0 && values.every(v => typeof v === 'string' && /^[A-Za-z][A-Za-z0-9 _-]*$/.test(v))

function fromSchema(schema, root, model, name, stack = new Set(), isRoot = false, sample = undefined) {
    const s = resolve(schema, root)
    const nullable = schemaNullable(s)
    const typeName = s.title ? pascal(s.title) : pascal(name)

    if (s.enum && enumerable(s.enum)) {
        return { ...model.addEnum(s.enum, typeName, s.description), nullable }
    }
    if (s.enum || s.const !== undefined) {
        return { ...scalarFromSchema(s), nullable }
    }

    switch (schemaTypeOf(s)) {
        case 'object': {
            if (s.properties) {
                // resolve() returns a fresh object each call, so recursion is tracked by $ref, not identity
                const ref = schema?.$ref
                if (ref && model.byRef.has(ref)) return { kind: 'ref', name: model.byRef.get(ref), nullable }

                // a $ref'd schema is a named type: claim its name up front so it can reference itself
                const declared = ref ? model.reserve(s.title ?? ref.split('/').pop(), isRoot) : null
                if (ref) model.byRef.set(ref, declared)

                const required = new Set(s.required ?? [])
                const fields = Object.entries(s.properties).map(([key, prop]) => {
                    const p = resolve(prop, root)
                    return {
                        key,
                        description: p.description,
                        optional: !required.has(key),
                        deprecated: !!p.deprecated,
                        type: fromSchema(prop, root, model, singular(pascal(key)), stack, false, sampleAt(sample, key)),
                    }
                })
                if (declared) return { ...model.pushObject(declared, fields), nullable }
                return { ...model.addObject(fields, typeName, isRoot), nullable }
            }
            const extra = s.additionalProperties
            const of = extra && typeof extra === 'object'
                ? fromSchema(extra, root, model, `${typeName}Value`, stack, false, Object.values(sample ?? {})[0])
                : ANY
            return { kind: 'map', of, nullable }
        }
        case 'array': {
            if (s.prefixItems?.length) {
                return {
                    kind: 'tuple',
                    of: s.prefixItems.map((p, i) => fromSchema(p, root, model, `${typeName}${i + 1}`, stack, false, sampleAt(sample, i))),
                    nullable,
                }
            }
            const of = s.items ? fromSchema(s.items, root, model, singular(typeName), stack, false, sampleAt(sample, 0)) : ANY
            return { kind: 'array', of, nullable }
        }
        default:
            return { ...scalarFromSchema(s, sample), nullable }
    }
}

const sampleAt = (sample, key) => (sample == null ? undefined : sample[key])

/**
 * A schema is only a claim about the data - when an example disagrees with a string `format`, believe the
 * example, so the generated types can still parse the document sitting next to them.
 */
const formatFits = (format, sample) => {
    if (typeof sample !== 'string') return true
    if (format === 'date') return ISO_DATE.test(sample) || ISO_DATETIME.test(sample)
    if (format === 'date-time') return ISO_DATETIME.test(sample)
    if (format === 'uuid') return UUID.test(sample)
    return true
}

function scalarFromSchema(s, sample) {
    const type = schemaTypeOf(s)
    if (type === 'boolean') return prim('boolean')
    if (type === 'integer') {
        return prim(s.maximum > 2147483647 || s.minimum < -2147483648 ? 'long' : 'integer')
    }
    if (type === 'number') {
        // a fractional multipleOf is the schema telling us this is a money-like decimal
        const fractional = s.multipleOf != null && s.multipleOf < 1
        return prim(fractional ? 'decimal' : 'double')
    }
    if (type === 'null') return prim('any')
    switch (formatFits(s.format, sample) ? s.format : undefined) {
        case 'date':
            return prim('date')
        case 'date-time':
            return prim('datetime')
        case 'uuid':
            return prim('uuid')
        default:
            return prim('string')
    }
}

// ---------------------------------------------------------------------------- inference: JSON example

/** Merge the shapes of every element so an array of objects yields one type with optional fields */
function mergeShapes(values) {
    const keys = new Map()
    for (const value of values) {
        for (const [key, v] of Object.entries(value)) {
            if (!keys.has(key)) keys.set(key, { values: [], count: 0 })
            const entry = keys.get(key)
            entry.values.push(v)
            entry.count++
        }
    }
    return { keys, total: values.length }
}

function fromJson(value, model, name, depth = 0, isRoot = false) {
    if (value === null || value === undefined) return { ...ANY, nullable: true }
    if (Array.isArray(value)) {
        if (!value.length) return { kind: 'array', of: ANY }
        const objects = value.filter(v => v && typeof v === 'object' && !Array.isArray(v))
        if (objects.length === value.length) {
            const { keys, total } = mergeShapes(objects)
            const fields = [...keys.entries()].map(([key, entry]) => ({
                key,
                optional: entry.count < total || entry.values.some(v => v === null),
                type: fromJsonMany(entry.values, model, singular(pascal(key)), depth + 1),
            }))
            return { kind: 'array', of: model.addObject(fields, singular(pascal(name))) }
        }
        return { kind: 'array', of: fromJsonMany(value, model, singular(pascal(name)), depth + 1) }
    }
    if (typeof value === 'object') {
        const fields = Object.entries(value).map(([key, v]) => ({
            key,
            optional: v === null,
            type: fromJson(v, model, singular(pascal(key)), depth + 1),
        }))
        return model.addObject(fields, pascal(name), isRoot)
    }
    if (typeof value === 'boolean') return prim('boolean')
    if (typeof value === 'number') {
        if (!Number.isInteger(value)) return prim('double')
        return prim(value > 2147483647 || value < -2147483648 ? 'long' : 'integer')
    }
    if (typeof value === 'string') {
        if (UUID.test(value)) return prim('uuid')
        if (ISO_DATETIME.test(value)) return prim('datetime')
        if (ISO_DATE.test(value)) return prim('date')
    }
    return prim('string')
}

/** The common type of several values, widening where they disagree */
function fromJsonMany(values, model, name, depth) {
    const present = values.filter(v => v !== null && v !== undefined)
    if (!present.length) return { ...ANY, nullable: true }
    const types = present.map(v => fromJson(v, model, name, depth))
    const first = types[0]
    const allSame = types.every(t => typeKey(t) === typeKey(first))
    if (allSame) return { ...first, nullable: present.length < values.length }
    // int + double -> double, anything else -> any
    if (types.every(t => t.kind === 'integer' || t.kind === 'double')) return prim('double')
    return { ...ANY, nullable: true }
}

// ---------------------------------------------------------------------------- build

const looksLikeSchema = v =>
    !!v && typeof v === 'object' && !Array.isArray(v) &&
    (v.$schema !== undefined || v.properties !== undefined || (v.type !== undefined && typeof v.type === 'string'))

export function buildModel({ name = 'data.json', json, schema } = {}) {
    const model = new ModelBuilder()
    const rootName = pascal(String(name).replace(/\.ui\.json$/, '').replace(/\.[^.]+$/, '') || 'Root')

    model.rootName = rootName
    let root
    if (schema && looksLikeSchema(schema)) {
        root = fromSchema(schema, schema, model, schema.title ? pascal(schema.title) : rootName, new Set(), true, json)
    } else {
        root = fromJson(json, model, rootName, 0, true)
    }
    // an unnamed root (array or scalar) still needs a home
    if (root.kind !== 'ref') {
        model.types.push({ kind: 'alias', name: model.uniqueName(rootName, 'alias'), type: root })
    }
    return { types: model.types, root }
}

// ---------------------------------------------------------------------------- emitters

/** Every type kind the model actually uses, so emitters import exactly what they need */
function kindsUsed(types) {
    const kinds = new Set()
    const visit = t => {
        if (!t) return
        kinds.add(t.kind)
        if (t.of) (Array.isArray(t.of) ? t.of : [t.of]).forEach(visit)
    }
    for (const type of types) {
        if (type.kind === 'object') type.fields.forEach(f => visit(f.type))
        else if (type.kind === 'alias') visit(type.type)
        else kinds.add(type.kind)
    }
    return kinds
}

const safe = (name, language, fallback) => {
    let out = name
    if (!isIdentifier(out)) out = fallback
    if (RESERVED[language]?.has(out)) out = language === 'csharp' ? '@' + out : out + '_'
    return out || fallback
}

const doc = (text, indent, prefix) =>
    text ? text.split('\n').map(line => `${indent}${prefix} ${line}`).join('\n') + '\n' : ''

// ---- C#

const CS_TYPES = {
    string: 'string', integer: 'int', long: 'long', double: 'double', decimal: 'decimal',
    boolean: 'bool', date: 'DateTime', datetime: 'DateTime', uuid: 'Guid', any: 'object',
}

function csType(t) {
    const base =
        t.kind === 'array' ? `List<${csType(t.of)}>`
        : t.kind === 'map' ? `Dictionary<string, ${csType(t.of)}>`
        : t.kind === 'tuple' ? `(${t.of.map(csType).join(', ')})`
        : t.kind === 'ref' ? t.name
        : CS_TYPES[t.kind] ?? 'object'
    return base
}
const CS_VALUE_KINDS = ['integer', 'long', 'double', 'decimal', 'boolean', 'date', 'datetime', 'uuid']
/** enums are value types too, so they must never be initialised with `= null!` */
const csValueType = (t, model) =>
    CS_VALUE_KINDS.includes(t.kind) ||
    t.kind === 'tuple' ||
    (t.kind === 'ref' && model?.types.some(x => x.name === t.name && x.kind === 'enum'))

function emitCSharp(model) {
    const out = []
    const kinds = kindsUsed(model.types)
    const uses = (...names) => names.some(n => kinds.has(n))
    // DateTime/Guid live in System, which isn't implicit in every project
    if (uses('date', 'datetime', 'uuid')) out.push('using System;')
    if (uses('array', 'map')) out.push('using System.Collections.Generic;')
    out.push('using System.Text.Json.Serialization;', '')

    for (const type of model.types) {
        if (type.kind === 'enum') {
            out.push(doc(type.description, '', '///').trimEnd())
            out.push(`public enum ${type.name}`, '{')
            out.push(type.values.map(v => `    ${safe(pascal(v), 'csharp', 'Value')},`).join('\n'))
            out.push('}', '')
            continue
        }
        if (type.kind === 'alias') {
            out.push(`// root: ${csType(type.type)}`, '')
            continue
        }
        out.push(`public class ${type.name}`, '{')
        type.fields.forEach((field, i) => {
            if (i) out.push('')
            out.push(doc(field.description, '    ', '///').trimEnd() || null)
            if (field.deprecated) out.push('    [System.Obsolete]')
            out.push(`    [JsonPropertyName("${field.key}")]`)
            let name = safe(pascal(field.key), 'csharp', 'Value')
            if (name === type.name) name += 'Value' // a member can't share its enclosing type's name
            const optional = field.optional || field.type.nullable
            const cs = csType(field.type)
            const nullable = optional ? `${cs}?` : cs
            const init = optional || csValueType(field.type, model) ? '' : field.type.kind === 'array' || field.type.kind === 'map' ? ' = new();' : ' = null!;'
            out.push(`    public ${nullable} ${name} { get; set; }${init}`)
        })
        out.push('}', '')
    }
    return out.filter(l => l !== null).join('\n').replace(/\n{3,}/g, '\n\n').trim() + '\n'
}

// ---- Python

const PY_TYPES = {
    string: 'str', integer: 'int', long: 'int', double: 'float', decimal: 'Decimal',
    boolean: 'bool', date: 'date', datetime: 'datetime', uuid: 'UUID', any: 'Any',
}

function pyType(t) {
    const base =
        t.kind === 'array' ? `List[${pyType(t.of)}]`
        : t.kind === 'map' ? `Dict[str, ${pyType(t.of)}]`
        : t.kind === 'tuple' ? `Tuple[${t.of.map(pyType).join(', ')}]`
        : t.kind === 'ref' ? t.name
        : PY_TYPES[t.kind] ?? 'Any'
    return base
}

function emitPython(model) {
    const body = []
    const typing = new Set()
    const imports = new Set()
    const kinds = kindsUsed(model.types)
    if (kinds.has('array')) typing.add('List')
    if (kinds.has('map')) typing.add('Dict')
    if (kinds.has('tuple')) typing.add('Tuple')
    if (kinds.has('any')) typing.add('Any')
    if (kinds.has('decimal')) imports.add('from decimal import Decimal')
    if (kinds.has('datetime')) imports.add('from datetime import datetime')
    if (kinds.has('date')) imports.add('from datetime import date')
    if (kinds.has('uuid')) imports.add('from uuid import UUID')

    let usesEnum = false
    let usesField = false

    for (const type of model.types) {
        if (type.kind === 'alias') {
            body.push(`# root: ${pyType(type.type)}`, '')
            continue
        }
        if (type.kind === 'enum') {
            usesEnum = true
            body.push(`class ${type.name}(str, Enum):`)
            if (type.description) body.push(`    """${type.description}"""`)
            type.values.forEach(v => body.push(`    ${safe(snake(v).toUpperCase(), 'python', 'VALUE')} = ${JSON.stringify(v)}`))
            body.push('', '')
            continue
        }
        body.push('@dataclass_json', '@dataclass', `class ${type.name}:`)
        // fields with defaults must come last
        const fields = [...type.fields].sort((a, b) => Number(a.optional) - Number(b.optional))
        if (!fields.length) body.push('    pass')
        fields.forEach(field => {
            const name = safe(snake(field.key), 'python', 'value')
            const optional = field.optional || field.type.nullable
            if (optional) typing.add('Optional')
            const annotation = optional ? `Optional[${pyType(field.type)}]` : pyType(field.type)
            const metadata = name !== field.key ? `metadata=config(field_name=${JSON.stringify(field.key)})` : null
            let suffix = ''
            if (metadata && optional) {
                usesField = true
                suffix = ` = field(default=None, ${metadata})`
            } else if (metadata) {
                usesField = true
                suffix = ` = field(${metadata})`
            } else if (optional) {
                suffix = ' = None'
            }
            if (field.description) body.push(`    # ${field.description}`)
            body.push(`    ${name}: ${annotation}${suffix}`)
        })
        body.push('', '')
    }

    // deferred annotations, so recursive types can name themselves before the class exists
    const head = ['from __future__ import annotations', '']
    head.push(`from dataclasses import dataclass${usesField ? ', field' : ''}`)
    head.push(`from dataclasses_json import dataclass_json${usesField ? ', config' : ''}`)
    if (usesEnum) head.push('from enum import Enum')
    if (typing.size) head.push(`from typing import ${[...typing].sort().join(', ')}`)
    head.push(...[...imports].sort())
    return head.join('\n') + '\n\n\n' + body.join('\n').replace(/\n{4,}/g, '\n\n\n').trim() + '\n'
}

// ---- TypeScript / JavaScript

const TS_TYPES = {
    string: 'string', integer: 'number', long: 'number', double: 'number', decimal: 'number',
    boolean: 'boolean', date: 'string', datetime: 'string', uuid: 'string', any: 'any',
}

function tsType(t) {
    const base =
        t.kind === 'array' ? `${tsType(t.of)}[]`
        : t.kind === 'map' ? `Record<string, ${tsType(t.of)}>`
        : t.kind === 'tuple' ? `[${t.of.map(tsType).join(', ')}]`
        : t.kind === 'ref' ? t.name
        : TS_TYPES[t.kind] ?? 'any'
    return base
}

const tsKey = key => (isIdentifier(key) ? key : JSON.stringify(key))

function emitTypeScript(model) {
    const out = []
    for (const type of model.types) {
        if (type.kind === 'alias') {
            out.push(`export type ${type.name} = ${tsType(type.type)}`, '')
            continue
        }
        if (type.kind === 'enum') {
            if (type.description) out.push(`/** ${type.description} */`)
            out.push(`export type ${type.name} = ${type.values.map(v => JSON.stringify(v)).join(' | ')}`, '')
            continue
        }
        out.push(`export class ${type.name} {`)
        type.fields.forEach(field => {
            if (field.description) out.push(`    /** ${field.description} */`)
            const optional = field.optional || field.type.nullable
            out.push(`    ${tsKey(field.key)}${optional ? '?' : '!'}: ${tsType(field.type)}`)
        })
        out.push('', `    constructor(init?: Partial<${type.name}>) { Object.assign(this, init) }`, '}', '')
    }
    return out.join('\n').trim() + '\n'
}

function emitJavaScript(model) {
    const out = []
    for (const type of model.types) {
        if (type.kind === 'alias' || type.kind === 'enum') continue
        out.push(`export class ${type.name} {`)
        type.fields.forEach(field => {
            const optional = field.optional || field.type.nullable
            out.push(`    /** @type {${tsType(field.type)}${optional ? '|undefined' : ''}}${field.description ? ` ${field.description}` : ''} */`)
            out.push(`    ${isIdentifier(field.key) ? field.key : `[${JSON.stringify(field.key)}]`}`)
        })
        out.push('', '    constructor(init = {}) { Object.assign(this, init) }', '}', '')
    }
    return out.join('\n').trim() + '\n'
}

const EMITTERS = { csharp: emitCSharp, python: emitPython, typescript: emitTypeScript, javascript: emitJavaScript }

// ---------------------------------------------------------------------------- api

/**
 * @param {object} opts
 * @param {string} opts.name    source file name, used to name the root type and the output file
 * @param {*}      opts.json    the JSON document (parsed, or a string)
 * @param {object} [opts.schema] its JSON Schema - richer output when supplied
 * @param {string} opts.language one of TYPE_LANGUAGES
 * @returns {{ path:string, content:string, language:string }}
 */
export function generateTypes({ name = 'data.json', json, schema, language } = {}) {
    const target = TYPE_LANGUAGES.find(l => l.id === language)
    if (!target) throw new Error(`Unsupported language '${language}'`)

    const value = typeof json === 'string' ? JSON.parse(json) : json
    const parsedSchema = typeof schema === 'string' ? JSON.parse(schema) : schema
    const model = buildModel({ name, json: value, schema: parsedSchema })

    const base = String(name).replace(/\.ui\.json$/, '').replace(/\.[^.]+$/, '') || 'data'
    return {
        path: base + target.ext,
        content: EMITTERS[language](model),
        language,
    }
}

export default generateTypes
