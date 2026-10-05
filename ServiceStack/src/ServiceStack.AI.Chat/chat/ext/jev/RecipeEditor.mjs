import DecisionTagPicker from './DecisionTagPicker.mjs'
import InputSchemaEditor from './InputSchemaEditor.mjs'
import QuestionEditor from './QuestionEditor.mjs'
import JsonEditor from './JsonEditor.mjs'
import { clone } from './recipeModel.mjs'
export default {
    components: {
        DecisionTagPicker,
        InputSchemaEditor,
        QuestionEditor,
        JsonEditor,
    },
    props: {
        api: Function,
        cacheScope: String,
        recipe: Object,
        json: String,
        jsonDirty: Boolean,
        busy: Boolean,
    },
    emits: ['change', 'json', 'apply'],
    data: () => ({ advanced: false }),
    methods: {
        edit(fn) {
            const doc = clone(this.recipe)
            fn(doc)
            this.$emit('change', doc)
        },
    },
    template: `<div data-jev-edit-view class="max-w-212.5"><div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div><h3 class="text-base font-semibold">Make the recipe yours</h3><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] mt-1">Input fields define the form. Questions define the decisions.</p></div><button type="button" :aria-pressed="advanced" @click="advanced=!advanced" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">{{advanced?'Visual editor':'Recipe JSON'}}</button></div>
      <template v-if="!advanced"><div data-jev-editor-section class="mt-6 mb-[35px]"><div data-jev-field-pair class="grid grid-cols-1 gap-4 max-[850px]:grid-cols-1"><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Name<input :value="recipe.name" @input="edit(d=>{d.name=$event.target.value})" maxlength="120"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></label></div><DecisionTagPicker :content="recipe.content" :tags="recipe.tags" :api="api" :cache-scope="cacheScope" @change="edit(d=>{Object.assign(d,$event)})"/><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Description<textarea rows="2" :value="recipe.description" @input="edit(d=>{d.description=$event.target.value})" placeholder="What this recipe helps someone decide"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal resize-y max-[620px]:text-base"></textarea></label></div><InputSchemaEditor :recipe="recipe" @change="$emit('change',$event)"/><QuestionEditor :recipe="recipe" @change="$emit('change',$event)"/></template>
      <template v-else><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Edit the portable recipe, including its input schema and questions. Apply validated JSON before running or saving.</p><JsonEditor :model-value="json" @update:model-value="$emit('json',$event)"/><div data-jev-actions class="flex items-center gap-2.5 flex-wrap"><button type="button" :disabled="!jsonDirty||busy" @click="$emit('apply')" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11">{{busy?'Validating…':'Apply JSON'}}</button><span v-if="jsonDirty"  data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Unapplied changes</span></div></template>
    </div>`,
}
