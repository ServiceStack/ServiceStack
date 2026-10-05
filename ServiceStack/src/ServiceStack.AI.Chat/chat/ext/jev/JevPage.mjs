import JsonBlock from './JsonBlock.mjs'
import ShareRecipeDialog from './ShareRecipeDialog.mjs'
import SaveExampleDialog from './SaveExampleDialog.mjs'
import StudioNotice from './StudioNotice.mjs'
import {
    ref,
    reactive,
    computed,
    inject,
    onMounted,
    onUnmounted,
    watch,
    nextTick,
} from 'vue'
import RecipeLibrary from './RecipeLibrary.mjs'
import DecisionResults from './DecisionResults.mjs'
import RecipeEditor from './RecipeEditor.mjs'
import RecipeExamples from './RecipeExamples.mjs'
import ExamplePicker from './ExamplePicker.mjs'
import RunHistory from './RunHistory.mjs'
import AiRecipeDialog from './AiRecipeDialog.mjs'
import ImportRecipeDialog from './ImportRecipeDialog.mjs'
import StudioIcon from './StudioIcon.mjs'
import {
    clone,
    canSaveRunExample,
    exampleFromRun,
    clearChangedExampleResults,
    uid,
    label,
    defaults,
    inputErrors,
    compile,
    blankRecipe,
    activeRun,
} from './recipeModel.mjs'
import { createDraftStore } from './draftStore.mjs'
import { createApi, createRunTracker } from './studioState.mjs'

