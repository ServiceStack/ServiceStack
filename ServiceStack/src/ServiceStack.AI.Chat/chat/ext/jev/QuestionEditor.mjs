import StudioNotice from './StudioNotice.mjs'
import { clone, label, validKey, renameQuestion, removeExpectations } from './recipeModel.mjs'
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
            try {
                this.$emit('change', renameQuestion(this.recipe, oldKey, newKey))
                this.error = ''
            } catch (e) {
                this.error = e.message
            }
        },
        label,
        title(key) {
            return this.recipe.presentation?.questions?.[key]?.label || label(key)
        },
        setTitle(key, value) {
            this.edit((d) => {
                d.presentation ||= { questions: {} }
                d.presentation.questions ||= {}
                d.presentation.questions[key] ||= {}
                d.presentation.questions[key].label = value
            })
        },
        instructions(key, value) {
            this.edit((d) => {
                d.questions[key].instructions = value
                removeExpectations(d, key)
            })
        },
        optionTitle(key, option, value) {
            this.edit((d) => {
                d.presentation ||= { questions: {} }
                d.presentation.questions ||= {}
                d.presentation.questions[key] ||= {}
                d.presentation.questions[key].optionLabels ||= {}
                if (value.trim()) d.presentation.questions[key].optionLabels[option] = value
                else delete d.presentation.questions[key].optionLabels[option]
            })
        },
        changeType(key, kind) {
            this.edit((d) => {
                const q = d.questions[key]
                q.type = kind
                q.criteria =
                    kind === 'score'
                        ? ['Low', 'High']
                        : kind === 'noul'
                          ? { true: 'The condition is met.', false: 'The condition is not met.' }
                          : { option_a: 'First alternative.', option_b: 'Second alternative.' }
                delete d.presentation?.questions?.[key]?.optionLabels
                removeExpectations(d, key)
            })
        },
        remove(key) {
            if (Object.keys(this.recipe.questions).length <= 1) return
            this.error = ''
            this.edit((d) => {
                delete d.questions[key]
                delete d.presentation?.questions?.[key]
                removeExpectations(d, key)
            })
        },
        add() {
            this.edit((d) => {
                let n = 1
                while ('question_' + n in d.questions) n++
                d.questions['question_' + n] = {
                    type: 'noul',
                    instructions: 'Does `text` meet the condition?',
                    criteria: { true: 'The condition is met.', false: 'The condition is not met.' },
                }
            })
        },
        optionKey(question, oldKey, newKey) {
            if (
                !validKey(newKey) ||
                (oldKey !== newKey && newKey in this.recipe.questions[question].criteria)
            ) {
                this.error = 'Choose a unique option key using letters, numbers and underscores.'
                return
            }
            this.error = ''
            this.edit((d) => {
                const q = d.questions[question]
                q.criteria = Object.fromEntries(
                    Object.entries(q.criteria).map(([k, v]) => [k === oldKey ? newKey : k, v]),
                )
                const names = d.presentation?.questions?.[question]?.optionLabels
                if (names?.[oldKey]) {
                    names[newKey] = names[oldKey]
                    if (oldKey !== newKey) delete names[oldKey]
                }
                removeExpectations(d, question)
            })
        },
        addOption(key) {
            this.edit((d) => {
                const q = d.questions[key]
                if (q.type === 'score') q.criteria.push('Describe this level')
                else {
                    let n = 1
                    while ('option_' + n in q.criteria) n++
                    q.criteria['option_' + n] = 'Describe this alternative.'
                }
                removeExpectations(d, key)
            })
        },
        removeOption(key, option) {
            this.edit((d) => {
                const q = d.questions[key]
                if (q.type === 'score') q.criteria.splice(option, 1)
                else {
                    delete q.criteria[option]
                    delete d.presentation?.questions?.[key]?.optionLabels?.[option]
                }
                removeExpectations(d, key)
            })
        },
        move(key, index, direction) {
            this.edit((d) => {
                const a = d.questions[key].criteria
                ;[a[index], a[index + direction]] = [a[index + direction], a[index]]
                removeExpectations(d, key)
            })
        },
        criteria(key, option, value) {
            this.edit((d) => {
                d.questions[key].criteria[option] = value
                removeExpectations(d, key)
            })
        },
    },
    template: `<section data-jev-editor-section class="mt-6 mb-[35px]"><div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div><h3 class="text-base font-semibold">Questions</h3><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] mt-1">Ask focused questions about the same input. Each is evaluated independently.</p></div><button type="button"  @click="add" :disabled="Object.keys(recipe.questions).length>=32" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="plus"  class="w-4.5 h-4.5 shrink-0"/>Add question</button></div>
      <StudioNotice v-if="error" tone="error" :message="error"/>
      <details v-for="(q,key,index) in recipe.questions" :key="key"  open data-jev-question-editor class="border border-gray-200 dark:border-gray-700 rounded-[10px] mb-3.5 bg-white dark:bg-gray-900 min-w-0"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex gap-2.5 items-center flex-wrap py-3.5 px-4 text-[13px] font-medium bg-slate-50 dark:bg-gray-800 rounded-[10px]"><span data-jev-badge class="inline-flex bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[11px] rounded-[5px] py-0.75 px-[7px] items-center font-normal">{{index+1}}</span><span data-jev-editor-title class="flex-1 min-w-0 wrap-anywhere">{{title(key)}}</span><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] ml-auto font-normal">{{q.type==='choice'?'Choose one':q.type==='score'?'Ordered scale':'Yes / no'}}</span><button type="button"  :aria-label="'Delete question '+title(key)" :disabled="Object.keys(recipe.questions).length<=1" :title="Object.keys(recipe.questions).length<=1?'Recipes need at least one question.':'Delete question'" @click.stop.prevent="remove(key)" data-jev-icon-button data-jev-danger data-jev-editor-delete class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-red-700 dark:text-red-300 border-0 rounded-md p-1.75 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-red-700 hover:dark:text-red-300 shrink-0 min-w-8 min-h-8 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11"><StudioIcon name="trash"  class="w-4.5 h-4.5 shrink-0"/></button></summary>
        <div data-jev-question-body class="pt-4.5 pr-4.5 pb-3.5 pl-4.5 max-[620px]:py-3.5 max-[620px]:px-3"><div data-jev-field-pair class="grid grid-cols-2 gap-4 max-[850px]:grid-cols-1"><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Result label<input :value="title(key)" @input="setTitle(key,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></label><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Answer type<select :value="q.type" @change="changeType(key,$event.target.value)"  class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"><option value="noul">Yes / no · Noul</option><option value="choice">Choose one · Choice</option><option value="score">Ordered scale · Score</option></select></label></div>
          <label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Question<textarea rows="2" :value="q.instructions" @input="instructions(key,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal resize-y max-[620px]:text-base"></textarea></label>
          <p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">{{q.type==='score'?'Describe each level clearly, ordered from low to high.':q.type==='choice'?'Make alternatives distinct; add other or unclear when useful.':'A higher probability means yes.'}}</p>
          <div v-for="(description,option) in q.criteria" :key="option"  data-jev-option-editor class="flex items-start gap-3 pt-4 min-w-0 max-[850px]:flex-wrap max-[620px]:gap-2">
            <label v-if="q.type==='choice'"  data-jev-option-key class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200 w-30 shrink-0 max-[850px]:w-full">Option key<input :value="option" @change="optionKey(key,option,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></label>
            <span v-else data-jev-option-index class="w-7 text-slate-500 dark:text-slate-400 text-xs pt-7">{{q.type==='score'?option:option==='true'?'Yes':'No'}}</span>
            <label data-jev-option-description class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200 flex-1 min-w-0 max-[850px]:basis-[70%]"><template v-if="q.type==='choice'">Display label<input :value="recipe.presentation?.questions?.[key]?.optionLabels?.[option]||''" :placeholder="label(option)" @input="optionTitle(key,option,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"></template>Criteria<textarea rows="2" :value="description" @input="criteria(key,option,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal resize-y max-[620px]:text-base"></textarea></label>
            <div data-jev-option-actions class="flex flex-col pt-5.5 max-[850px]:pt-5 pointer-coarse:gap-1.5"><template v-if="q.type==='score'"><button type="button"  :disabled="option===0" aria-label="Move level up" @click="move(key,option,-1)" data-jev-icon-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-slate-500 dark:text-slate-400 border-0 rounded-md p-1 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-gray-800 hover:dark:text-gray-200 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11">↑</button><button type="button"  :disabled="option===q.criteria.length-1" aria-label="Move level down" @click="move(key,option,1)" data-jev-icon-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-slate-500 dark:text-slate-400 border-0 rounded-md p-1 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-gray-800 hover:dark:text-gray-200 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11">↓</button></template><button v-if="q.type!=='noul'" type="button"  :disabled="Object.keys(q.criteria).length<=2" aria-label="Remove option" @click="removeOption(key,option)" data-jev-icon-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-slate-500 dark:text-slate-400 border-0 rounded-md p-1 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-gray-800 hover:dark:text-gray-200 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11"><StudioIcon name="close"  class="w-4.5 h-4.5 shrink-0"/></button></div>
          </div><button v-if="q.type!=='noul'" type="button"  :disabled="Object.keys(q.criteria).length >= (q.type==='score'?10:255)" @click="addOption(key)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Add {{q.type==='score'?'level':'option'}}</button>
          <details data-jev-advanced-key class="text-xs text-slate-500 dark:text-slate-400 mt-[15px]"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Output key</summary><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200 mt-3">Output key<input :value="key" @change="rename(key,$event.target.value)"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal font-mono max-[620px]:text-base"></label></details>
        </div></details>
    </section>`,
}
