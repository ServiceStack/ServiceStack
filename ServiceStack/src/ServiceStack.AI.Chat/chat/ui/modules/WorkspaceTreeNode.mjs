export default {
    name: 'WorkspaceTreeNode',
    props: ['entry', 'tree', 'selectedPath', 'selectedFile', 'isRoot'],
    emits: ['directory', 'file', 'retry'],
    computed: { item() { return this.tree.node(this.entry.path) } },
    template: `<div>
        <div class="flex items-center rounded" :class="(entry.directory ? selectedPath === entry.path && !selectedFile : selectedFile === entry.path) ? $styles.threadItemActive : $styles.threadItemHover">
            <button type="button" @click="entry.directory ? $emit('directory', entry.path) : $emit('file', entry)" :aria-expanded="entry.directory ? item.expanded : undefined" :aria-label="entry.directory ? (item.expanded ? 'Collapse ' : 'Expand ') + entry.name : undefined" :title="entry.path"  data-workspace-tree-label class="flex items-center gap-2 min-w-0 min-h-7.5 py-1 px-2 flex-1 text-left">
                <svg v-if="entry.directory" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 640" class="size-4 shrink-0 text-gray-500 dark:text-gray-400" aria-hidden="true">
                  <path v-if="item.expanded" fill="currentColor" d="m129.5 464l50-160h379.4l-50 160zm190.7 48H509c21 0 39.6-13.6 45.8-33.7l50-160c9.7-30.9-13.4-62.3-45.8-62.3H179.6c-21 0-39.6 13.6-45.8 33.7l-21.6 68.7V160c0-8.8 7.2-16 16-16h138.7c3.5 0 6.8 1.1 9.6 3.2l38.4 28.8c13.8 10.4 30.7 16 48 16h117.3c8.8 0 16 7.2 16 16h48c0-35.3-28.7-64-64-64H362.9c-6.9 0-13.7-2.2-19.2-6.4l-38.4-28.8c-11.1-8.3-24.5-12.8-38.4-12.8H128.2c-35.3 0-64 28.7-64 64v288c0 35.3 28.7 64 64 64z"/>
                  <path v-else fill="currentColor" d="M512 464H128c-8.8 0-16-7.2-16-16V304h416v144c0 8.8-7.2 16-16 16m16-208H112v-96c0-8.8 7.2-16 16-16h138.7c3.5 0 6.8 1.1 9.6 3.2l38.4 28.8c13.8 10.4 30.7 16 48 16H512c8.8 0 16 7.2 16 16zM128 512h384c35.3 0 64-28.7 64-64V208c0-35.3-28.7-64-64-64H362.7c-6.9 0-13.7-2.2-19.2-6.4l-38.4-28.8C294 100.5 280.5 96 266.7 96H128c-35.3 0-64 28.7-64 64v288c0 35.3 28.7 64 64 64"/>
                </svg>
                <svg v-else-if="!entry.directory" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="shrink-0" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6"/></svg>
                <span class="truncate" :class="{ 'font-medium': isRoot }">{{entry.name}}</span>
            </button>
        </div>
        <div v-if="entry.directory && item.expanded"  data-workspace-tree-children class="pl-4">
            <p v-if="item.loading" role="status" class="text-xs opacity-70 p-1">Loading…</p>
            <p v-else-if="item.error" role="alert" class="text-xs p-1">{{item.error}} <button type="button" @click="$emit('retry', entry.path)">Retry</button></p>
            <template v-else-if="item.response">
                <WorkspaceTreeNode v-for="child in item.response.entries" :key="child.path" :entry="child" :tree="tree" :selected-path="selectedPath" :selected-file="selectedFile" @retry="$emit('retry', $event)" @directory="$emit('directory', $event)" @file="$emit('file', $event)" />
                <p v-if="!item.response.entries.length" class="text-xs opacity-70 p-1">Empty directory</p>
                <p v-if="item.response.truncated" class="text-xs opacity-70 p-1">Showing the first 2,000 entries.</p>
            </template>
        </div>
    </div>`
}
