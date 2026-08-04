# Shared UI components

Components here depend only on `vue`, `@servicestack/vue` and `@servicestack/client`, so they can move into
`@servicestack/vue` unchanged. They style themselves with plain Tailwind + `dark:` variants rather than the
app's `$styles` theme object, for the same reason.

## JsonSchemaForm

Renders a JSON Schema as an editable form, so any JSON API with a published schema can be given a UI.

```html
<JsonSchemaForm :schema="schema" v-model="data" :status="responseStatus" @change="save" />
```

The schema can also be written inline as the component's body, which is handy for demos, docs and pasting a
schema straight out of an API spec:

```html
<JsonSchemaForm v-model="data">{
  "type": "object",
  "properties": { "name": { "type": "string", "title": "Name" } },
  "required": ["name"]
}</JsonSchemaForm>
```

| Prop | Description |
| --- | --- |
| `schema` | the JSON Schema to render. Optional - falls back to parsing the component's body |
| `modelValue` / `data` | the object being edited - mutated in place, `v-model` also works |
| `status` | a ServiceStack `ResponseStatus` whose `errors[]` are shown against their fields |
| `readOnly` | render every input disabled |
| `showTitle` | show the schema's `title`/`description` header (default `true`) |
| `wrapper` | wrap the whole form in the same collapsible panel nested objects get (default `false`) |
| `validateOn` | `'change'` validates as you type, `'submit'` (default) only when `validate()` is called |

Emits `change` (and `update:modelValue`) after every edit. Exposes `validate()`, which returns a
`ResponseStatus`-shaped object (`{ errorCode, message, errors[] }`) or `null` when the data is valid, and
`reset()` to clear client-side errors:

```js
const form = ref()
const status = ref(null)
async function submit() {
    status.value = form.value.validate()
    if (status.value) return
    status.value = (await client.api(new CreateBooking(data.value))).error
}
```

### Schema support

**Structure** - `type` (`object`, `array`, `string`, `number`, `integer`, `boolean`, `null`), type unions like
`["string","null"]`, OpenAPI 3.0's `nullable`, `$ref` into `$defs`/`definitions` (including **recursive**
schemas), `allOf` (merged), `oneOf`/`anyOf` (as a variant picker, or a select when every branch is a `const`),
`const`, tuples via `prefixItems`/array-form `items`, `additionalProperties` (add and remove free-form keys),
and any root type - an object, an array or a bare value.

**Presentation** - `title`, `description` (help text), `examples[0]` (placeholder), `default` (used when adding
an array row or a missing property), `readOnly`, `writeOnly`, `deprecated`, `enum` (with `x-enumNames`),
and `format`: `textarea`, `date`, `date-time`, `time`, `month`, `week`, `email`, `uri`/`url`, `password`,
`color`, `uuid`, `tel`, `search`.

**Constraints** - `required`, `minLength`, `maxLength`, `pattern`, `minimum`, `maximum`,
`exclusiveMinimum`, `exclusiveMaximum`, `multipleOf`, `minItems`, `maxItems`, `uniqueItems`, `enum`. They are
applied to the inputs (`min`, `max`, `step`, `maxlength`, `pattern`) *and* checked by `validate()`.

**Extensions** - `x-widget` forces a control (`textarea`, `select`, `radio`, `checkbox`, `password`, `hidden`),
`x-titleKey` names the property used to label array rows (`"1. Platform review"` rather than `"Item 1"`),
`x-order` sorts properties, and `x-collapsed` starts a group closed.

Objects render as collapsible groups and arrays as rows you can add to, remove and reorder. The **root** object
is an exception: by default its fields render flush, with the schema's `title`/`description` above them as
plain text. Pass `:wrapper="true"` to give it the same bordered, collapsible panel its children get - the
panel header then carries the title instead of the heading above it.

### Validation errors

`status.errors[].fieldName` is matched case-insensitively against each field's path, so `Reference`,
`customer.email`, `lines[1].qty` and `lines.1.qty` all find their field. A bare leaf name (`qty`) is matched
too, but **only when that name is unambiguous** in the schema - otherwise an error for `name` would light up
every `name` field in the form. The matching input gets a red border, `aria-invalid` and the message below it.

An error naming a field the schema doesn't render is shown as a summary at the top rather than being
swallowed, as is `status.message` when there are no field errors.

### Notes and limits

- **Collapsing unmounts.** A closed group's fields are removed from the DOM (not just hidden), which is what
  lets a recursive schema stop unfolding - so a collapsed subtree loses transient UI state like focus.
- **Editing is in place.** The bound object is mutated directly and `update:modelValue` re-emits the same
  reference, so a parent watching by reference won't fire - watch `@change` instead, or `{ deep: true }`.
- **Containers are materialised lazily.** A missing object/array starts collapsed and is only created in the
  data when you expand or edit it, so opening a form doesn't dirty the model and recursive schemas don't
  expand forever.
- **`validate()` is a pragmatic subset**, not a spec-complete validator: it covers the keywords listed above
  and does not evaluate `if`/`then`/`else`, `dependentSchemas`, `not`, `contains`, `patternProperties`,
  `propertyNames`, `unevaluated*` or format-specific semantics (an `email` field is checked by the browser,
  not by `validate()`). Treat the server's `ResponseStatus` as the source of truth.
- **Not supported**: remote `$ref` (only same-document refs are followed), `if`/`then`/`else`,
  `dependentRequired`/`dependentSchemas`, `not`, `patternProperties`, `propertyNames`, `contains`,
  `unevaluatedProperties`/`unevaluatedItems`, and file/binary uploads.
- `oneOf`/`anyOf` picks the branch that best fits the current data; switching branch keeps whatever
  properties the new branch shares with the old one and drops the rest.

## jsonTypes

Generates typed classes from a JSON document or a JSON Schema. Deterministic, dependency free and instant -
the same input always produces the same output, and no model is involved.

```js
import { generateTypes, TYPE_LANGUAGES } from '/ui/components/jsonTypes.mjs'

const { path, content } = generateTypes({ name: 'invoice.json', json, schema, language: 'csharp' })
```

`json` and `schema` take either a string or a parsed value; `schema` is optional. `language` is one of
`csharp`, `python`, `typescript`, `javascript` (see `TYPE_LANGUAGES` for their labels and extensions).
Returns `{ path, content, language }`, where `path` is the input name with the language's extension.

**Pass the schema when you have one.** A JSON example only carries JSON's six types, so the schema is what
turns `required` into non-nullable members, `multipleOf: 0.01` into `decimal`, `format` into date/uuid types,
`enum` into a real enum, `description` into doc comments and `additionalProperties`/`prefixItems` into maps
and tuples. Where the two disagree - a `format: date` against a `"31 July 2026"` example - **the example
wins**, so the generated types can still parse the document they were generated from.

Without a schema the shape is inferred from the example: ISO date/date-time/UUID strings become typed,
integers widen to `long` past int32, array element shapes are merged, and `null`/`[]` degrade to `object`.

Structurally identical objects collapse into one type; recursive `$ref`s generate recursive classes; keys that
aren't identifiers keep their wire name (`X-Api-Key` -> `XApiKey` + `[JsonPropertyName("X-Api-Key")]`, `2fa` ->
`_2fa`, `café` -> `Café`) and language keywords are escaped (`@class`, `class_`).

`buildModel()` is also exported for anyone who wants the language-neutral model - a list of named
object/enum/alias types plus the root - to write their own emitter against.
