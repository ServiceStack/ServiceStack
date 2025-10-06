export default {
    template:`
    <div class="border-b border-gray-200 bg-gray-50 px-6 py-4">
        <div class="max-w-6xl mx-auto">
            <label class="block text-sm font-medium text-gray-700 mb-2">
                System Prompt
                <span v-if="selected" class="text-gray-500 font-normal">
                    ({{ prompts.find(p => p.id === selected.id)?.name || 'Custom' }})
                </span>
            </label>
            <textarea
                :value="modelValue" @input="$emit('update:modelValue', $event.target.value)"
                placeholder="Enter a system prompt to guide AI's behavior..."
                rows="6"
                class="block w-full resize-vertical rounded-md border border-gray-300 px-3 py-2 text-sm placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            ></textarea>
            <div class="mt-2 text-xs text-gray-500">
                You can modify this system prompt before sending messages. Changes will only apply to new conversations.
            </div>
        </div>
    </div>
    `,
    emits: ['update:modelValue'],
    props: {
        prompts: Array,
        selected: Object,
        modelValue: String,
    },
    setup() {
    }
}
