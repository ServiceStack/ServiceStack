import { computed, inject } from "vue"
export default {
    template:`
        <div v-if="$ai.auth?.profileUrl" :title="authTitle">
            <img :src="$ai.auth.profileUrl" class="size-8 rounded-full" />
        </div>
    `,
    setup() {
        const ai = inject('ai')
        const authTitle = computed(() => {
            if (!ai.auth) return ''
            const { userId, userName, displayName, bearerToken, roles } = ai.auth
            const name = userName || displayName
            const prefix = roles && roles.includes('Admin') ? 'Admin' : 'Name'
            const sb = [
                name ? `${prefix}: ${name}` : '',
                `API Key: ${bearerToken}`,
                `${userId}`,
            ]
            return sb.filter(x => x).join('\n')
        })
        
        return {
            authTitle,
        }
    }
}
