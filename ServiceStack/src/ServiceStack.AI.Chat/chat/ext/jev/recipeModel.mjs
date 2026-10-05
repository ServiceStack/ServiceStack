export const clone = (value) => JSON.parse(JSON.stringify(value))
export const uid = () =>
    globalThis.crypto?.randomUUID?.() ||
    Date.now() + '-' + Math.random().toString(36).slice(2)
export const label = (key) =>
    key.replaceAll('_', ' ').replace(/^./, (c) => c.toUpperCase())
export const keyPattern = /^[A-Za-z][A-Za-z0-9_]{0,63}$/
export const validKey = (key) =>
    keyPattern.test(key) &&
    !['constructor', 'prototype', '__proto__'].includes(key)
export const activeRun = (run) => ['pending', 'running'].includes(run?.status)
export const percent = (value) =>
    new Intl.NumberFormat(undefined, {
        style: 'percent',
        maximumFractionDigits: 1,
    }).format(value)
export const cost = (value) =>
    typeof value === 'number' && Number.isFinite(value) && value >= 0
        ? new Intl.NumberFormat(undefined, {
              style: 'currency',
              currency: 'USD',
              minimumFractionDigits: 2,
              maximumFractionDigits: 8,
          }).format(value)
        : 'Cost unavailable'

export function defaults(schema) {
    if ('default' in schema) return clone(schema.default)
    if (schema.type === 'object')
        return Object.fromEntries(
            Object.entries(schema.properties || {}).map(([key, prop]) => [
                key,
                defaults(prop),
            ]),
        )
    if (schema.enum?.length) return clone(schema.enum[0])
    return schema.type === 'array'
        ? []
        : schema.type === 'boolean'
          ? false
          : ['integer', 'number'].includes(schema.type)
            ? 0
            : ''
}

export function inputErrors(schema, value, path = '', required = true) {
    const errors = [],
        add = (message) => errors.push({ fieldName: path, message })
    if (schema.type === 'object') {
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            add('Expected an object.')
            return errors
        }
        for (const key of schema.required || [])
            if (!(key in value) || value[key] == null)
                errors.push({
                    fieldName: path ? path + '.' + key : key,
                    message: 'This field is required.',
                })
        for (const [key, v] of Object.entries(value)) {
            if (schema.properties?.[key])
                errors.push(
                    ...inputErrors(
                        schema.properties[key],
                        v,
                        path ? path + '.' + key : key,
                        (schema.required || []).includes(key),
                    ),
                )
            else if (!schema.additionalProperties)
                errors.push({
                    fieldName: key,
                    message: 'Remove this unknown input field.',
                })
        }
    } else if (schema.type === 'string') {
        if (typeof value !== 'string') add('Expected text.')
        else if (required && !value.trim()) add('This field is required.')
        else if (
            (value || required) &&
            (value.length < (schema.minLength || 0) ||
                value.length > (schema.maxLength ?? 524288))
        )
            add('Text length is outside the allowed range.')
    } else if (schema.type === 'array') {
        if (!Array.isArray(value)) add('Expected a list.')
        else {
            if (
                value.length < (schema.minItems || 0) ||
                value.length > (schema.maxItems ?? 500)
            )
                add('List length is outside the allowed range.')
            value.forEach((v, i) =>
                errors.push(...inputErrors(schema.items, v, path + '.' + i)),
            )
        }
    } else if (schema.type === 'boolean') {
        if (typeof value !== 'boolean') add('Expected true or false.')
    } else if (
        typeof value !== 'number' ||
        !Number.isFinite(value) ||
        (schema.type === 'integer' && !Number.isInteger(value))
    )
        add('Enter a valid ' + schema.type + '.')
    else if (
        value < (schema.minimum ?? -Infinity) ||
        value > (schema.maximum ?? Infinity)
    )
        add('Number is outside the allowed range.')
    if (
        schema.enum &&
        !schema.enum.some((v) => typeof v === typeof value && v === value)
    )
        add('Choose one of the listed options.')
    return errors
}

export function compile(recipe, input) {
    return {
        model: recipe.decisionModel,
        state:
            recipe.state?.mode === 'text'
                ? input[recipe.state.field]
                : clone(input),
        questions: clone(recipe.questions),
    }
}

// A recorded result remains valid when only documentation or labels change.
export const executionDefinition = (recipe) =>
    JSON.stringify({
        inputSchema: recipe.inputSchema,
        state: recipe.state || { mode: 'object' },
        questions: recipe.questions,
        decisionModel: recipe.decisionModel,
    })

