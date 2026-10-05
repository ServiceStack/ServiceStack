/** Shared native checkbox, styled by CheckBox.css (also applied to schema-generated fields). */
export const CheckBox = {
    template: `<input type="checkbox" class="llms-checkbox" :checked="modelValue" :indeterminate="indeterminate"
        @change="$emit('update:modelValue', $event.target.checked)">`,
    props: { modelValue: Boolean, indeterminate: Boolean },
    emits: ['update:modelValue'],
}
