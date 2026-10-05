import StudioNotice from './StudioNotice.mjs'
import { clone, validKey } from './recipeModel.mjs'
import StudioIcon from './StudioIcon.mjs'
export default {
    components: { StudioNotice, StudioIcon },
    props: { recipe: Object },
    emits: ['change'],
    data: () => ({ error: '' }),
    methods: {
        edit(fn) {
            const doc = clone(this.recipe)
            fn(doc)
            this.$emit('change', doc)
        },
        rename(oldKey, newKey) {
            if (!validKey(newKey) || (oldKey !== newKey && newKey in this.recipe.inputSchema.properties)) {
                this.error = 'Choose a unique field key using letters, numbers and underscores.'
                return
            }
            this.error = ''
            this.edit((d) => {
                d.inputSchema.properties = Object.fromEntries(
                    Object.entries(d.inputSchema.properties).map(([k, v]) => [k === oldKey ? newKey : k, v]),
                )
                d.inputSchema.required = (d.inputSchema.required || []).map((k) =>
                    k === oldKey ? newKey : k,
                )
                if (d.state?.field === oldKey) d.state.field = newKey
                for (const e of d.examples || [])
                    if (oldKey in e.input) {
                        e.input[newKey] = e.input[oldKey]
                        if (oldKey !== newKey) delete e.input[oldKey]
                    }
            })
        },
        required(key, enabled) {
            this.edit((d) => {
                d.inputSchema.required = (d.inputSchema.required || []).filter((k) => k !== key)
                if (enabled) d.inputSchema.required.push(key)
            })
        },
        type(key, type) {
            this.edit((d) => {
                const old = d.inputSchema.properties[key]
                d.inputSchema.properties[key] = {
                    type,
                    title: old.title,
                    description: old.description || '',
                    default: type === 'boolean' ? false : type === 'number' || type === 'integer' ? 0 : '',
                }
                d.examples = []
            })
        },
        remove(key) {
            this.error = ''
            this.edit((d) => {
                delete d.inputSchema.properties[key]
                d.inputSchema.required = (d.inputSchema.required || []).filter((k) => k !== key)
                for (const e of d.examples || []) delete e.input[key]
                if (d.state?.field === key) d.state = { mode: 'object' }
            })
        },
        add() {
            this.edit((d) => {
                let n = 1
                while ('field_' + n in d.inputSchema.properties) n++
                d.inputSchema.properties['field_' + n] = { type: 'string', title: 'New field', default: '' }
            })
        },
        options(key, value) {
            this.edit((d) => {
                const options = value
                    .split('\n')
                    .map((v) => v.trim())
                    .filter(Boolean)
                if (options.length) {
                    d.inputSchema.properties[key].enum = options
                    d.inputSchema.properties[key].default = options[0]
                } else {
                    delete d.inputSchema.properties[key].enum
                    d.inputSchema.properties[key].default = ''
                }
            })
        },
    },
    template: `<section data-jev-editor-section class="mt-6 mb-[35px]"><div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div><h3 class="text-base font-semibold">Input fields</h3><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] mt-1">Define the information someone supplies each time they run this recipe.</p></div><button type="button"  @click="add" :disabled="Object.keys(recipe.inputSchema.properties||{}).length>=32" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="plus"  class="w-4.5 h-4.5 shrink-0"/>Add field</button></div>
      <StudioNotice v-if="error" tone="error" :message="error"/>
      <details v-for="(prop,key) in recipe.inputSchema.properties" :key="key"  open data-jev-field-editor class="border border-gray-200 dark:border-gray-700 rounded-[10px] mb-3.5 bg-white dark:bg-gray-900 min-w-0"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex gap-2.5 items-center flex-wrap py-3.5 px-4 text-[13px] font-medium bg-slate-50 dark:bg-gray-800 rounded-[10px]"><span data-jev-editor-title class="flex-1 min-w-0 wrap-anywhere">{{prop.title||key}}</span><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] ml-auto font-normal">{{prop.type}} · {{(recipe.inputSchema.required||[]).includes(key)?'Required':'Optional'}}</span><button type="button"  :aria-label="'Delete field '+(prop.title||key)" title="Delete field" @click.stop.prevent="remove(key)" data-jev-icon-button data-jev-danger data-jev-editor-delete class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-red-700 dark:text-red-300 border-0 rounded-md p-1.75 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-red-700 hover:dark:text-red-300 shrink-0 min-w-8 min-h-8 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11"><StudioIcon name="trash"  class="w-4.5 h-4.5 shrink-0"/></button></summary>
        <div data-jev-field-body class="pt-4.5 pr-4.5 pb-3.5 pl-4.5 max-[620px]:py-3.5 max-[620px]:px-3"><div data-jev-field-pair class="grid grid-cols-2 gap-4 max-[850px]:grid-cols-1"><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Label<input :value="prop.title" @input="edit(d=>{d.inputSchema.properties[key].title=$event.target.value})"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></label><label v-if="!['object','array'].includes(prop.type)"  class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Type<select :value="prop.type" @change="type(key,$event.target.value)"  class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"><option value="string">Text</option><option value="number">Number</option><option value="integer">Whole number</option><option value="boolean">Yes / no checkbox</option></select></label></div>
          <label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Help text<input :value="prop.description||''" @input="edit(d=>{d.inputSchema.properties[key].description=$event.target.value})"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></label>
          <div data-jev-actions class="flex items-center gap-2.5 flex-wrap"><label data-jev-check class="inline-flex text-xs font-normal mb-3.5 text-gray-800 dark:text-gray-200 items-center gap-2 mt-0.5 mb-3"><input type="checkbox" :checked="(recipe.inputSchema.required||[]).includes(key)" @change="required(key,$event.target.checked)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 max-[620px]:text-base">Required</label><label v-if="prop.type==='string'&&!prop.enum"  data-jev-check class="inline-flex text-xs font-normal mb-3.5 text-gray-800 dark:text-gray-200 items-center gap-2 mt-0.5 mb-3"><input type="checkbox" :checked="prop.format==='textarea'" @change="edit(d=>{if($event.target.checked)d.inputSchema.properties[key].format='textarea';else delete d.inputSchema.properties[key].format})"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 max-[620px]:text-base">Long text</label></div>
          <details v-if="prop.type==='string'"  data-jev-advanced-key class="text-xs text-slate-500 dark:text-slate-400 mt-[15px]"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Dropdown options</summary><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200 mt-3">One value per line<textarea :value="prop.enum?.join('\\n')||''" rows="3" @change="options(key,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal resize-y max-[620px]:text-base"></textarea></label><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Leave empty for free text.</p></details>
          <p v-if="['object','array'].includes(prop.type)"  data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">This structured field is preserved. Edit its nested schema in Recipe JSON.</p>
          <details data-jev-advanced-key class="text-xs text-slate-500 dark:text-slate-400 mt-[15px]"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Field key</summary><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200 mt-3">Field key<input :value="key" @change="rename(key,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal font-mono max-[620px]:text-base"></label><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">After renaming a field, update any question instructions that reference it.</p></details>
        </div></details><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Changing a field's type clears saved examples so you can add cases that fit the new form.</p>
    </section>`,
}
