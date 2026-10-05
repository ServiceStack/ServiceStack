import { ref, watch, computed } from 'vue'
import MetadataValueInput from './MetadataValueInput.mjs'
import { loadDecisionTags, tagGroup, recipeMetadata } from './decisionTags.mjs'
export default {
    components: { MetadataValueInput },
    props: { content: String, tags: Array, api: Function, cacheScope: String },
    emits: ['change'],
    template: `<div data-jev-tag-picker class="grid grid-cols-2 gap-4 min-w-0 mb-3.5 max-[620px]:grid-cols-1">
      <MetadataValueInput label="Content" :values="metadata.content?[metadata.content]:[]" :suggestions="suggestions.filter(tag=>tagGroup(tag)==='content')" :max="1" replace hint="Type of content this recipe acts on. Choose or create one." @change="changeContent"/>
      <MetadataValueInput label="Tags" :values="metadata.tags" :suggestions="suggestions.filter(tag=>tagGroup(tag)==='tag')" :max="3" hint="Up to 3 tags. Enter or comma adds one." @change="changeTags"/>
    </div>`,
    setup(props, { emit }) {
        const suggestions = ref([])
        const metadata = computed(() =>
            recipeMetadata(props, suggestions.value),
        )
        let generation = 0
        watch(
            () => [props.api, props.cacheScope],
            async () => {
                const token = ++generation
                suggestions.value = []
                if (!props.api) return
                const catalog = await loadDecisionTags(
                    props.api,
                    props.cacheScope,
                )
                if (token === generation) suggestions.value = catalog.tags
            },
            { immediate: true },
        )
        function changeContent(values) {
            emit('change', {
                content: values[0] || '',
                tags: metadata.value.tags,
            })
        }
        function changeTags(tags) {
            emit('change', { content: metadata.value.content, tags })
        }
        return { suggestions, metadata, tagGroup, changeContent, changeTags }
    },
}