export default {
    components: {
        JsonBlock,
        ShareRecipeDialog,
        SaveExampleDialog,
        StudioNotice,
        RecipeLibrary,
        DecisionResults,
        RecipeEditor,
        RecipeExamples,
        ExamplePicker,
        RunHistory,
        AiRecipeDialog,
        ImportRecipeDialog,
        StudioIcon,
    },
    template: `<div data-jev-studio class="text-gray-800 dark:text-gray-200 bg-white dark:bg-gray-900 h-full min-h-0 flex flex-col text-sm leading-[1.5] isolate [--jev-sidebar-width:242px] max-[1100px]:[--jev-sidebar-width:215px]">
      <header data-jev-header class="min-[850px]:-mt-6 pb-2 border-b border-b-gray-200 dark:border-b-gray-700 grid grid-cols-[var(--jev-sidebar-width)_minmax(0,_1fr)] items-center shrink-0 max-[1100px]:py-[17px] max-[620px]:py-3.5 max-[620px]:px-4 max-[620px]:flex max-[620px]:flex-wrap max-[620px]:gap-3"><div data-jev-brand class="flex gap-3 items-center min-w-0 pl-2 max-[1100px]:pl-[23px] max-[620px]:pl-0"><div><h1 class="text-[19px] font-semibold tracking-[-0.3px] max-[620px]:text-[17px]">Decision Studio</h1><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] max-[620px]:hidden">Small questions. Useful answers.</span></div></div><div data-jev-header-actions class="flex items-center gap-4 flex-wrap max-[850px]:gap-3 max-[620px]:ml-auto">
        <button type="button"  @click="openAI(false)" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11">
            <StudioIcon name="spark"  class="w-4.5 h-4.5 shrink-0"/>Create with AI</button>
        <span data-jev-connection role="status" class="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1.75 max-[850px]:hidden">
        <span class="size-1.5 rounded-full" :class="status.available ? 'bg-emerald-500' : 'bg-slate-500 dark:bg-slate-400'"/>Jev via OpenRouter</span></div></header>
      <div data-jev-layout class="grid grid-cols-[var(--jev-sidebar-width)_minmax(0,_1fr)] flex-1 min-h-0 max-[620px]:flex max-[620px]:flex-col max-[620px]:overflow-auto"><RecipeLibrary :items="library" :selected="selected" :loading="loading" @select="select" @new="fresh" @import="importOpen=true"/>
        <main data-jev-workspace class="overflow-auto min-w-0 pt-6.5 pr-8 pb-11 pl-8 min-[1650px]:py-[35px] min-[1650px]:px-12.5 max-[1100px]:p-[23px] max-[620px]:overflow-visible max-[620px]:pt-5 max-[620px]:pr-[17px] max-[620px]:pb-[35px] max-[620px]:pl-[17px] max-[620px]:flex-1">
          <StudioNotice v-if="error"  tone="error" :message="error" dismissible @dismiss="error=''" data-jev-page-alert class="mb-4.5"/>
          <StudioNotice v-if="notice"  :tone="noticeTone" :message="notice" dismissible @dismiss="notice=''" data-jev-page-alert class="mb-4.5"/>
          <div v-if="!status.available&&!loading"  data-jev-setup class="flex items-center gap-3.5 flex-wrap bg-slate-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-4 rounded-[10px] mb-[25px] max-[620px]:p-3"><StudioIcon name="branch"  class="w-4.5 h-4.5 shrink-0 text-indigo-600 dark:text-indigo-300"/><div class="flex-1 min-w-50 max-[620px]:min-w-45"><strong class="text-[13px] font-medium">Connect OpenRouter when you’re ready to run</strong><p class="text-xs text-slate-500 dark:text-slate-400 mt-[3px]">{{status.message}} You can explore and design recipes now.</p></div><button type="button" @click="ctx.openModal('models')" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Models &amp; providers</button><button type="button" @click="refreshStatus" data-jev-text-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 text-indigo-600 dark:text-indigo-300 bg-transparent border-0 text-xs text-left underline underline-offset-3">Check connection</button></div>
          <template v-if="current">
            <div data-jev-recipe-header class="flex justify-between gap-5 items-start mb-5.5 max-[1100px]:flex-wrap max-[620px]:gap-[13px] max-[620px]:mb-[15px]"><div class="max-w-155 min-w-0"><div data-jev-eyebrow class="text-[10px] font-semibold tracking-[1.1px] text-slate-500 dark:text-slate-400 mb-1.5">{{current.id.startsWith('local:')?'NEW RECIPE':'YOUR RECIPE'}}<span v-if="!current.id.startsWith('local:')"> · {{current.id}}</span><span v-if="dirty"> · Unsaved changes</span></div><h2 class="text-[26px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">{{current.recipe.name}}</h2><p class="text-slate-500 dark:text-slate-400 mt-2 text-[13px] leading-[1.7]">{{current.recipe.description||'Define your input and the questions you want to answer.'}}</p><a v-if="current.importSource?.publishedUrl" :href="current.importSource.publishedUrl" target="_blank" rel="noopener noreferrer" data-jev-source-link class="inline-block mt-2 text-xs text-slate-500 dark:text-slate-400 underline underline-offset-3 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-3">Original recipe</a></div>
              <div data-jev-actions class="flex items-center gap-2.5 flex-wrap"><button v-if="!current.id.startsWith('local:')"  type="button" :aria-label="favourite?'Unstar recipe':'Star recipe'" :aria-pressed="favourite" @click="toggleFavourite" data-jev-icon-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-transparent text-slate-500 dark:text-slate-400 border-0 rounded-md p-1.75 inline-flex items-center justify-center hover:bg-slate-100 hover:dark:bg-slate-800 hover:text-gray-800 hover:dark:text-gray-200 aria-pressed:text-indigo-600 aria-pressed:dark:text-indigo-300 aria-pressed:bg-indigo-50 aria-pressed:dark:bg-indigo-950/50 max-[620px]:min-w-8 max-[620px]:min-h-8 pointer-coarse:min-h-11"><StudioIcon name="star"  class="w-4.5 h-4.5 shrink-0"/></button><a v-if="current.publication?.publishedUrl" :href="current.publication.publishedUrl" target="_blank" rel="noopener" title="View public recipe (opens in a new tab)" data-jev-shared-link class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium no-underline cursor-pointer bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 hover:bg-emerald-200 dark:hover:bg-emerald-900 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2"><svg class="size-3.5 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"></path></svg>Shared</a><button type="button" :disabled="busy||current.jsonDirty" @click="shareRecipe" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="share"  class="w-4.5 h-4.5 shrink-0"/>Share</button><button type="button" :disabled="busy||current.jsonDirty" @click="validate" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="check"  class="w-4.5 h-4.5 shrink-0"/>Check</button><button type="button" :disabled="busy||current.jsonDirty||!dirty" @click="save()" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11">{{busy?'Saving…':'Save'}}</button><details data-jev-menu class="relative"><summary aria-label="Recipe actions" data-jev-button class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 list-none tracking-[2px] [&::-webkit-details-marker]:hidden pointer-coarse:min-h-11">•••</summary><div class="absolute right-0 top-[calc(100%_+_5px)] min-w-42.5 z-5 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 shadow-xl rounded-[9px] p-[5px]"><button type="button" @click="shareRecipe"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Share recipe</button><button type="button" @click="duplicateRecipe"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Duplicate</button><button type="button" @click="save(true)" :disabled="current.jsonDirty"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Save as a copy</button><button type="button" @click="exportRecipe"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Export recipe</button><button v-if="!current.id.startsWith('local:')" type="button" @click="askReload"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Reload saved recipe</button><button type="button" @click="askDelete"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full text-left py-[9px] px-2.5 bg-transparent border-0 rounded-[5px] text-gray-800 dark:text-gray-200 text-xs hover:bg-slate-100 hover:dark:bg-slate-800">Delete recipe</button></div></details></div>
            </div>
            <div data-jev-toolbar class="flex items-center justify-between gap-3 border-b border-b-gray-200 dark:border-b-gray-700 mb-[27px] flex-wrap max-[850px]:pb-2.5"><nav aria-label="Recipe workspace" data-jev-tabs class="flex gap-[21px] max-[1100px]:gap-[15px] max-[620px]:gap-4.5 max-[620px]:w-full max-[620px]:justify-between"><button v-for="tab in tabs" :key="tab.id" type="button" :aria-current="view===tab.id?'page':undefined" @click="view=tab.id"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex items-center gap-1.75 py-3 px-0 border-0 border-b-2 border-b-transparent bg-transparent text-[13px] text-slate-500 dark:text-slate-400 aria-[current=page]:border-b-indigo-600 aria-[current=page]:dark:border-b-indigo-300 aria-[current=page]:text-indigo-600 aria-[current=page]:dark:text-indigo-300 aria-[current=page]:font-medium max-[620px]:text-xs max-[620px]:gap-1.25 pointer-coarse:min-h-11"><StudioIcon :name="tab.icon"  class="w-[15px] h-[15px] shrink-0 max-[620px]:w-[13px] max-[620px]:h-[13px]"/>{{tab.label}}</button></nav><label data-jev-model-label class="flex gap-2 items-center text-[11px] text-slate-500 dark:text-slate-400 max-[620px]:w-full max-[620px]:justify-end">Decision model<select :value="current.recipe.decisionModel" aria-label="Decision model" @change="updateModel($event.target.value)"  class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-[7px] text-gray-800 dark:text-gray-200 text-xs py-1.5 px-2 max-w-60"><option v-for="model in status.models" :key="model" :value="model">{{model.includes('latest')?'Jev · Latest':model.replace('typesafe/','')}}</option></select></label></div>
            <StudioNotice v-if="current.jsonDirty" tone="warning" message="Apply or discard the Recipe JSON changes before running or saving."><template #actions><button type="button" @click="discardJSON" data-jev-text-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 text-indigo-600 dark:text-indigo-300 bg-transparent border-0 text-xs text-left underline underline-offset-3">Discard JSON changes</button></template></StudioNotice>
            <div v-if="view==='run'"  data-jev-run-grid class="grid grid-cols-[minmax(0,_1fr)_minmax(0,_1fr)] gap-9.5 items-start max-w-310 min-[1650px]:gap-13.5 max-[1100px]:gap-6 max-[850px]:grid-cols-1">
              <section data-jev-input-panel class="min-w-0"><div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div><h3 class="text-base font-semibold">Your input</h3><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] mt-1">{{Object.keys(current.recipe.questions).length}} independent questions in one call</p></div><ExamplePicker v-if="current.recipe.examples?.length" :key="selected" :examples="current.recipe.examples" :selected-id="current.exampleId" @select="loadExample"/></div>
                <form @submit.prevent="runCurrent"  data-jev-input-form class="[&_.text-red-500]:inline-block [&_.text-red-500]:size-[1em] [&_.text-red-500]:ml-auto [&_.text-red-500]:shrink-0 [&_.text-red-500]:overflow-hidden [&_.text-red-500]:indent-[-9999px] [&_.text-red-500]:leading-none [&_.text-red-500]:bg-current [&_.text-red-500]:cursor-help [&_.text-red-500]:[mask:url(data:image/svg+xml,%3Csvg%20xmlns=%27http://www.w3.org/2000/svg%27%20viewBox=%270%200%2024%2024%27%3E%3Cpath%20d=%27M18.562%2014.634L14%2012l4.562-2.634a1%201%200%200%200-1-1.732L13%2010.268V5a1%201%200%200%200-2%200v5.268L6.438%207.634a1%201%200%200%200-1%201.732L10%2012l-4.562%202.634a1%201%200%200%200%201%201.732L11%2013.732V19a1%201%200%200%200%202%200v-5.268l4.562%202.634a1%201%200%200%200%201-1.732%27/%3E%3C/svg%3E)_no-repeat_center/contain] dark:[&_.text-red-500]:text-red-400 [&_textarea]:min-h-45 [&_label]:flex [&_label]:items-center [&_label]:justify-between [&_label]:w-full [&_label]:text-[13px] [&_label]:mb-1.5 [&_[id$=-err]]:hidden mt-6"><JsonSchemaForm :key="selected+':'+current.editVersion" :schema="current.recipe.inputSchema" :data="current.input" :show-title="false" :status="formStatus" @change="updateInput"/>
                  <div data-jev-run-actions class="flex gap-4.5 items-center flex-wrap mt-[23px] mb-2.5"><button type="submit"  :disabled="!canRun||activeRun(current.pending)" data-jev-button data-jev-primary data-jev-run-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-[13px] font-medium py-2.5 px-4.5 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11"><StudioIcon name="play"  class="w-4.5 h-4.5 shrink-0"/>{{activeRun(current.pending)?'Evaluating…':'Run decision'}}</button><button type="button" @click="clearInput" data-jev-text-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 text-indigo-600 dark:text-indigo-300 bg-transparent border-0 text-xs text-left underline underline-offset-3">Clear input</button></div>
                  <StudioNotice v-if="current.submission&&!activeRun(current.pending)" tone="warning" message="Could not confirm whether the decision started. Retry the same submission to recover its status without starting a duplicate."><template #actions><button type="button" :disabled="activeRun(current.pending)" @click="retrySubmission" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Retry submission</button></template></StudioNotice><p v-if="runHelp"  role="status" data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">{{runHelp}}</p>
                </form>
                <details data-jev-request class="border-t border-t-gray-200 dark:border-t-gray-700 mt-[25px] pt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex items-center gap-1.75 text-xs text-slate-500 dark:text-slate-400 list-none"><StudioIcon name="code"  class="w-4.5 h-4.5 shrink-0"/>Request JSON &amp; curl</summary><JsonBlock :text="requestJSON"/><div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-3"><button type="button" @click="copy(requestJSON)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Copy JSON</button><button type="button" @click="copyCurl" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Copy curl</button></div></details>
                <div data-jev-recipe-explainer class="mt-7.5 text-xs"><strong class="text-[11px] uppercase tracking-[0.7px] text-slate-500 dark:text-slate-400 font-medium">What this recipe asks</strong><div v-for="(q,key) in current.recipe.questions" :key="key"  class="flex gap-2.5 items-center mt-3 text-slate-500 dark:text-slate-400"><span data-jev-type class="text-[10px] border border-gray-200 dark:border-gray-700 py-0.5 px-[5px] rounded min-w-[45px] text-center">{{q.type==='choice'?'Choice':q.type==='score'?'Score':'Yes / no'}}</span><span>{{current.recipe.presentation?.questions?.[key]?.label||label(key)}}</span></div></div>
              </section>
              <div ref="resultsPanel"><DecisionResults :run="current.result" :pending="current.pending" :stale="stale" :can-save-example="canSaveExample(current.result)" @save-example="saveExample" @cancel="cancel" @export="exportRun"/><StudioNotice v-if="current.pending?.trackingError" tone="warning" :message="current.pending.trackingError"><template #actions><button type="button" @click="reconnect" data-jev-text-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 text-indigo-600 dark:text-indigo-300 bg-transparent border-0 text-xs text-left underline underline-offset-3">Reconnect</button></template></StudioNotice></div>
            </div>
            <template v-else-if="view==='edit'"><div data-jev-edit-actions class="flex items-center justify-between gap-[15px] mb-6 max-[620px]:flex-wrap"><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Keep your questions specific and your criteria clear.</span><button type="button" @click="openAI(true)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="spark"  class="w-4.5 h-4.5 shrink-0"/>Improve with AI</button></div><fieldset data-jev-editor-fieldset class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 border-0 p-0 min-w-0 disabled:opacity-75"><RecipeEditor :api="api" :cache-scope="tagCacheScope" :recipe="current.recipe" :json="current.json" :json-dirty="current.jsonDirty" :busy="busy" @change="updateRecipe" @json="setJSON" @apply="applyJSON"/></fieldset></template>
            <RecipeExamples v-else-if="view==='examples'" :recipe="current.recipe" :results="exampleResults[selected]||{}" :checking="checking===selected" :can-run="status.available&&!current.jsonDirty" :can-save-example="canSaveExample(current.result)" @load="loadExample" @add="saveExample" @remove="removeExample" @rename="renameExample" @check="checkExamples" @stop="stopChecks"/>
            <RunHistory v-else :items="history.items" :cursor="history.cursor" :loading="historyLoading" @select="showHistory" @more="loadHistory(true)" @refresh="loadHistory()" @clear="askClearHistory"/>
          </template>
          <div v-else data-jev-page-empty class="py-20 px-5 text-center"><span v-if="loading"  data-jev-spinner class="inline-block w-4 h-4 border-2 border-gray-200 dark:border-gray-700 border-t-indigo-600 dark:border-t-indigo-300 rounded-full animate-spin shrink-0 motion-reduce:animate-none"/><h2 class="text-[26px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">{{loading?'Loading Decision Studio…':'Start with a decision'}}</h2><p class="my-[15px] mx-0 text-slate-500 dark:text-slate-400">Choose a recipe from the library, create your own, or import one from the collection.</p><div v-if="!loading"  data-jev-actions class="flex items-center gap-2.5 flex-wrap"><button type="button"  @click="fresh" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-indigo-600 text-white text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-indigo-700 border-indigo-600 pointer-coarse:min-h-11">Create recipe</button><button type="button"  @click="importOpen=true" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Import recipe</button><button v-if="error" type="button"  @click="initialize" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Retry loading recipes</button></div></div>
        </main>
      </div>
      <input ref="importFile" type="file" accept="application/json,.json" hidden @change="importRecipe"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">
      <ImportRecipeDialog :cache-scope="tagCacheScope" :open="importOpen" :api="api" @close="importOpen=false" @imported="useImported" @file="importFile?.click()"/>
      <ShareRecipeDialog :open="shareOpen" :recipe-id="selected" :dirty="dirty||current?.jsonDirty" :api="api" @close="shareOpen=false" @run="shareOpen=false;view='run'" @save-run="saveAndRun" @shared="shareChanged"/>
      <AiRecipeDialog :open="aiOpen" :origin="aiOrigin" :models="ctx.state.models" :default-model="ctx.state.selectedModel" :api="api" @close="aiOpen=false" @apply="applyAI"/>
      <SaveExampleDialog :draft="exampleDraft" :api="api" @close="exampleDraft=null" @save="commitExample"/>
      <dialog ref="confirmDialog"  @cancel="confirmation=null" aria-labelledby="jev-confirm-title" data-jev-dialog class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 fixed inset-0 m-auto border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 rounded-2xl shadow-xl p-6.5 max-h-[85vh] overflow-auto w-[min(540px,_calc(100%_-_32px))] text-sm backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)] max-[620px]:p-5"><div v-if="confirmation"><h2 id="jev-confirm-title"  class="mb-3 text-[21px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">{{confirmation.title}}</h2><p class="leading-[1.7]">{{confirmation.message}}</p><div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-5"><button type="button" @click="closeConfirm" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Keep it</button><button type="button" @click="confirmAction" data-jev-button data-jev-danger class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-red-700 dark:text-red-300 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">{{confirmation.action}}</button></div></div></dialog>
      <dialog ref="historyDialog"  aria-labelledby="jev-history-title" data-jev-history-dialog class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 fixed inset-0 m-auto border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 rounded-2xl shadow-xl p-6.5 max-h-[85vh] overflow-auto w-[min(720px,_calc(100%_-_32px))] text-sm backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)] max-[620px]:p-5"><div v-if="historical"><div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><div class="pr-8"><span data-jev-eyebrow class="text-[10px] font-semibold tracking-[1.1px] text-slate-500 dark:text-slate-400 mb-1.5">RECORDED SNAPSHOT</span><h2 id="jev-history-title"  class="mb-3 text-[21px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">{{historical.name}}</h2></div><button type="button" aria-label="Close history detail" @click="historyDialog.close()" data-jev-icon-button class="absolute top-2 right-2 grid place-items-center size-8 rounded-lg cursor-pointer text-slate-400 hover:bg-slate-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:size-11"><StudioIcon name="close" class="size-5"/></button></div><div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-5"><button type="button" @click="replay(historical)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Use recipe &amp; inputs</button><button type="button" @click="exportRun(historical)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Export record</button><button type="button" :disabled="activeRun(historical)" @click="deleteHistoryRun" data-jev-button data-jev-danger class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-red-700 dark:text-red-300 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Delete record</button></div><StudioNotice v-if="historical.error" tone="error" :message="historical.error"/><details data-jev-criteria class="text-xs text-slate-500 dark:text-slate-400 mt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Recorded input</summary><JsonBlock :text="JSON.stringify(historical.input,null,2)"/></details><DecisionResults :run="historical" :pending="activeRun(historical)?historical:null" @cancel="cancel" @export="exportRun"/></div></dialog>
    </div>`,
    setup() {
        const ctx = inject('ctx'),
            ext = ctx.scope('jev'),
            publishExt = ctx.scope('share_llmspy'),
            api = createApi(ext)
        const tagCacheScope = computed(() =>
            JSON.stringify([
                ctx.ai?.auth?.userId || ctx.ai?.auth?.userName || 'default',
                publishExt.state.publish?.baseUrl || 'https://ai.llmspy.org',
            ]),
        )
        const owner =
            (ctx.ai.base || location.origin) +
            '::' +
            (ctx.ai.auth?.userName || ctx.ai.auth?.userId || 'default')
        const drafts = createDraftStore(owner),
            records = reactive({}),
            items = ref([]),
            selected = ref(''),
            view = ref('run'),
            status = reactive({
                available: false,
                message: 'Checking connection…',
                models: ['~typesafe/jev-latest'],
            })
        const loading = ref(true),
            busy = ref(false),
            error = ref(''),
            notice = ref(''),
            noticeTone = ref('info'),
            history = reactive({ items: [], cursor: null }),
            historyLoading = ref(false),
            historical = ref(null)
        const importOpen = ref(false),
            shareOpen = ref(false)
        const aiOpen = ref(false),
            aiOrigin = ref(null),
            confirmation = ref(null),
            exampleDraft = ref(null),
            checking = ref(''),
            exampleResults = reactive({})
        const resultsPanel = ref(null)
        const importFile = ref(null),
            confirmDialog = ref(null),
            historyDialog = ref(null)
        let disposed = false,
            saveTimer,
            statusRequest,
            selectionToken = 0,
            checkToken = 0
        const current = computed(() => records[selected.value] || null)
        const dirty = computed(
            () =>
                current.value &&
                JSON.stringify(current.value.recipe) !==
                    current.value.savedDocument,
        )
        const favourite = computed(
            () => items.value.find((i) => i.id === selected.value)?.favourite,
        )
        const errors = computed(() =>
            current.value
                ? inputErrors(
                      current.value.recipe.inputSchema,
                      current.value.input,
                  )
                : [],
        )
        const formStatus = computed(() => {
            const visible = errors.value.filter(
                (e) => e.message !== 'This field is required.',
            )
            return visible.length
                ? { errors: visible, message: 'Complete the required fields.' }
                : null
        })
        const canRun = computed(
            () =>
                !!current.value &&
                status.available &&
                !current.value.jsonDirty &&
                !current.value.submission &&
                !errors.value.length,
        )
        const runHelp = computed(() => {
            if (!current.value) return ''
            if (activeRun(current.value.pending))
                return 'Wait for this decision to finish or cancel it.'
            if (current.value.submission)
                return 'Use Retry submission above to recover the earlier decision.'
            if (current.value.jsonDirty)
                return 'Apply or discard your Recipe JSON changes first.'
            if (!status.available) return status.message
            if (errors.value.length)
                return errors.value
                    .map((e) => `${label(e.fieldName)}: ${e.message}`)
                    .join(' ')
            return ''
        })
        const requestJSON = computed(() =>
            current.value
                ? JSON.stringify(
                      compile(current.value.recipe, current.value.input),
                      null,
                      2,
                  )
                : '',
        )
        const stale = computed(
            () =>
                current.value?.result &&
                (JSON.stringify({ ...current.value.recipe, examples: [] }) !==
                    JSON.stringify({
                        ...current.value.result.recipe,
                        examples: [],
                    }) ||
                    JSON.stringify(current.value.input) !==
                        JSON.stringify(current.value.result.input)),
        )
        const library = computed(() => [
            ...items.value,
            ...Object.values(records)
                .filter((r) => r.id.startsWith('local:'))
                .map((r) => ({
                    id: r.id,
                    local: true,
                    name: r.recipe.name,
                    description: r.recipe.description,
                    tags: r.recipe.tags,
                    content: r.recipe.content,
                    questionCount: Object.keys(r.recipe.questions).length,
                })),
        ])
        const tabs = [
            { id: 'run', label: 'Run', icon: 'play' },
            { id: 'edit', label: 'Edit', icon: 'file' },
            { id: 'examples', label: 'Examples', icon: 'check' },
            { id: 'history', label: 'History', icon: 'clock' },
        ]
        function notify(message, tone = 'info') {
            noticeTone.value = tone
            notice.value = message
        }
        async function attempt(fn) {
            error.value = ''
            try {
                return await fn()
            } catch (e) {
                if (!disposed) error.value = e.message
                return null
            }
        }
        function saveDraft(record) {
            const snapshot = clone(record)
            // History stays on the server; browser recovery retains references rather than cached responses.
            for (const field of ['result', 'pending'])
                if (snapshot[field]?.id)
                    snapshot[field] = {
                        id: snapshot[field].id,
                        status: snapshot[field].status,
                        error: snapshot[field].error,
                    }
            return drafts.save(record.id, snapshot)
        }
        function persist() {
            clearTimeout(saveTimer)
            saveTimer = setTimeout(() => {
                for (const record of Object.values(records)) saveDraft(record)
                if (drafts.error()) notify(drafts.error(), 'warning')
            }, 200)
        }
        function recordFrom(row) {
            return {
                id: row.id,
                filename: row.filename,
                revision: row.revision,
                importSource: row.importSource,
                publication: row.publication,
                recipe: clone(row.document),
                savedDocument: JSON.stringify(row.document),
                input: defaults(row.document.inputSchema),
                json: JSON.stringify(row.document, null, 2),
                jsonDirty: false,
                editVersion: 0,
                result: null,
                pending: null,
            }
        }
        async function refreshStatus() {
            if (disposed) return
            if (statusRequest) return statusRequest
            statusRequest = attempt(async () => {
                const result = await api('/status')
                if (!disposed) Object.assign(status, result)
            })
            try {
                await statusRequest
            } finally {
                statusRequest = null
            }
        }
        function refreshConnection() {
            if (!document.hidden) refreshStatus()
        }
        async function refreshLibrary() {
            items.value = (await api('/recipes')).items
        }
        async function restore(record, hydrate = true) {
            if (hydrate)
                for (const field of ['result', 'pending']) {
                    const reference = record[field]
                    if (!reference?.id || reference.recipe) continue
                    try {
                        const run =
                            record.result?.id === reference.id &&
                            record.result.recipe
                                ? record.result
                                : await api(
                                      '/runs/' +
                                          encodeURIComponent(reference.id),
                                  )
                        if (!disposed) record[field] = run
                    } catch (e) {
                        if (!disposed) {
                            record[field] = null
                            if (e.status !== 404)
                                notify(
                                    'Could not restore the earlier decision. Open history to reconnect.',
                                    'warning',
                                )
                        }
                    }
                }
            if (activeRun(record.pending)) {
                if (record.pending.id) tracker.follow(record.pending, record.id)
                else
                    record.pending = {
                        status: 'interrupted',
                        error: 'Submission was interrupted. Retry it to recover its status.',
                    }
            }
            return record
        }
        async function select(identity) {
            if (!identity) return
            if (selected.value !== identity) view.value = 'run'
            selected.value = identity
            const token = ++selectionToken
            error.value = ''
            if (!records[identity])
                await attempt(async () => {
                    const row = await api(
                            '/recipes/' + encodeURIComponent(identity),
                        ),
                        saved = await drafts.load(identity)
                    if (disposed) return
                    records[identity] = {
                        ...recordFrom(row),
                        ...(saved || {}),
                        id: identity,
                    }
                })
            if (records[identity]) await restore(records[identity])
            if (token === selectionToken && records[identity]) persist()
        }
        function fresh() {
            const id = 'local:' + uid(),
                row = { id, document: blankRecipe(), revision: 0 }
            records[id] = recordFrom(row)
            records[id].savedDocument = ''
            selected.value = id
            view.value = 'edit'
            persist()
        }
        async function initialize() {
            loading.value = true
            await attempt(async () => {
                await Promise.all([refreshLibrary(), refreshStatus()])
                for (const draft of await drafts.list()) {
                    const identity = items.value.some(
                        (item) => item.id === draft.key,
                    )
                        ? draft.key
                        : items.value.find((item) =>
                              item.previousIds?.includes(draft.key),
                          )?.id || draft.key
                    const saved = items.value.find(
                        (item) => item.id === identity,
                    )
                    records[identity] = {
                        ...draft.value,
                        id: identity,
                        filename: saved?.filename || draft.value.filename,
                    }
                    if (identity !== draft.key) {
                        if (
                            records[identity].submission?.recipeId === draft.key
                        )
                            records[identity].submission.recipeId = identity
                        await drafts.remove(draft.key)
                    }
                    await restore(records[identity], false)
                }
                await select(
                    selected.value ||
                        items.value[0]?.id ||
                        Object.keys(records)[0] ||
                        '',
                )
                await loadHistory()
                for (const run of history.items.filter(activeRun)) {
                    const full = await api(
                        '/runs/' + encodeURIComponent(run.id),
                    )
                    tracker.follow(full, full.recipeId)
                }
            })
            loading.value = false
        }
        function updateInput(value) {
            current.value.input = clone(value)
            persist()
        }
        function updateRecipe(doc, record = current.value) {
            if (!record) return
            doc = clearChangedExampleResults(record.recipe, doc)
            const old = record.recipe.inputSchema.properties,
                newProps = doc.inputSchema.properties
            const removed = Object.keys(old).filter((k) => !(k in newProps)),
                added = Object.keys(newProps).filter((k) => !(k in old))
            if (removed.length === 1 && added.length === 1)
                record.input[added[0]] = record.input[removed[0]]
            record.input = {
                ...defaults(doc.inputSchema),
                ...Object.fromEntries(
                    Object.entries(record.input).filter(
                        ([key]) => key in newProps,
                    ),
                ),
            }
            record.recipe = clone(doc)
            record.json = JSON.stringify(doc, null, 2)
            record.jsonDirty = false
            record.editVersion++
            persist()
        }
        function updateModel(model) {
            const doc = clone(current.value.recipe)
            doc.decisionModel = model
            updateRecipe(doc)
        }
        function setJSON(value) {
            current.value.json = value
            current.value.jsonDirty =
                value !== JSON.stringify(current.value.recipe, null, 2)
            current.value.editVersion++
            persist()
        }
        function discardJSON() {
            current.value.json = JSON.stringify(current.value.recipe, null, 2)
            current.value.jsonDirty = false
            persist()
        }
        async function applyJSON() {
            const record = current.value,
                version = record.editVersion
            busy.value = true
            await attempt(async () => {
                const parsed = clearChangedExampleResults(
                        record.recipe,
                        JSON.parse(record.json),
                    ),
                    result = await api('/validate', 'POST', { recipe: parsed })
                if (record.editVersion !== version)
                    throw Error(
                        'The draft changed while validation was running. Apply it again.',
                    )
                updateRecipe(result.recipe, record)
                notify('Recipe JSON applied.', 'success')
            })
            busy.value = false
        }
        async function validate() {
            busy.value = true
            await attempt(async () => {
                await api('/validate', 'POST', {
                    recipe: clone(current.value.recipe),
                })
                notify('Recipe is valid and ready to test.', 'success')
            })
            busy.value = false
        }
        async function save(asCopy = false) {
            if (busy.value || current.value?.jsonDirty) return
            const record = current.value,
                snapshot = clone(record.recipe),
                version = record.editVersion
            if (asCopy) snapshot.name = copyName(snapshot.name)
            busy.value = true
            await attempt(async () => {
                const creating = asCopy || record.id.startsWith('local:'),
                    saved = await api(
                        creating
                            ? '/recipes'
                            : '/recipes/' + encodeURIComponent(record.id),
                        creating ? 'POST' : 'PUT',
                        {
                            document: snapshot,
                            revision: record.revision,
                            ...(creating
                                ? {
                                      filename: asCopy
                                          ? copyFilename(
                                                record.id.startsWith('local:')
                                                    ? record.filename ||
                                                          record.recipe.name +
                                                              '.json'
                                                    : record.filename,
                                            )
                                          : record.filename,
                                  }
                                : {}),
                        },
                    )
                if (creating) {
                    const next = recordFrom(saved)
                    next.input = clone(record.input)
                    next.result = clone(record.result)
                    const moving =
                        record.id.startsWith('local:') &&
                        !asCopy &&
                        record.editVersion === version
                    if (moving) {
                        next.pending = clone(record.pending)
                        next.submission = record.submission
                            ? clone(record.submission)
                            : null
                        tracker.retarget(record.id, saved.id)
                    }
                    records[saved.id] = next
                    if (selected.value === record.id) selected.value = saved.id
                    if (moving) {
                        delete records[record.id]
                        await drafts.remove(record.id)
                    }
                } else {
                    record.savedDocument = JSON.stringify(snapshot)
                    record.revision = saved.revision
                }
                await refreshLibrary()
                persist()
                ctx.toast(
                    record.editVersion === version
                        ? 'Recipe saved.'
                        : 'Recipe saved; your newer edits are still in the original draft.',
                )
            })
            busy.value = false
        }
        function duplicateRecipe() {
            const source = current.value,
                id = 'local:' + uid(),
                record = recordFrom({
                    id,
                    revision: 0,
                    document: source.recipe,
                })
            record.recipe.name = copyName(source.recipe.name)
            record.filename = copyFilename(
                source.id.startsWith('local:')
                    ? record.recipe.name + '.json'
                    : source.filename,
            )
            record.json = JSON.stringify(record.recipe, null, 2)
            record.input = clone(source.input)
            record.savedDocument = ''
            records[id] = record
            selected.value = id
            view.value = 'edit'
            persist()
        }
        async function toggleFavourite() {
            const identity = selected.value,
                enabled = !favourite.value
            await attempt(async () => {
                await api(
                    '/favourites/' + encodeURIComponent(identity),
                    'PUT',
                    {
                        enabled,
                    },
                )
                await refreshLibrary()
            })
        }
        function loadExample(example) {
            current.value.input = clone(example.input)
            current.value.exampleId = example.id
            view.value = 'run'
            persist()
        }
        function clearInput() {
            current.value.input = defaults(current.value.recipe.inputSchema)
            persist()
        }
        const tracker = createRunTracker(api, (run, origin) => {
            if (historical.value?.id === run.id) historical.value = run
            const record = records[origin]
            if (!record || (record.pending?.id && record.pending.id !== run.id))
                return
            record.pending = run
            if (run.status === 'succeeded') record.result = run
            persist()
        })
        function submission(
            record,
            input = record.input,
            recipe = record.recipe,
        ) {
            return {
                recipe: clone(recipe),
                input: clone(input),
                submissionId: uid(),
                recipeId: record.id.startsWith('local:') ? null : record.id,
            }
        }
        async function submit(
            record,
            input = record.input,
            recipe = record.recipe,
        ) {
            return api('/runs', 'POST', submission(record, input, recipe))
        }
        async function retrySubmission() {
            const record = current.value
            if (!record?.submission || activeRun(record.pending)) return
            record.pending = { status: 'pending', id: null }
            persist()
            nextTick(() => {
                if (
                    !disposed &&
                    current.value === record &&
                    view.value === 'run' &&
                    matchMedia('(max-width:850px)').matches
                )
                    resultsPanel.value?.scrollIntoView({
                        block: 'start',
                        behavior: matchMedia('(prefers-reduced-motion:reduce)')
                            .matches
                            ? 'auto'
                            : 'smooth',
                    })
            })
            try {
                const run = await api('/runs', 'POST', clone(record.submission))
                record.submission = null
                if (!disposed) tracker.follow(run, record.id)
            } catch (e) {
                record.pending = { status: 'failed', error: e.message }
                if ([400, 401, 403, 404, 409, 429].includes(e.status))
                    record.submission = null
                persist()
            }
        }
        async function runCurrent() {
            if (!canRun.value || activeRun(current.value.pending)) return
            current.value.submission = submission(current.value)
            await retrySubmission()
        }
        async function cancel(run) {
            if (!run?.id) return
            await attempt(async () => {
                const result = await api(
                    '/runs/' + encodeURIComponent(run.id) + '/cancel',
                    'POST',
                    {},
                )
                for (const record of Object.values(records))
                    if (record.pending?.id === run.id) {
                        record.pending = result
                        persist()
                    }
                if (historical.value?.id === run.id) historical.value = result
            })
        }
        function reconnect() {
            if (current.value.pending?.id)
                tracker.follow(current.value.pending, selected.value)
        }
        async function loadHistory(more = false) {
            historyLoading.value = true
            await attempt(async () => {
                const result = await api(
                    '/runs' +
                        (more && history.cursor
                            ? '?cursor=' + encodeURIComponent(history.cursor)
                            : ''),
                )
                history.items = more
                    ? [...history.items, ...result.items]
                    : result.items
                history.cursor = result.cursor
            })
            historyLoading.value = false
        }
        async function showHistory(id) {
            await attempt(async () => {
                historical.value = await api('/runs/' + encodeURIComponent(id))
                historyDialog.value.showModal()
                if (activeRun(historical.value))
                    tracker.follow(historical.value, historical.value.recipeId)
            })
        }
        function replay(run) {
            const id = 'local:' + uid(),
                record = recordFrom({ id, revision: 0, document: run.recipe })
            record.savedDocument = ''
            record.input = clone(run.input)
            record.result = clone(run)
            records[id] = record
            selected.value = id
            view.value = 'run'
            historyDialog.value.close()
            persist()
        }
        async function deleteHistoryRun() {
            await attempt(async () => {
                const id = historical.value.id
                await api('/runs/' + encodeURIComponent(id), 'DELETE')
                for (const record of Object.values(records)) {
                    if (record.result?.id === id) record.result = null
                    if (record.pending?.id === id) record.pending = null
                }
                historical.value = null
                persist()
                historyDialog.value.close()
                await loadHistory()
            })
        }
        function confirm(title, message, action, fn) {
            confirmation.value = { title, message, action, fn }
            confirmDialog.value.showModal()
        }
        function closeConfirm() {
            confirmDialog.value.close()
            confirmation.value = null
        }
        async function confirmAction() {
            const fn = confirmation.value.fn
            closeConfirm()
            await attempt(fn)
        }
        function askReload() {
            const record = current.value
            confirm(
                'Reload saved recipe?',
                'Unsaved recipe edits and input will be replaced with the saved version.',
                'Reload recipe',
                async () => {
                    const row = await api(
                            '/recipes/' + encodeURIComponent(record.id),
                        ),
                        next = recordFrom(row)
                    next.pending = record.pending
                    next.result = record.result
                    next.submission = record.submission
                    await drafts.remove(record.id)
                    records[record.id] = next
                    persist()
                },
            )
        }
        function askDelete() {
            const record = current.value
            confirm(
                'Delete recipe?',
                'Recorded decision snapshots will remain in history. An existing public share stays available; use Stop sharing or My recipes to remove it.',
                'Delete recipe',
                async () => {
                    if (!record.id.startsWith('local:'))
                        await api(
                            '/recipes/' + encodeURIComponent(record.id),
                            'DELETE',
                        )
                    await drafts.remove(record.id)
                    delete records[record.id]
                    await refreshLibrary()
                    selected.value = ''
                    await select(items.value[0]?.id || '')
                },
            )
        }
        function askClearHistory() {
            confirm(
                'Clear decision history?',
                'This deletes completed records and their stored inputs. Active decisions are kept.',
                'Clear history',
                async () => {
                    await api('/history', 'DELETE')
                    for (const record of Object.values(records)) {
                        record.result = null
                        if (!activeRun(record.pending)) record.pending = null
                    }
                    await loadHistory()
                    persist()
                },
            )
        }
        function download(value, name) {
            const blob = new Blob([JSON.stringify(value, null, 2)], {
                    type: 'application/json',
                }),
                url = URL.createObjectURL(blob),
                a = document.createElement('a')
            a.href = url
            a.download = name
            a.click()
            setTimeout(() => URL.revokeObjectURL(url), 1000)
        }
        function exportRecipe() {
            download(
                current.value.recipe,
                current.value.id.startsWith('local:')
                    ? current.value.filename ||
                          current.value.recipe.name + '.json'
                    : current.value.filename,
            )
        }
        function exportRun(run) {
            download(run, 'decision-' + run.id + '.json')
        }
        async function copy(value) {
            await attempt(async () => {
                if (!navigator.clipboard)
                    throw Error(
                        'Clipboard is unavailable. Select and copy the displayed JSON.',
                    )
                await navigator.clipboard.writeText(value)
                notify('Copied to clipboard.')
            })
        }
        function copyCurl() {
            const payload = requestJSON.value.replaceAll("'", "'\"'\"'")
            copy(
                'curl https://openrouter.ai/api/alpha/decisions \\\n  -H "Authorization: Bearer $OPENROUTER_API_KEY" \\\n  -H "Content-Type: application/json" \\\n  -d \'' +
                    payload +
                    "'",
            )
        }
        async function useImported(row, activate = true) {
            const token = selectionToken
            await attempt(async () => {
                await drafts.remove(row.id)
                records[row.id] = recordFrom(row)
                delete exampleResults[row.id]
                await refreshLibrary()
                await loadHistory()
                if (!activate || token !== selectionToken) return
                await select(row.id)
                view.value = 'run'
                notice.value = ''
            })
        }
        async function importRecipe(event) {
            const file = event.target.files?.[0]
            event.target.value = ''
            if (!file) return
            await attempt(async () => {
                if (file.size > 524288)
                    throw Error('Choose a recipe file under 512 KB.')
                const document = JSON.parse(await file.text())
                await importDocument(
                    '/recipes',
                    { document, filename: file.name },
                    true,
                )
            })
        }
        function copyName(name) {
            const names = new Set(
                library.value.map((item) => item.name.toLowerCase()),
            )
            let suffix = ' (copy)',
                count = 2
            while (names.has((name.slice(0, 100) + suffix).toLowerCase()))
                suffix = ` (copy ${count++})`
            return name.slice(0, 100) + suffix
        }
        function copyFilename(filename) {
            const ids = new Set(
                library.value
                    .filter((item) => !item.local)
                    .map((item) => item.id.toLowerCase()),
            )
            for (const record of Object.values(records))
                if (record.filename)
                    ids.add(record.filename.slice(0, -5).toLowerCase())
            let stem =
                filename
                    .slice(0, -5)
                    .replace(/[<>:"/\\|?*\x00-\x1f]/g, '-')
                    .trim() || 'recipe'
            while (new TextEncoder().encode(stem).length > 220)
                stem = Array.from(stem).slice(0, -1).join('')
            let suffix = ' (copy)',
                count = 2
            while (ids.has((stem + suffix).toLowerCase()))
                suffix = ` (copy ${count++})`
            return stem + suffix + '.json'
        }
        function askReplace(existing, fn) {
            confirm(
                'Replace existing recipe?',
                `The recipe "${existing.id}" already exists. Importing will overwrite it and clear all of its old history and saved input. Any existing public share stays available and can be removed separately in My recipes.`,
                'Replace recipe & clear history',
                fn,
            )
        }
        async function importDocument(path, body, edit = false) {
            try {
                const saved = await api(path, 'POST', body)
                importOpen.value = false
                await useImported(saved)
                if (edit) view.value = 'edit'
            } catch (e) {
                if (e.code !== 'RecipeExistsError' || !e.existingRecipe) throw e
                askReplace(e.existingRecipe, () =>
                    importDocument(
                        path,
                        { ...body, replaceRevision: e.existingRecipe.revision },
                        edit,
                    ),
                )
            }
        }
        function openAI(improve) {
            aiOrigin.value = {
                key: selected.value,
                version: current.value?.editVersion,
                recipe: current.value ? clone(current.value.recipe) : null,
                input: clone(current.value?.input || {}),
                improve,
            }
            aiOpen.value = true
        }
        function applyAI(doc) {
            const origin = aiOrigin.value,
                record = records[origin.key]
            aiOpen.value = false
            if (
                origin.improve &&
                record &&
                record.editVersion === origin.version
            ) {
                record.recipe = clone(doc)
                record.json = JSON.stringify(doc, null, 2)
                record.jsonDirty = false
                record.input = {
                    ...defaults(doc.inputSchema),
                    ...Object.fromEntries(
                        Object.entries(record.input).filter(
                            ([key]) => key in doc.inputSchema.properties,
                        ),
                    ),
                }
                record.editVersion++
                selected.value = record.id
            } else {
                const id = 'local:' + uid(),
                    next = recordFrom({ id, revision: 0, document: doc })
                next.savedDocument = ''
                records[id] = next
                selected.value = id
                if (origin.improve && record?.editVersion !== origin.version)
                    notify(
                        'The original recipe changed. AI changes were kept as a separate draft.',
                        'warning',
                    )
            }
            view.value = 'edit'
            persist()
        }
        function canSaveExample(run) {
            return (
                !!current.value &&
                !current.value.jsonDirty &&
                !activeRun(current.value.pending) &&
                canSaveRunExample(current.value.recipe, run)
            )
        }
        function saveExample(run = current.value?.result) {
            if (!canSaveExample(run)) return
            exampleDraft.value = {
                key: selected.value,
                version: current.value.editVersion,
                recipe: clone(current.value.recipe),
                run: clone(run),
            }
        }
        function commitExample(name) {
            const draft = exampleDraft.value,
                record = draft && records[draft.key]
            if (!record || current.value !== record || record.editVersion !== draft.version || !canSaveExample(draft.run)) {
                exampleDraft.value = null
                notify('The recipe changed. Reopen Save as example for the current recipe.', 'warning')
                return
            }
            const doc = clone(record.recipe)
            doc.examples ||= []
            doc.examples.push(exampleFromRun(doc, draft.run, name))
            updateRecipe(doc)
            exampleDraft.value = null
            view.value = 'examples'
            notify('Recorded input and results added to this draft. Save the recipe to keep the example.')
        }
        function renameExample({ id, label }) {
            const doc = clone(current.value.recipe),
                example = doc.examples.find((item) => item.id === id)
            if (!example || !label.trim() || label.trim().length > 120) return
            example.label = label.trim()
            updateRecipe(doc)
        }
        function removeExample(id) {
            const doc = clone(current.value.recipe)
            doc.examples = doc.examples.filter((e) => e.id !== id)
            updateRecipe(doc)
        }
        async function checkExamples() {
            if (checking.value || !current.value?.recipe.examples?.length)
                return
            const record = current.value,
                recipe = clone(record.recipe),
                token = ++checkToken
            checking.value = record.id
            exampleResults[record.id] = {}
            try {
                await api('/validate', 'POST', { recipe })
                for (const example of recipe.examples) {
                    if (token !== checkToken || disposed) break
                    let run = await submit(record, example.input, recipe)
                    while (
                        activeRun(run) &&
                        token === checkToken &&
                        !disposed
                    ) {
                        await new Promise((resolve) => setTimeout(resolve, 700))
                        if (token !== checkToken || disposed) break
                        run = await api('/runs/' + encodeURIComponent(run.id))
                    }
                    if (token !== checkToken || disposed) {
                        if (activeRun(run) && !disposed)
                            await api(
                                '/runs/' +
                                    encodeURIComponent(run.id) +
                                    '/cancel',
                                'POST',
                                {},
                            )
                        break
                    }
                    exampleResults[record.id][example.id] = run
                }
            } catch (e) {
                if (!disposed) error.value = e.message
            } finally {
                if (token === checkToken) checking.value = ''
            }
        }
        function stopChecks() {
            checkToken++
            checking.value = ''
            notify(
                'Stopping example checks. Completed calls remain in history.',
            )
        }
        watch(view, (value) => {
            if (value === 'history') loadHistory()
        })
        watch(
            () => ctx.state.config?.status?.enabled?.includes('openrouter'),
            refreshStatus,
        )
        onMounted(() => {
            window.addEventListener('focus', refreshConnection)
            window.addEventListener('online', refreshConnection)
            document.addEventListener('visibilitychange', refreshConnection)
            initialize()
        })
        onUnmounted(() => {
            disposed = true
            window.removeEventListener('focus', refreshConnection)
            window.removeEventListener('online', refreshConnection)
            document.removeEventListener('visibilitychange', refreshConnection)
            checkToken++
            tracker.dispose()
            clearTimeout(saveTimer)
            for (const record of Object.values(records)) saveDraft(record)
            drafts.flush()
        })
        async function shareChanged(identity) {
            const record = records[identity]
            try {
                const saved = await api(
                    '/recipes/' + encodeURIComponent(identity),
                )
                if (records[identity] === record && record)
                    record.publication = saved.publication || null
            } catch (e) {
                if (e.status !== 404) notify(e.message, 'warning')
            }
            await refreshLibrary()
        }
        async function saveAndRun() {
            const record = current.value
            shareOpen.value = false
            if (record?.jsonDirty) await applyJSON()
            if (current.value !== record) return
            await save()
            if (current.value !== record) return
            view.value = 'run'
            if (!dirty.value && !record?.jsonDirty) await runCurrent()
        }
        async function getPublishConfig() {
            let pub = publishExt.state.publish
            if (!pub || pub.apiKey === undefined) {
                try {
                    const api = await publishExt.getJson('/config.json')
                    if (api.response) {
                        publishExt.setState({ publish: api.response })
                        pub = api.response
                    }
                } catch (e) {
                    console.warn('Failed to load publish config', e)
                }
            }
            return pub
        }
        async function shareRecipe() {
            if (busy.value || current.value?.jsonDirty) return
            const cfg = await getPublishConfig()
            const isConnected = !!(cfg?.userName && cfg?.apiKey)
            if (!isConnected) {
                ctx.toggleTop('SharePanel', true)
                return
            }
            if (current.value.id.startsWith('local:')) {
                await save()
                if (current.value.id.startsWith('local:')) return
            }
            shareOpen.value = true
        }
        return {
            shareOpen,
            shareRecipe,
            saveAndRun,
            shareChanged,
            ctx,
            api,
            tagCacheScope,
            selected,
            view,
            status,
            current,
            dirty,
            favourite,
            errors,
            formStatus,
            canRun,
            runHelp,
            stale,
            requestJSON,
            library,
            tabs,
            loading,
            busy,
            error,
            notice,
            noticeTone,
            history,
            historyLoading,
            historical,
            importOpen,
            useImported,
            aiOpen,
            aiOrigin,
            confirmation,
            exampleDraft,
            api,
            checking,
            exampleResults,
            resultsPanel,
            importFile,
            confirmDialog,
            historyDialog,
            label,
            activeRun,
            initialize,
            select,
            fresh,
            refreshStatus,
            updateInput,
            updateRecipe,
            updateModel,
            setJSON,
            discardJSON,
            applyJSON,
            validate,
            save,
            duplicateRecipe,
            toggleFavourite,
            loadExample,
            clearInput,
            runCurrent,
            retrySubmission,
            cancel,
            reconnect,
            loadHistory,
            showHistory,
            replay,
            deleteHistoryRun,
            closeConfirm,
            confirmAction,
            askReload,
            askDelete,
            askClearHistory,
            exportRecipe,
            exportRun,
            copy,
            copyCurl,
            importRecipe,
            openAI,
            applyAI,
            saveExample,
            commitExample,
            renameExample,
            canSaveExample,
            removeExample,
            checkExamples,
            stopChecks,
        }
    },
}
