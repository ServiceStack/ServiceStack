import OpenAiSettings from './OpenAiSettings.mjs'

export default {
    install(ctx) {
        ctx.setSettings({
            openai_subscription: {
                component: OpenAiSettings,
            }
        })
    }
}
