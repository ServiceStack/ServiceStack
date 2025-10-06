export default {
    template:`
    <div class="flex-shrink-0 px-4 py-4 border-b border-gray-200 bg-white min-h-16 select-none">
        <div class="flex items-center justify-between">
            <button type="button"
                @click="$emit('home')"
                class="text-lg font-semibold text-gray-900 hover:text-blue-600 focus:outline-none transition-colors"
                title="Go back to initial state"
            >
                History
            </button>
            <button type="button"
                @click="$emit('new')"
                class="text-gray-900 hover:text-blue-600 focus:outline-none transition-colors"
                title="New Chat"
            >
                <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"/></g></svg>
            </button>
        </div>
    </div>
    `,
    emits:['home','new'],
}