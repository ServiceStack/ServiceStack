import ProviderStatus from "./ProviderStatus.mjs"
import ProviderIcon from "./ProviderIcon.mjs"
import { useFormatters } from "@servicestack/vue"

export default {
    components: {
        ProviderStatus,
        ProviderIcon,
    },
    template:`
        <!-- Model Selector -->
        <div class="pl-1 flex space-x-2">
            <Autocomplete id="model" :options="models" label=""
                :modelValue="modelValue" @update:modelValue="$emit('update:modelValue', $event)"
                class="w-72 xl:w-84"
                :match="(x, value) => x.id.toLowerCase().includes(value.toLowerCase())"
                placeholder="Select Model...">
                <template #item="{ id, provider, provider_model, pricing }">
                    <div :key="id + provider + provider_model" class="group truncate max-w-72 flex justify-between">
                        <span :title="id">{{id}}</span>
                        <span class="flex items-center space-x-1">
                            <span v-if="pricing && (parseFloat(pricing.input) == 0 && parseFloat(pricing.input) == 0)">
                                <span class="text-xs text-gray-500" title="Free to use">FREE</span>                            
                            </span>
                            <span v-else-if="pricing" class="text-xs text-gray-500" 
                                :title="'Estimated Cost per token: ' + pricing.input + ' input | ' + pricing.output + ' output'">
                              {{tokenPrice(pricing.input)}}
                              &#183;
                              {{tokenPrice(pricing.output)}} M
                            </span>
                            <span :title="provider_model + ' from ' + provider">    
                                <ProviderIcon :provider="provider" />
                            </span>
                        </span>
                    </div>
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

        const numFmt = new Intl.NumberFormat(undefined,{style:'currency',currency:'USD'})
        
        function tokenPrice(price) {
            if (!price) return ''
            var ret = numFmt.format(parseFloat(price) * 1_000_000)
            return ret.endsWith('.00') ? ret.slice(0, -3) : ret
        }

        return {
            tokenPrice
        }
    }
}