export function canSaveRunExample(recipe, run) {
    return !!(
        run?.status === 'succeeded' &&
        run.answers &&
        Number.isFinite(run.completedAt) &&
        executionDefinition(recipe) === executionDefinition(run.recipe) &&
        !inputErrors(recipe.inputSchema, run.input).length &&
        (recipe.examples?.length || 0) < 30 &&
        !(recipe.examples || []).some(
            (example) =>
                example.execution?.completedAt ===
                    new Date(run.completedAt * 1000).toISOString() &&
                JSON.stringify(example.input) === JSON.stringify(run.input) &&
                JSON.stringify(example.execution.answers) ===
                    JSON.stringify(run.answers),
        )
    )
}

export function exampleFromRun(recipe, run, name) {
    if (!canSaveRunExample(recipe, run))
        throw Error(
            'Run this recipe successfully before saving it as an example.',
        )
    if (typeof name !== 'string' || !name.trim() || name.trim().length > 120)
        throw Error('Enter an example name of up to 120 characters.')
    const request = compile(recipe, run.input)
    const execution = {
        status: 'succeeded',
        input: clone(run.input),
        prompt: clone(request.state),
        answers: clone(run.answers),
        model: run.response?.model || run.model || request.model,
        completedAt: new Date(run.completedAt * 1000).toISOString(),
        ...(run.durationMs == null ? {} : { durationMs: run.durationMs }),
    }
    return {
        id: uid(),
        label: name.trim(),
        input: clone(run.input),
        provenance: 'authored',
        execution,
    }
}

export function clearChangedExampleResults(previous, next) {
    if (executionDefinition(previous) === executionDefinition(next)) return next
    next = clone(next)
    for (const example of next.examples || []) {
        if (example.execution) {
            delete example.execution
            example.notes ||=
                'Recipe changed; run this input again to record a new result.'
        }
    }
    return next
}

export function blankRecipe() {
    return {
        schemaVersion: 1,
        name: 'Untitled recipe',
        description: '',
        content: '',
        tags: [],
        decisionModel: '~typesafe/jev-latest',
        inputSchema: {
            type: 'object',
            properties: {
                text: {
                    type: 'string',
                    title: 'Text to evaluate',
                    format: 'textarea',
                    default: '',
                },
            },
            required: ['text'],
            additionalProperties: false,
        },
        state: { mode: 'object' },
        questions: {
            is_relevant: {
                type: 'noul',
                instructions:
                    'Does `text` relate to the topic you want to assess?',
                criteria: {
                    true: 'The text directly addresses the topic.',
                    false: 'The text does not address the topic.',
                },
            },
        },
        presentation: {
            questions: { is_relevant: { label: 'Relevant to the topic' } },
        },
        examples: [],
    }
}

export function renameQuestion(recipe, oldKey, newKey) {
    if (!validKey(newKey) || (oldKey !== newKey && newKey in recipe.questions))
        throw Error(
            'Choose a unique question key using letters, numbers and underscores.',
        )
    const result = clone(recipe)
    result.questions = Object.fromEntries(
        Object.entries(result.questions).map(([key, value]) => [
            key === oldKey ? newKey : key,
            value,
        ]),
    )
    const labels = result.presentation?.questions || {}
    if (oldKey in labels) {
        labels[newKey] = labels[oldKey]
        if (oldKey !== newKey) delete labels[oldKey]
    }
    for (const example of result.examples || [])
        if (oldKey in (example.expected || {})) {
            example.expected[newKey] = example.expected[oldKey]
            if (oldKey !== newKey) delete example.expected[oldKey]
        }
    return result
}

export function removeExpectations(recipe, questionKey) {
    for (const example of recipe.examples || [])
        if (questionKey in (example.expected || {})) {
            delete example.expected[questionKey]
            example.provenance = 'authored'
            example.notes = 'Question changed; review its expected answer.'
        }
}

export function expectation(example, key, answer) {
    const recorded = example.execution?.answers?.[key]
    if (recorded && recorded.type === answer?.type) {
        const matched =
            answer.type === 'choice'
                ? recorded.choice === answer.choice
                : answer.type === 'noul'
                  ? (recorded.noul >= 0.5) === (answer.noul >= 0.5)
                  : answer.type === 'score' &&
                    Math.abs(recorded.score - answer.score) <= 0.5
        return matched ? 'Matches saved run' : 'Differs from saved run'
    }
    if (!(key in (example.expected || {}))) return 'Unlabelled'
    const wanted = example.expected[key]
    const matched =
        answer?.type === 'choice'
            ? wanted === answer.choice
            : answer?.type === 'noul'
              ? wanted === answer.noul >= 0.5
              : answer?.type === 'score' &&
                Math.abs(wanted - answer.score) <= 0.5
    return (
        (example.provenance === 'user-reviewed' ? '' : 'Suggested: ') +
        (matched ? 'Matches' : 'Differs')
    )
}
