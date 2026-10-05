import JsonBlock from './JsonBlock.mjs'
import StudioNotice from './StudioNotice.mjs'
import { ref, computed, watch, nextTick, onUnmounted } from 'vue'
import { clone, cost, label } from './recipeModel.mjs'
import StudioIcon from './StudioIcon.mjs'
import { CheckBox } from '/ui/components/CheckBox.mjs'
export default {
    components: { JsonBlock, StudioNotice, StudioIcon, CheckBox },
    props: {
        open: Boolean,
        origin: Object,
        models: Array,
        defaultModel: String,
        api: Function,
    },
    emits: ['close', 'apply'],
    template: `<dialog ref="dialog"  @cancel="$emit('close')" @click="backdrop" aria-labelledby="jev-ai-title" data-jev-ai-dialog class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 fixed inset-0 m-auto border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 rounded-2xl shadow-xl p-6.5 max-h-[85vh] overflow-auto w-[min(650px,_calc(100%_-_32px))] text-sm backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)] max-[620px]:p-5"><div data-jev-ai-inner>
      <div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div data-jev-ai-heading class="flex items-center gap-2.5 pr-8"><StudioIcon name="spark"  class="w-4.5 h-4.5 shrink-0 text-indigo-600 dark:text-indigo-300"/><h2 id="jev-ai-title"  class="text-[21px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">{{origin?.improve?'Improve this recipe':'Create with AI'}}</h2></div><button type="button"  aria-label="Close AI assistant" @click="$emit('close')" data-jev-dialog-close class="absolute top-2 right-2 grid place-items-center size-8 rounded-lg cursor-pointer text-slate-400 hover:bg-slate-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:size-11"><StudioIcon name="close" class="size-5"/></button></div>
      <p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7]">Describe the decision you want to make. A chat model designs the recipe; Jev evaluates it when you run.</p>
      <form @submit.prevent="generate"  class="mt-5.5"><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">{{origin?.improve?'What would you like to change?':'What do you want to decide?'}}<textarea :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]" ref="goalField" v-model="goal" rows="4" required maxlength="6000" placeholder="For example: classify customer emails by intent, tone, and whether they need a reply today." data-jev-ai-goal class="focus-visible:outline-none focus-visible:outline-offset-3 resize-y block w-full min-w-0 mt-1.5 border border-solid rounded-lg py-[9px] px-3 text-sm outline-none focus:border-blue-500 focus:shadow-[0_0_0_3px_rgb(59_130_246_/_12%)] max-[620px]:text-base"></textarea></label>
        <div data-jev-ai-model-field class="flex items-center justify-between gap-4 mb-4.5"><span class="block text-[13px] font-medium whitespace-nowrap">Recipe-writing model</span><ModelPicker class="flex-1 max-w-[340px] [&_.llms-model-trigger]:px-3 [&_.llms-model-trigger]:py-2 [&_.llms-model-trigger>span]:flex [&_.llms-model-trigger>span]:items-center [&_.llms-model-trigger>span]:gap-2 [&_.llms-model-trigger>span]:min-w-0 [&_.llms-model-trigger>span]:whitespace-nowrap [&_.llms-model-trigger_strong]:inline [&_.llms-model-trigger_strong]:text-[13px] [&_.llms-model-trigger_strong]:truncate [&_.llms-model-trigger_small]:inline [&_.llms-model-trigger_small]:mt-0 [&_.llms-model-trigger_small]:text-[11px]" ref="modelPicker" :output-modalities="['text']" trigger-label="Choose recipe-writing model" help-text="Choose a text model to design your recipe. Your chat model stays the same." v-model="model" :models="textModels" :disabled="busy"/></div>
        <label data-jev-check class="inline-flex text-xs font-normal mb-3.5 text-gray-800 dark:text-gray-200 items-center gap-2 mt-0.5 mb-3"><CheckBox v-model="includeExample" :disabled="busy"/>Use my current input to help design the recipe</label><details v-if="includeExample"  data-jev-criteria class="text-xs text-slate-500 dark:text-slate-400 mt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Input that will be sent</summary><JsonBlock :text="JSON.stringify(origin?.input,null,2)"/></details>
        <p v-if="origin?.improve"  data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7]">The current recipe definition will also be sent. Chat history and project files are not included.</p>
        <div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-5"><button type="submit"  :disabled="busy||!goal.trim()||!model.trim()" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11"><StudioIcon name="spark"  class="w-4.5 h-4.5 shrink-0"/>{{busy?'Designing recipe…':result?.recipe?'Generate another draft':'Generate recipe'}}</button><span v-if="busy"  role="status" aria-label="Generating recipe" data-jev-spinner class="inline-block w-4 h-4 border-2 border-gray-200 dark:border-gray-700 border-t-indigo-600 dark:border-t-indigo-300 rounded-full animate-spin shrink-0 motion-reduce:animate-none"/></div>
      </form>
      <StudioNotice v-if="error" tone="error" :message="error"/>
      <div v-if="result"  data-jev-ai-proposal class="mt-6 border-t border-t-gray-200 dark:border-t-gray-700 pt-5"><div v-if="result.recipe"><h3 class="mb-2.5 text-[17px] font-medium">{{result.recipe.name}}</h3><p class="leading-[1.7]">{{result.recipe.description}}</p><div data-jev-ai-proposal-fields class="flex flex-wrap gap-1.5 my-[15px] mx-0"><span v-for="field in Object.values(result.recipe.inputSchema.properties)" :key="field.title"  data-jev-badge class="inline-flex bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[11px] rounded-[5px] py-0.75 px-[7px] items-center font-normal">{{field.title}}</span></div><div v-for="(q,key) in result.recipe.questions" :key="key"  data-jev-ai-proposed-question class="py-3 px-0 border-t border-t-gray-200 dark:border-t-gray-700 text-xs"><strong>{{result.recipe.presentation?.questions?.[key]?.label||label(key)}}</strong><span data-jev-badge class="inline-flex bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[11px] rounded-[5px] py-0.75 px-[7px] items-center font-normal ml-2.5">{{q.type}}</span><p class="mt-[7px] leading-[1.7] text-slate-500 dark:text-slate-400">{{q.instructions}}</p></div><details data-jev-criteria class="text-xs text-slate-500 dark:text-slate-400 mt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Review full recipe JSON</summary><JsonBlock :text="JSON.stringify(result.recipe,null,2)"/></details><div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-5"><button type="button"  @click="$emit('apply',clone(result.recipe))" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11">{{origin?.improve?'Review & apply changes':'Use this draft'}}</button><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Editable before saving or running</span></div></div>
        <template v-else><StudioNotice tone="warning" :message="result.diagnostic"/><details data-jev-criteria class="text-xs text-slate-500 dark:text-slate-400 mt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Returned draft</summary><JsonBlock :text="result.draft"/></details><button type="button" :disabled="busy" @click="generate(true)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Repair draft</button></template>
        <p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7]">{{cost(result.usage?.cost)}} · {{result.model}}</p>
      </div>
    </div></dialog>`,
    setup(props, { emit }) {
        const dialog = ref(null),
            modelPicker = ref(null),
            goalField = ref(null),
            goal = ref(''),
            model = ref(''),
            includeExample = ref(false),
            busy = ref(false),
            result = ref(null),
            error = ref('')
        let session = 0,
            disposed = false,
            previousFocus
        const textModels = computed(() =>
            (props.models || []).filter(
                (m) =>
                    !(m.name || m.id || '').toLowerCase().includes('jev') &&
                    (!m.modalities?.output ||
                        m.modalities.output.includes('text')),
            ),
        )
        watch(
            () => props.open,
            async (open) => {
                if (open) {
                    previousFocus = document.activeElement
                    session++
                    goal.value = ''
                    const chosen =
                        textModels.value.find(
                            (m) => (m.name || m.id) === props.defaultModel,
                        ) || textModels.value[0]
                    model.value = chosen?.name || chosen?.id || ''
                    includeExample.value = false
                    busy.value = false
                    result.value = null
                    error.value = ''
                    await nextTick()
                    dialog.value?.showModal()
                    goalField.value?.focus()
                } else {
                    modelPicker.value?.close()
                    dialog.value?.close()
                    previousFocus?.focus?.()
                }
            },
        )
        async function generate(repair = false) {
            if (busy.value) return
            const token = session
            busy.value = true
            error.value = ''
            const body = {
                goal: goal.value,
                model: model.value,
                ...(props.origin?.improve
                    ? { recipe: clone(props.origin.recipe) }
                    : {}),
                ...(includeExample.value
                    ? { example: clone(props.origin?.input || {}) }
                    : {}),
                ...(repair === true && result.value?.draft
                    ? { repair: result.value.draft }
                    : {}),
            }
            try {
                const response = await props.api(
                    props.origin?.improve ? '/improve' : '/generate',
                    'POST',
                    body,
                )
                if (!disposed && token === session) result.value = response
            } catch (e) {
                if (!disposed && token === session) error.value = e.message
            } finally {
                if (!disposed && token === session) busy.value = false
            }
        }
        function backdrop(event) {
            if (event.target === dialog.value) {
                const rect = dialog.value.getBoundingClientRect()
                if (
                    event.clientX < rect.left ||
                    event.clientX > rect.right ||
                    event.clientY < rect.top ||
                    event.clientY > rect.bottom
                )
                    emit('close')
            }
        }
        onUnmounted(() => {
            disposed = true
            dialog.value?.close()
        })
        return {
            dialog,
            modelPicker,
            goalField,
            goal,
            model,
            includeExample,
            busy,
            result,
            error,
            textModels,
            generate,
            backdrop,
            clone,
            cost,
            label,
        }
    },
}
