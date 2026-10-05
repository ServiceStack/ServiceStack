import { ref, computed, nextTick, onMounted, onUnmounted, watch, inject, useId } from 'vue'
import { modalityIcons, searchIcon, sortIcon, starIcon } from './modelPickerIcons.mjs'

const sorts = [
    { id: 'release_date', label: 'Release date' },
    { id: 'name', label: 'Name' },
    { id: 'knowledge', label: 'Knowledge cutoff' },
    { id: 'last_updated', label: 'Last updated' },
    { id: 'cost_input', label: 'Input price' },
    { id: 'cost_output', label: 'Output price' },
    { id: 'context', label: 'Context limit' },
]
const name = (m) => m.display_name || m.name || m.id || ''
const number = (value) =>
    value == null || value === '' || !Number.isFinite(Number(value)) ? null : Number(value)
const price = (value) =>
    number(value) == null
        ? '—'
        : new Intl.NumberFormat(undefined, {
              style: 'currency',
              currency: 'USD',
              maximumFractionDigits: 4,
          }).format(Number(value))
const short = (value) =>
    new Intl.NumberFormat(undefined, { notation: 'compact', maximumFractionDigits: 1 }).format(value)

export default {
    props: {
        modelValue: String,
        models: Array,
        disabled: Boolean,
        open: { type: Boolean, default: undefined },
        hideTrigger: Boolean,
        provider: String,
        providerLabel: String,
        allowedProviders: Array,
        inputModalities: Array,
        outputModalities: Array,
        requireTools: Boolean,
        requireReasoning: Boolean,
        minimumContext: Number,
        modelFilter: Function,
        initialSearch: { type: String, default: '' },
        valueKey: { type: String, default: 'name' },
        title: { type: String, default: 'Select a model' },
        helpText: { type: String, default: 'Browse models from your enabled providers.' },
        triggerLabel: { type: String, default: 'Choose model' },
        placeholder: String,
        favorites: { type: Array, default: undefined },
        showModalities: { type: Boolean, default: true },
    },
    emits: ['update:modelValue', 'update:open', 'select', 'close', 'toggle-favorite'],
    template: `<div class="llms-model-picker">
      <button v-if="!hideTrigger" ref="trigger" type="button" class="llms-model-trigger" :aria-label="triggerLabel" aria-haspopup="dialog" :aria-expanded="opened" :disabled="disabled" @click="show">
        <ProviderIcon v-if="selected?.provider" :provider="selected.provider" class="llms-provider-icon"/>
        <span><strong>{{placeholder || (selected?name(selected):modelValue||'Choose a model')}}</strong><small v-if="selected?.provider">{{selected.provider}}</small></span>
        <svg class="llms-model-chevron" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
      </button>
      <Teleport to="body"><dialog ref="dialog" class="llms-model-dialog" :aria-labelledby="titleId" @keydown.escape.stop @cancel.stop.prevent="cancel" @click="backdrop">
        <header class="llms-model-header"><div><h2 :id="titleId">{{title}}</h2><p class="llms-help">{{helpText}}</p></div><slot name="header-actions"/><button class="llms-dialog-close" type="button" aria-label="Close model picker" @click="close"><svg viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/></svg></button></header>
        <div class="llms-model-filters">
          <div class="llms-model-toolbar">
            <div class="llms-model-search-provider">
              <div class="llms-model-search"><span class="llms-icon" aria-hidden="true" v-html="searchIcon"/><input ref="searchField" v-model="search" type="search" aria-label="Search models" placeholder="Search models…"/><button v-if="search" type="button" class="llms-model-clear" aria-label="Clear model search" @click="search=''" ><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button></div>
              <div ref="providerRoot" class="llms-model-provider-picker" @focusout="providerFocusOut">
                <button ref="providerTrigger" type="button" class="llms-model-provider-button" :aria-label="props.provider?'Provider fixed: '+providerName(props.provider):'Choose provider'" aria-haspopup="dialog" :aria-expanded="providerOpen" :disabled="!!props.provider" :title="props.provider?'Provider fixed to '+providerName(props.provider):'Filter by provider'" @click="toggleProviders">
                  <ProviderIcon :provider="props.provider||provider" class="llms-provider-icon"/><span>{{providerName(props.provider||provider)}}</span><svg v-if="!props.provider" class="llms-model-chevron" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
                </button>
                <div v-if="providerOpen" class="llms-model-provider-popup" :style="providerPopupStyle" role="dialog" :aria-labelledby="titleId+'-providers'" @keydown.escape.stop.prevent="closeProviders" @keydown.down.prevent="moveProviderFocus($event,1)" @keydown.up.prevent="moveProviderFocus($event,-1)">
                  <h3 :id="titleId+'-providers'" class="sr-only">Choose provider</h3>
                  <div class="llms-model-provider-search"><span class="llms-icon" aria-hidden="true" v-html="searchIcon"/><input ref="providerSearchField" v-model="providerSearch" type="search" aria-label="Search providers" placeholder="Search providers…"/></div>
                  <div class="llms-model-provider-list"><button v-if="!providerSearch.trim()" type="button" :aria-pressed="!provider" @click="chooseProvider('')"><ProviderIcon class="llms-provider-icon"/><span>All providers</span><small>{{catalog.length}}</small></button>
                    <button v-for="p in filteredProviders" :key="p" type="button" :aria-pressed="provider===p" @click="chooseProvider(p)"><ProviderIcon :provider="p" class="llms-provider-icon"/><span>{{providerName(p)}}</span><small>{{providerCounts[p]}}</small></button>
                    <p v-if="providerSearch.trim()&&!filteredProviders.length" class="llms-help">No providers match your search.</p>
                  </div>
                </div>
              </div>
            </div>
            <div v-if="showModalities" class="llms-model-modalities">
              <div v-if="inputChoices.length" class="llms-model-modality-group" role="group" aria-label="Input modalities"><div class="llms-model-modality-icons"><button v-for="type in inputChoices" :key="type" type="button" class="llms-model-modality" :aria-label="'Input: '+type+(inputLocked?' (required)':'')" :title="'Input: '+type+(inputLocked?' (required)':'')" :aria-pressed="inputSelection===type" :disabled="inputLocked" @click="inputModality=inputModality===type?'':type" v-html="modalityIcons[type]"/></div><span>input</span></div>
              <div v-if="outputChoices.length" class="llms-model-modality-group" role="group" aria-label="Output modalities"><div class="llms-model-modality-icons"><button v-for="type in outputChoices" :key="type" type="button" class="llms-model-modality" :aria-label="'Output: '+type+(outputLocked?' (required)':'')" :title="'Output: '+type+(outputLocked?' (required)':'')" :aria-pressed="outputSelection===type" :disabled="outputLocked" @click="outputModality=outputModality===type?'':type" v-html="modalityIcons[type]"/></div><span>output</span></div>
            </div>
            <div class="llms-model-sort"><label class="sr-only" :for="titleId+'-sort'">Sort models by</label><select :id="titleId+'-sort'" v-model="sort" aria-label="Sort models by"><option v-for="s in sorts" :key="s.id" :value="s.id">{{s.label}}</option></select><button type="button" class="llms-model-sort-direction" :class="{'is-descending':!ascending}" :aria-label="ascending?'Sort descending':'Sort ascending'" :title="ascending?'Ascending':'Descending'" @click="ascending=!ascending" v-html="sortIcon"/></div>
          </div>
          <div v-if="favorites" class="llms-model-provider-row">
            <button v-if="favorites" type="button" class="llms-model-favorites-filter" :aria-pressed="favoritesOnly" @click="favoritesOnly=!favoritesOnly"><span class="llms-icon" aria-hidden="true" v-html="starIcon"/> Favorites ({{favorites.length}})</button>
          </div>
        </div>
        <div ref="list" class="llms-model-list" @scroll.passive="onScroll"><div v-if="!filtered.length" class="llms-model-empty"><h3>{{catalog.length?'No models match your filters':'No models available'}}</h3><p class="llms-help">{{catalog.length?'Try a different search or filter.':'Enable a provider in Models & providers.'}}</p><button v-if="search||provider||favoritesOnly||inputModality||outputModality" class="llms-button" type="button" @click="clearFilters">Clear filters</button></div>
          <div v-else class="llms-model-grid"><article v-for="m in visible" :key="key(m)" class="llms-model-entry"><button type="button" class="llms-model-card" :aria-pressed="matches(m)" @click="choose(m)">
            <div class="llms-model-card-heading"><ProviderIcon v-if="m.provider" :provider="m.provider" class="llms-provider-icon"/><strong>{{name(m)}}</strong><span v-if="matches(m)" class="llms-icon" aria-hidden="true">✓</span></div><span class="llms-model-provider">{{m.provider||'Configured model'}}</span><code v-if="m.id">{{m.id}}</code>
            <div class="llms-model-tags"><span v-if="m.limit?.context" :title="Number(m.limit.context).toLocaleString()+' context tokens'">{{short(m.limit.context)}} context</span><span v-if="m.release_date">{{m.release_date}}</span><span v-if="free(m)" class="llms-model-free">Free</span><span v-else-if="m.cost?.input!=null||m.cost?.output!=null" :title="'Prices per million tokens: input '+price(m.cost?.input)+', output '+price(m.cost?.output)">{{price(m.cost?.input)}} / {{price(m.cost?.output)}}</span><span v-if="m.reasoning">Reasoning</span><span v-if="m.tool_call">Tools</span></div>
          </button><button v-if="favorites" type="button" class="llms-model-favorite" :aria-label="(isFavorite(m)?'Remove favorite: ':'Add favorite: ')+name(m)" :aria-pressed="isFavorite(m)" @click="$emit('toggle-favorite',m)"><span class="llms-icon" aria-hidden="true" v-html="starIcon"/></button></article></div>
          <div v-if="visible.length < filtered.length" class="llms-model-more"><button type="button" class="llms-button" @click="loadMore">Load 20 more</button></div>
          <div v-if="favoritesOnly && unavailable.length" class="llms-model-unavailable"><h3>Unavailable favorites</h3><p class="llms-help">Enable their providers to use these models.</p><div v-for="m in unavailable" :key="key(m)">{{m.provider}} · {{m.id}} <button type="button" class="llms-button" :aria-label="'Remove favorite: '+m.id" @click="$emit('toggle-favorite',m)">Remove</button></div></div>
        </div>
        <footer class="llms-model-footer"><span class="llms-help">{{visible.length}} of {{filtered.length}} models · prices per 1M tokens</span></footer>
      </dialog></Teleport>
    </div>`,
    setup(props, { emit }) {
        const ctx = inject('ctx', null)
        const titleId = 'model-picker-' + useId()
        const propsRef = props
        const modalities = (m, direction) => m.modalities?.[direction] || ['text']
        const supports = (m, direction, allowed) =>
            !allowed?.length || allowed.some((type) => modalities(m, direction).includes(type))
        const catalog = computed(() =>
            (props.models || ctx?.state.models || []).filter(
                (m) =>
                    (!props.provider || m.provider === props.provider) &&
                    (!props.allowedProviders || props.allowedProviders.includes(m.provider)) &&
                    supports(m, 'input', props.inputModalities) &&
                    supports(m, 'output', props.outputModalities) &&
                    (!props.requireTools || m.tool_call) &&
                    (!props.requireReasoning || m.reasoning) &&
                    (props.minimumContext == null || Number(m.limit?.context) >= props.minimumContext) &&
                    (!props.modelFilter || props.modelFilter(m)),
            ),
        )
        const choices = (direction, allowed) => {
            const types =
                allowed ||
                (direction === 'input'
                    ? ['text', 'image', 'audio', 'video', 'pdf']
                    : ['text', 'image', 'audio', 'video', 'speech'])
            return [...new Set(types)].filter(
                (type) =>
                    modalityIcons[type] &&
                    (allowed || catalog.value.some((m) => modalities(m, direction).includes(type))),
            )
        }
        const inputChoices = computed(() => choices('input', props.inputModalities))
        const outputChoices = computed(() => choices('output', props.outputModalities))
        const inputLocked = computed(() => props.inputModalities?.length === 1)
        const outputLocked = computed(() => props.outputModalities?.length === 1)
        const inputSelection = computed(() =>
            inputLocked.value ? props.inputModalities[0] : inputModality.value,
        )
        const outputSelection = computed(() =>
            outputLocked.value ? props.outputModalities[0] : outputModality.value,
        )
        const key = (m) => `${m.provider}:${m.id || m.name}`
        const favoritesOnly = ref(false),
            inputModality = ref(''),
            outputModality = ref('')
        const isFavorite = (m) => props.favorites?.includes(key(m)) || false
        const unavailable = computed(() => {
            const keys = new Set((props.models || ctx?.state.models || []).map(key))
            return (props.favorites || [])
                .filter((k) => !keys.has(k))
                .map((k) => {
                    const [provider, ...id] = k.split(':')
                    return { provider, id: id.join(':') }
                })
                .filter(
                    (m) =>
                        (!props.provider || m.provider === props.provider) &&
                        (!props.allowedProviders || props.allowedProviders.includes(m.provider)),
                )
        })
        const list = ref(null),
            limit = ref(20)
        const providerRoot = ref(null),
            providerTrigger = ref(null),
            providerSearchField = ref(null)
        const providerOpen = ref(false),
            providerSearch = ref('')
        const providerPopupStyle = ref({})
        let previousFocus
        const dialog = ref(null),
            trigger = ref(null),
            searchField = ref(null),
            opened = ref(false)
        const search = ref(''),
            provider = ref(''),
            sort = ref('release_date'),
            ascending = ref(false)
        const selected = computed(() =>
            catalog.value.find((m) => m.name === props.modelValue || m.id === props.modelValue),
        )
        const providers = computed(() =>
            [...new Set(catalog.value.map((m) => m.provider).filter(Boolean))].sort(),
        )
        const providerCounts = computed(() => {
            const counts = {}
            catalog.value.forEach((m) => (counts[m.provider] = (counts[m.provider] || 0) + 1))
            return counts
        })
        function providerName(id) {
            if (!id) return 'All providers'
            if (props.provider === id && props.providerLabel) return props.providerLabel
            const configured = ctx?.state.config?.providers?.find((p) => p.id === id)?.name
            return (
                configured ||
                {
                    google: 'Gemini',
                    openrouter: 'OpenRouter',
                    openai: 'OpenAI',
                    anthropic: 'Anthropic',
                    ollama: 'Ollama',
                    lmstudio: 'LM Studio',
                }[id] ||
                id
            )
        }
        const filteredProviders = computed(() => {
            const q = providerSearch.value.trim().toLowerCase()
            return providers.value.filter(
                (p) => !q || [p, providerName(p)].some((v) => v.toLowerCase().includes(q)),
            )
        })
        async function toggleProviders() {
            if (props.provider) return
            if (providerOpen.value) return closeProviders()
            providerOpen.value = true
            providerSearch.value = ''
            await nextTick()
            positionProviders()
            providerSearchField.value?.focus()
        }
        function positionProviders() {
            if (!providerOpen.value || !dialog.value || !providerTrigger.value) return
            const bounds = dialog.value.getBoundingClientRect()
            const button = providerTrigger.value.getBoundingClientRect()
            const below = bounds.bottom - button.bottom - 12
            const above = button.top - bounds.top - 12
            const upwards = below < 220 && above > below
            providerPopupStyle.value = {
                top: upwards ? 'auto' : 'calc(100% + 8px)',
                bottom: upwards ? 'calc(100% + 8px)' : 'auto',
                maxHeight: Math.max(0, upwards ? above : below) + 'px',
            }
        }
        function closeProviders(restoreFocus = true) {
            if (!providerOpen.value) return
            providerOpen.value = false
            if (restoreFocus) providerTrigger.value?.focus()
        }
        function chooseProvider(id) {
            if (props.provider || (id && !providers.value.includes(id))) return
            provider.value = id
            closeProviders()
        }
        function moveProviderFocus(event, step) {
            const items = [...providerRoot.value.querySelectorAll('.llms-model-provider-list button')]
            if (!items.length) return
            const index = items.indexOf(event.target)
            items[
                index < 0 ? (step > 0 ? 0 : items.length - 1) : (index + step + items.length) % items.length
            ].focus()
        }
        function outsideProvider(event) {
            if (providerOpen.value && !providerRoot.value?.contains(event.target)) closeProviders(false)
        }
        function providerFocusOut(event) {
            if (event.relatedTarget && !providerRoot.value?.contains(event.relatedTarget))
                closeProviders(false)
        }
        function cancel() {
            if (providerOpen.value) closeProviders()
            else close()
        }
        watch(providers, (available) => {
            if (provider.value && !available.includes(provider.value)) provider.value = ''
        })
        watch(
            () => props.provider,
            () => {
                provider.value = ''
                closeProviders(false)
            },
        )
        watch(inputChoices, (available) => {
            if (!available.includes(inputModality.value)) inputModality.value = ''
        })
        watch(outputChoices, (available) => {
            if (!available.includes(outputModality.value)) outputModality.value = ''
        })
        const filtered = computed(() => {
            const query = search.value.trim().toLowerCase()
            const value = (m) =>
                sort.value === 'context'
                    ? number(m.limit?.context)
                    : sort.value.startsWith('cost_')
                      ? number(m.cost?.[sort.value.slice(5)])
                      : sort.value === 'name'
                        ? name(m)
                        : m[sort.value] || null
            return catalog.value
                .filter(
                    (m) =>
                        (!provider.value || m.provider === provider.value) &&
                        (!favoritesOnly.value || isFavorite(m)) &&
                        (!inputSelection.value || modalities(m, 'input').includes(inputSelection.value)) &&
                        (!outputSelection.value || modalities(m, 'output').includes(outputSelection.value)) &&
                        (!query || [name(m), m.id, m.provider].some((v) => v?.toLowerCase().includes(query))),
                )
                .slice()
                .sort((a, b) => {
                    const av = value(a),
                        bv = value(b)
                    if (av == null && bv == null) return name(a).localeCompare(name(b))
                    if (av == null) return 1
                    if (bv == null) return -1
                    const order = typeof av === 'number' ? av - bv : String(av).localeCompare(String(bv))
                    return (ascending.value ? order : -order) || name(a).localeCompare(name(b))
                })
        })
        const visible = computed(() => filtered.value.slice(0, limit.value))
        watch(
            filtered,
            () => {
                limit.value = 20
                if (list.value) list.value.scrollTop = 0
            },
            { flush: 'sync' },
        )
        function loadMore() {
            limit.value = Math.min(limit.value + 20, filtered.value.length)
        }
        function onScroll() {
            const el = list.value
            if (el && el.scrollHeight - el.scrollTop - el.clientHeight < 160) loadMore()
        }
        function clearFilters() {
            closeProviders(false)
            search.value = ''
            provider.value = ''
            favoritesOnly.value = false
            inputModality.value = ''
            outputModality.value = ''
        }
        async function show() {
            if (props.disabled || opened.value) return
            previousFocus = document.activeElement
            clearFilters()
            search.value = props.initialSearch
            limit.value = 20
            opened.value = true
            emit('update:open', true)
            await nextTick()
            if (!opened.value || !dialog.value || dialog.value.open) return
            dialog.value.showModal()
            if (list.value) list.value.scrollTop = 0
            searchField.value?.focus()
        }
        function close() {
            if (!opened.value) return
            closeProviders(false)
            dialog.value?.close()
            opened.value = false
            emit('update:open', false)
            emit('close')
            const focus = trigger.value || previousFocus
            if (focus?.isConnected) focus.focus()
        }
        function choose(m) {
            emit('update:modelValue', m[props.valueKey] || m.name || m.id)
            emit('select', m)
            close()
        }
        watch(
            () => props.open,
            (value) => {
                if (value) show()
                else if (value === false) close()
            },
            { immediate: true },
        )
        function matches(m) {
            return props.modelValue === m.name || props.modelValue === m.id
        }
        function free(m) {
            return number(m.cost?.input) === 0 && number(m.cost?.output) === 0
        }
        function backdrop(event) {
            if (event.target !== dialog.value) return
            const r = dialog.value.getBoundingClientRect()
            if (
                event.clientX < r.left ||
                event.clientX > r.right ||
                event.clientY < r.top ||
                event.clientY > r.bottom
            )
                close()
        }
        onMounted(() => {
            document.addEventListener('pointerdown', outsideProvider)
            window.addEventListener('resize', positionProviders)
        })
        onUnmounted(() => {
            document.removeEventListener('pointerdown', outsideProvider)
            window.removeEventListener('resize', positionProviders)
            dialog.value?.close()
        })
        return {
            dialog,
            trigger,
            searchField,
            opened,
            search,
            provider,
            sort,
            ascending,
            selected,
            providers,
            providerRoot,
            providerTrigger,
            providerSearchField,
            providerOpen,
            providerSearch,
            providerPopupStyle,
            providerName,
            filteredProviders,
            providerCounts,
            toggleProviders,
            closeProviders,
            providerFocusOut,
            chooseProvider,
            moveProviderFocus,
            cancel,
            modalityIcons,
            searchIcon,
            sortIcon,
            starIcon,
            inputChoices,
            outputChoices,
            inputLocked,
            outputLocked,
            inputSelection,
            outputSelection,
            filtered,
            visible,
            list,
            loadMore,
            onScroll,
            clearFilters,
            props: propsRef,
            catalog,
            titleId,
            key,
            favoritesOnly,
            inputModality,
            outputModality,
            isFavorite,
            unavailable,
            sorts,
            name,
            price,
            short,
            show,
            close,
            choose,
            matches,
            free,
            backdrop,
        }
    },
}
