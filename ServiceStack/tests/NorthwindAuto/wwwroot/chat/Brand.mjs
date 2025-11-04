export default {
    template:`
    <div class="flex-shrink-0 pl-2 pr-4 py-4 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 min-h-16 select-none">
        <div class="flex items-center justify-between">
            <div class="flex items-center space-x-2">
                <button type="button"
                    @click="$emit('toggle-sidebar')"
                    class="group relative text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none transition-colors"
                    title="Collapse sidebar"
                >
                    <div class="relative size-5">
                        <!-- Default sidebar icon -->
                        <svg class="absolute inset-0 group-hover:hidden" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                            <line x1="9" y1="3" x2="9" y2="21"></line>
                        </svg>
                        <!-- Hover state: |← icon -->
                        <svg class="absolute inset-0 hidden group-hover:block" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m10.071 4.929l1.414 1.414L6.828 11H16v2H6.828l4.657 4.657l-1.414 1.414L3 12zM18.001 19V5h2v14z"/></svg>
                    </div>
                </button>

                <button type="button"
                    @click="$emit('home')"
                    class="text-lg font-semibold text-gray-900 dark:text-gray-200 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none transition-colors"
                    title="Go back to initial state"
                >
                    History
                </button>
            </div>

            <div class="flex items-center space-x-2">
                <button type="button"
                    @click="$emit('analytics')"
                    class="text-gray-900 dark:text-gray-200 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none transition-colors"
                    title="Analytics"
                >
                    <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M5 22a1 1 0 0 1-1-1v-8a1 1 0 0 1 2 0v8a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1V3a1 1 0 0 1 2 0v18a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1V9a1 1 0 0 1 2 0v12a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1v-4a1 1 0 0 1 2 0v4a1 1 0 0 1-1 1"/></svg>
                </button>

                <button type="button"
                    @click="$emit('new')"
                    class="text-gray-900 dark:text-gray-200 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none transition-colors"
                    title="New Chat"
                >
                    <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"/></g></svg>
                </button>
            </div>
        </div>
    </div>
    `,
    emits:['home','new','analytics','toggle-sidebar'],
}