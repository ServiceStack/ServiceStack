import { ref } from 'vue'

export default {
    template: `
        <p class="my-4">Current count: {{currentCount}}</p>

        <PrimaryButton @click="incrementCount">Click me</PrimaryButton>
    `,
    setup() {
        const currentCount = ref(0)
        const incrementCount = () => currentCount.value++

        return { currentCount, incrementCount }
    }
}
