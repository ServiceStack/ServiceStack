export default {
    template:`
    <div class="flex-shrink-0 px-4 py-4 border-b border-gray-200 bg-white min-h-16 select-none">
        <div class="flex items-center justify-between">
            <button type="button"
                @click="$emit('home')"
                class="text-lg font-semibold text-gray-900 hover:text-blue-600 focus:outline-none transition-colors"
                title="Go back to initial state"
            >
                <svg class="mr-1 mb-0.5 inline-block size-6 text-gray-700" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" fill-rule="evenodd" d="M15.48 9.83c-.39-2.392-1.768-4.268-3.653-5.338l1.106-2.432a.75.75 0 1 0-1.366-.62l-1.112 2.446A7.9 7.9 0 0 0 8 3.5a7.9 7.9 0 0 0-2.455.386L4.433 1.44a.75.75 0 1 0-1.366.62l1.106 2.432C2.288 5.562.909 7.438.52 9.83c-.13.798-.178 1.655.107 2.433c.325.89.989 1.441 1.768 1.75c.701.28 1.54.383 2.404.433c.887.052 1.963.054 3.201.054s2.314-.002 3.2-.054c.864-.05 1.704-.154 2.405-.432c.78-.31 1.443-.86 1.768-1.75c.285-.78.237-1.636.107-2.434M2 10.071C1.53 12.961 3 13 8 13s6.47-.038 6-2.929C13.5 7 11 5 8 5s-5.5 2-6 5.071m8.5 1.179a.75.75 0 0 1-.75-.75V9a.75.75 0 0 1 1.5 0v1.5a.75.75 0 0 1-.75.75m-5.75-.75a.75.75 0 0 0 1.5 0V9a.75.75 0 0 0-1.5 0z" clip-rule="evenodd"/></svg>
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