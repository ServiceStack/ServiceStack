import ProviderStatus from "./ProviderStatus.mjs";
export default {
    components: {
        ProviderStatus,
    },
    template:`
        <!-- Model Selector -->
        <div class="pl-1 flex space-x-2">
            <Autocomplete id="model" :options="models" label=""
                :modelValue="modelValue" @update:modelValue="$emit('update:modelValue', $event)"
                class="w-72 xl:w-84"
                :match="(x, value) => x.toLowerCase().includes(value.toLowerCase())"
                placeholder="Select Model...">
                <template #item="{ value }">
                    <div class="truncate max-w-72" :title="value">{{value}}</div>
                </template>
            </Autocomplete>
            <ProviderStatus @updated="$emit('updated', $event)" />
        </div>
    `,
    emits: ['updated', 'update:modelValue'],
    props: {
        models: Array,
        modelValue: String,
    },
    setup() {
    }
}
