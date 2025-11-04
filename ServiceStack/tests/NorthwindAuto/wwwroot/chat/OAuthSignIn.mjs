import { inject, ref, onMounted } from "vue"
import Welcome from './Welcome.mjs'

export default {
    components: {
        Welcome,
    },
    template: `
    <div class="min-h-full -mt-36 flex flex-col justify-center sm:px-6 lg:px-8">
        <div class="sm:mx-auto sm:w-full sm:max-w-md text-center">
            <Welcome />
        </div>
        <div class="sm:mx-auto sm:w-full sm:max-w-md">
            <div v-if="errorMessage" class="mb-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-200 rounded-lg px-4 py-3">
                <div class="flex items-start space-x-2">
                    <div class="flex-1">
                        <div class="text-base font-medium">{{ errorMessage }}</div>
                    </div>
                    <button type="button"
                        @click="errorMessage = null"
                        class="text-red-400 dark:text-red-300 hover:text-red-600 dark:hover:text-red-100 flex-shrink-0"
                    >
                        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                        </svg>
                    </button>
                </div>
            </div>
            <div class="py-8 px-4 sm:px-10">
                <div class="space-y-4">
                    <button
                        type="button"
                        @click="signInWithGitHub"
                        class="w-full inline-flex items-center justify-center px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-base font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 transition-colors"
                    >
                        <svg class="w-6 h-6 mr-3" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path fill-rule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clip-rule="evenodd" />
                        </svg>
                        Sign in with GitHub
                    </button>
                </div>
            </div>
        </div>
    </div>     
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ai = inject('ai')
        const errorMessage = ref(null)
        
        function signInWithGitHub() {
            // Redirect to GitHub OAuth endpoint
            window.location.href = '/auth/github'
        }
        
        // Check for session token in URL (after OAuth callback redirect)
        onMounted(async () => {
            const urlParams = new URLSearchParams(window.location.search)
            const sessionToken = urlParams.get('session')
            
            if (sessionToken) {
                try {
                    // Validate session with server
                    const response = await ai.get(`/auth/session?session=${sessionToken}`)
                    
                    if (response.ok) {
                        const sessionData = await response.json()
                        
                        // Clean up URL
                        const url = new URL(window.location.href)
                        url.searchParams.delete('session')
                        window.history.replaceState({}, '', url.toString())
                        
                        // Emit done event with session data
                        emit('done', sessionData)
                    } else {
                        errorMessage.value = 'Failed to validate session'
                    }
                } catch (error) {
                    console.error('Session validation error:', error)
                    errorMessage.value = 'Failed to validate session'
                }
            }
        })
        
        return {
            signInWithGitHub,
            errorMessage,
        }
    }
}

