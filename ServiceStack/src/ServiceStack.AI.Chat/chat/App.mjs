import Sidebar from "./Sidebar.mjs"

export default {
    components: {
        Sidebar,
    },
    template: `
        <div class="flex h-screen bg-white">
            <!-- Sidebar -->
            <div class="w-72 xl:w-80 flex-shrink-0">
                <Sidebar />
            </div>

            <!-- Main Area -->
            <div class="flex-1 flex flex-col">
                <RouterView />
            </div>
        </div>
    `,
}
