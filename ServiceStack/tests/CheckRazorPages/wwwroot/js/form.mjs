import { computed, useAttrs, inject } from 'https://unpkg.com/vue@3/dist/vue.esm-browser.js'
import { errorResponse, humanize, omit, ResponseStatus, toPascalCase } from './servicestack-client.mjs'
function map(o, f) { return o == null ? null : f(o) }
export var SelectInput = {
    template: /*html*/`
    <div>
        <label v-if="useLabel" :for="id" class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ useLabel }}</label>
        <select :id="id" :name="id" :class="['mt-1 block w-full pl-3 pr-10 py-2 text-base focus:outline-none border-gray-300 sm:text-sm rounded-md dark:text-white dark:bg-gray-900 dark:border-gray-600',
            !errorField ? 'text-gray-900 focus:ring-indigo-500 focus:border-indigo-500' : 'text-red-900 focus:ring-red-500 focus:border-red-500']"
            :value="modelValue"
            @input="$emit('update:modelValue', value($event.target))"
            :aria-invalid="errorField != null"
            :aria-describedby="idError"
            v-bind="remaining">
          <option v-for="entry in kvpValues" :value="entry.key">{{ entry.value }}</option>
        </select>
        <p v-if="errorField" class="mt-2 text-sm text-red-500" :id="idError">{{ errorField }}</p>
    </div>
    `,
    props: ['status', 'id', 'modelValue', 'label', 'options', 'values'],
    setup(props) {
        const value = e => e.value
        const useLabel = computed(() => props.label || humanize(toPascalCase(props.id)))
        const remaining = computed(() => omit(useAttrs(), [...Object.keys(props)]))
        let ctx = inject('ApiState', undefined)
        const errorField = computed(() => errorResponse.call({ responseStatus: props.status || map(ctx, x => x.error.value) }, props.id))
        const kvpValues = computed(() => props.values
            ? props.values.map(x => ({ key: x, value: x }))
            : props.options
                ? Object.keys(props.options).map(key => ({ key, value: props.options[key] }))
                : [])
        const idError = computed(() => `${props.id}-error`)

        return {
            value,
            useLabel,
            remaining,
            errorField,
            kvpValues,
            idError,
        }
    }
}

export var TextInput = {
    template:/*html*/`<div>
        <label v-if="useLabel" :for="id" class="block text-sm font-medium text-gray-700">{{ useLabel }}</label>
        <div class="mt-1 relative rounded-md shadow-sm">
          <input :type="useType"
                 :name="id"
                 :id="id"
                 :class="cls"
                 :placeholder="usePlaceholder"
                 :value="modelValue"
                 @input="$emit('update:modelValue', value($event.target))"
                 :aria-invalid="errorField != null"
                 :aria-describedby="idDescription"
                 v-bind="remaining">
          <div v-if="errorField" class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
            <svg class="h-5 w-5 text-red-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </div>
        </div>
        <p v-if="errorField" class="mt-2 text-sm text-red-500" :id="idError">{{ errorField }}</p>
        <p v-else-if="help" class="mt-2 text-sm text-gray-500" :id="idDescription">{{ help }}</p>
    </div>`,
    props: ['status', 'id', 'label', 'help', 'placeholder', 'modelValue'],
    setup(props) {
        const value = e => e.value
        const useType = computed(() => props.type || 'text')
        const useLabel = computed(() => props.label != null ? props.label : humanize(toPascalCase(props.id)))
        const usePlaceholder = computed(() => props.placeholder != null ? props.placeholder : useLabel.value)
        const remaining = computed(() => omit(useAttrs(), [...Object.keys(props)]))
        let ctx = inject('ApiState', undefined)
        const errorField = computed(() => errorResponse.call({ responseStatus: props.status || map(ctx, x => x.error.value) }, props.id))
        const cls = computed(() => ['block w-full sm:text-sm rounded-md dark:text-white dark:bg-gray-900', errorField.value
            ? 'pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500'
            : 'shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 dark:border-gray-600'])
        const idError = computed(() => `${props.id}-error`)
        const idDescription = computed(() => `${props.id}-description`)

        return {
            cls,
            value,
            useType,
            useLabel,
            usePlaceholder,
            remaining,
            errorField,
            idError,
            idDescription,
        }
    }
}

export var TextareaInput = {
    template: /*html*/`<div>
        <label v-if="useLabel" :for="id" class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ useLabel }}</label>
        <div class="mt-1 relative rounded-md shadow-sm">
          <textarea
             :name="id"
             :id="id"
             :class="cls"
             :placeholder="usePlaceholder"
             @input="$emit('update:modelValue', value($event.target))"
             :aria-invalid="errorField != null"
             :aria-describedby="idDescription"
             v-bind="remaining">{{ modelValue }}</textarea>
        </div>
        <p v-if="errorField" class="mt-2 text-sm text-red-500" :id="idError">{{ errorField }}</p>
        <p v-else-if="help" class="mt-2 text-sm text-gray-500" :id="idDescription">{{ help }}</p>
      </div>`,
    props: ['status', 'id', 'label', 'help', 'placeholder', 'modelValue'],
    setup(props) {
        const value = e => e.value
        const useLabel = computed(() => props.label != null ? props.label : humanize(toPascalCase(props.id)))
        const usePlaceholder = computed(() => props.placeholder != null ? props.placeholder : useLabel.value)
        const remaining = computed(() => omit(useAttrs(), [...Object.keys(props)]))
        let ctx = inject('ApiState', undefined)
        const errorField = computed(() => errorResponse.call({ responseStatus: props.status || map(ctx, x => x.error.value) }, props.id))
        const cls = computed(() => ['shadow-sm block w-full sm:text-sm rounded-md dark:text-white dark:bg-gray-900', errorField.value
            ? 'text-red-900 focus:ring-red-500 focus:border-red-500 border-red-300'
            : 'text-gray-900 focus:ring-indigo-500 focus:border-indigo-500 border-gray-300 dark:border-gray-600'])
        const idError = computed(() => `${props.id}-error`)
        const idDescription = computed(() => `${props.id}-description`)

        return {
            cls,
            value,
            useLabel,
            usePlaceholder,
            remaining,
            errorField,
            idError,
            idDescription,
        }
    }
}
