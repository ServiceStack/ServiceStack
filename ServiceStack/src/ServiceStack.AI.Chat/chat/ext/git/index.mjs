import GitSidebar from './GitSidebar.mjs'
import GitDiffView from './GitDiffView.mjs'

const GitIcon = {
    template: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"><circle cx="4.5" cy="3.5" r="1.75"/><circle cx="11.5" cy="3.5" r="1.75"/><circle cx="4.5" cy="12.5" r="1.75"/><path d="M5.25 8.25c3 0 6 .5 6-2.5m-6.5 4.5v-4.5"/></g></svg>`
}

export default {
    install(ctx) {
        ctx.setRightIcons({
            git: { name: 'Git', title: 'Git history', component: GitIcon, panel: GitSidebar, preview: GitDiffView, aliases: ['history'] },
            'git-staged': { name: 'Staged diff', component: GitIcon, preview: GitDiffView, isVisible: () => false },
        })
    }
}
