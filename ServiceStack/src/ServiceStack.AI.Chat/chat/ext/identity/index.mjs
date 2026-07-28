/* C#-only UI extension (not synced from llms-py):
 * replaces the stock API-key SignIn with a redirect to the host's ASP.NET Identity Auth
 * login page when the server is configured with AuthType=OAuth. */
import { inject } from 'vue'

const IdentitySignIn = {
    template: `
    <div class="min-h-full -mt-12 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div class="sm:mx-auto sm:w-full sm:max-w-md text-center">
            <h2 class="mt-6 text-3xl font-extrabold text-gray-900 dark:text-gray-50">
                Sign In
            </h2>
            <p class="mt-6 text-gray-600 dark:text-gray-300">
                Redirecting to sign in&hellip;
            </p>
            <p class="mt-4">
                <a :href="signInUrl" class="text-blue-600 dark:text-blue-400 hover:underline">
                    Continue to sign in
                </a>
            </p>
        </div>
    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const returnTo = location.pathname + location.search
        const signInUrl = (ctx?.state?.config?.signInUrl || '/Account/Login')
            + '?ReturnUrl=' + encodeURIComponent(returnTo)
        setTimeout(() => location.href = signInUrl, 500)
        return { signInUrl }
    }
}

export default {
    order: 100,
    install(ctx) {
        if (ctx.ai.authType === 'oauth') {
            ctx.component('SignIn', IdentitySignIn)
        }
    }
}
