import StudioIcon from './StudioIcon.mjs'
import RecipeSearch from './RecipeSearch.mjs'
export default {
  components: { StudioIcon, RecipeSearch },
  props: { items: Array, selected: String, loading: Boolean },
  emits: ['select', 'new', 'import'],
  data: () => ({ search: '', filter: 'all' }),
  computed: {
    filtered() {
      const search = this.search.toLowerCase()
      return this.items.filter(
        (item) =>
          (this.filter !== 'favourites' || item.favourite) &&
          [
            item.name,
            item.description,
            item.content || '',
            ...(item.tags || []),
          ]
            .join(' ')
            .toLowerCase()
            .includes(search),
      )
    },
  },
  template: `<aside aria-label="Recipe library" data-jev-library class="flex flex-col bg-slate-50 dark:bg-gray-800 border-r border-r-gray-200 dark:border-r-gray-700 pt-5 pr-3 pb-4 pl-3 min-h-0 max-[620px]:block max-[620px]:border-r-0 max-[620px]:border-b max-[620px]:border-b-gray-200 max-[620px]:dark:border-b-gray-700 max-[620px]:py-3.5 max-[620px]:px-[15px] max-[620px]:shrink-0">
      <div data-jev-library-heading class="flex items-center justify-between py-0 px-2 text-slate-500 dark:text-slate-400 text-[11px] tracking-[1.1px] font-semibold max-[620px]:p-0"><span>RECIPES</span><button type="button" aria-label="New recipe" @click="$emit('new')" data-jev-icon-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-slate-500 dark:text-slate-400 border-0 rounded-md p-1.75 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-gray-800 hover:dark:text-gray-200 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11"><StudioIcon name="plus"  class="w-4.5 h-4.5 shrink-0"/></button></div>
      <RecipeSearch v-model="search" class="mt-4 mr-1 mb-3 ml-1 max-[620px]:my-2 max-[620px]:mx-0"/>
      <div aria-label="Filter recipes" data-jev-filter class="flex gap-0.5 mr-1 mb-4 ml-1 bg-slate-100 dark:bg-slate-800 rounded-[7px] p-0.75 max-[620px]:my-2 max-[620px]:mx-0"><button v-for="f in [{id:'all',label:'All'},{id:'favourites',label:'Starred'}]" :key="f.id" type="button" :aria-pressed="filter===f.id" @click="filter=f.id"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex-1 text-xs p-1 border-0 rounded-[5px] text-slate-500 dark:text-slate-400 bg-transparent aria-pressed:text-gray-800 aria-pressed:dark:text-gray-200 aria-pressed:bg-white aria-pressed:dark:bg-gray-900 aria-pressed:shadow-[0_1px_3px_#0000000a] pointer-coarse:min-h-11">{{f.label}}</button></div>
      <div data-jev-library-list class="overflow-y-auto flex-1 min-h-0 flex flex-col gap-1.25 max-[620px]:flex-row max-[620px]:overflow-x-auto max-[620px]:max-h-27.5 max-[620px]:pb-1.25"><p v-if="loading"  role="status" data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Loading recipes…</p>
        <button v-for="item in filtered" :key="item.id" type="button"  :aria-current="selected===item.id ? 'true' : undefined" @click="$emit('select',item.id)" data-jev-recipe-row class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 border border-transparent bg-transparent text-gray-800 dark:text-gray-200 text-left p-3 rounded-[9px] block w-full hover:bg-slate-100 hover:dark:bg-slate-800 aria-[current=true]:bg-white aria-[current=true]:dark:bg-gray-900 aria-[current=true]:border-gray-200 aria-[current=true]:dark:border-gray-700 aria-[current=true]:shadow-[0_2px_7px_#00000004] max-[620px]:min-w-[185px] max-[620px]:max-w-55 max-[620px]:p-[9px] max-[620px]:shrink-0 group">
          <div data-jev-row class="flex items-center gap-2.5 flex-wrap justify-between"><span data-jev-recipe-name class="group-aria-[current=true]:text-indigo-600 group-aria-[current=true]:dark:text-indigo-300 text-[13px] font-semibold">{{item.name}}</span><StudioIcon v-if="item.favourite" name="star"  class="w-[13px] h-[13px] shrink-0 text-indigo-600 dark:text-indigo-300"/></div>
          <span data-jev-recipe-description class="block text-xs text-slate-500 dark:text-slate-400 mt-1 leading-[1.5] max-[620px]:hidden">{{item.description || 'Your custom decision recipe'}}</span>
          <span data-jev-recipe-meta class="block text-[11px] text-slate-500 dark:text-slate-400 mt-2 max-[620px]:mt-1">{{item.local ? 'Draft' : item.id}}<span v-if="item.questionCount"> · {{item.questionCount}} {{item.questionCount===1?'question':'questions'}}</span></span>
        </button><p v-if="!loading&&!filtered.length"  data-jev-help data-jev-library-empty class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] py-5 px-3">{{search ? 'No recipes match your search.' : filter==='favourites' ? 'Star recipes to keep them close.' : 'Create a recipe or import one from the collection.'}}</p>
      </div>
      <button type="button" @click="$emit('import')" data-jev-button data-jev-library-import class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 mt-[15px] mr-1 ml-1 justify-center inline-flex items-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 max-[620px]:mt-[9px] max-[620px]:mr-0 max-[620px]:mb-0 max-[620px]:ml-0 max-[620px]:py-1 max-[620px]:px-[9px] pointer-coarse:min-h-11"><StudioIcon name="import" class="size-4 shrink-0"/>Import recipe</button>
    </aside>`,
}
