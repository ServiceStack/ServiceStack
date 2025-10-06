export default {
    template:`
        <button v-if="modelValue" type="button" title="Clear System Prompt" @click="$emit('update:modelValue', null)">
            <svg class="size-4 text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/></svg>
        </button>
        
        <Autocomplete id="prompt" :options="prompts" label=""
            :modelValue="modelValue" @update:modelValue="$emit('update:modelValue', $event)"
            class="w-72 xl:w-84"
            :match="(x, value) => x.name.toLowerCase().includes(value.toLowerCase())"
            placeholder="Select a System Prompt...">
            <template #item="{ value }">
                <div class="truncate max-w-72" :title="value">{{value}}</div>
            </template>
        </Autocomplete>

        <!-- Toggle System Prompt Visibility -->
        <button type="button"
            @click="$emit('toggle')"
            :class="show ? 'text-blue-700' : 'text-gray-600'"
            class="p-1 rounded-md hover:bg-blue-100 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            :title="show ? 'Hide system prompt' : 'Show system prompt'"
        >
            <svg v-if="!show" class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36"><path fill="currentColor" d="M33.62 17.53c-3.37-6.23-9.28-10-15.82-10S5.34 11.3 2 17.53l-.28.47l.26.48c3.37 6.23 9.28 10 15.82 10s12.46-3.72 15.82-10l.26-.48Zm-15.82 8.9C12.17 26.43 7 23.29 4 18c3-5.29 8.17-8.43 13.8-8.43S28.54 12.72 31.59 18c-3.05 5.29-8.17 8.43-13.79 8.43"/><path fill="currentColor" d="M18.09 11.17A6.86 6.86 0 1 0 25 18a6.86 6.86 0 0 0-6.91-6.83m0 11.72A4.86 4.86 0 1 1 23 18a4.87 4.87 0 0 1-4.91 4.89"/><path fill="none" d="M0 0h36v36H0z"/></svg>
            <svg v-else class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36"><path fill="currentColor" d="M25.19 20.4a6.8 6.8 0 0 0 .43-2.4a6.86 6.86 0 0 0-6.86-6.86a6.8 6.8 0 0 0-2.37.43L18 13.23a5 5 0 0 1 .74-.06A4.87 4.87 0 0 1 23.62 18a5 5 0 0 1-.06.74Z" class="clr-i-outline clr-i-outline-path-1"/><path fill="currentColor" d="M34.29 17.53c-3.37-6.23-9.28-10-15.82-10a16.8 16.8 0 0 0-5.24.85L14.84 10a14.8 14.8 0 0 1 3.63-.47c5.63 0 10.75 3.14 13.8 8.43a17.8 17.8 0 0 1-4.37 5.1l1.42 1.42a19.9 19.9 0 0 0 5-6l.26-.48Z"/><path fill="currentColor" d="m4.87 5.78l4.46 4.46a19.5 19.5 0 0 0-6.69 7.29l-.26.47l.26.48c3.37 6.23 9.28 10 15.82 10a16.9 16.9 0 0 0 7.37-1.69l5 5l1.75-1.5l-26-26Zm9.75 9.75l6.65 6.65a4.8 4.8 0 0 1-2.5.72A4.87 4.87 0 0 1 13.9 18a4.8 4.8 0 0 1 .72-2.47m-1.45-1.45a6.85 6.85 0 0 0 9.55 9.55l1.6 1.6a14.9 14.9 0 0 1-5.86 1.2c-5.63 0-10.75-3.14-13.8-8.43a17.3 17.3 0 0 1 6.12-6.3Z"/><path fill="none" d="M0 0h36v36H0z"/></svg>
        </button>
    `,
    emits: ['updated', 'update:modelValue', 'toggle'],
    props: {
        prompts: Array,
        modelValue: Object,
        show: Boolean,
    },
    setup() {
    }
}
