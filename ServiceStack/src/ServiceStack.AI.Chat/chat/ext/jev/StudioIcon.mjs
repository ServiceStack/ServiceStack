export const decisionTreePath =
    'M30 12V4h-8v3h-4a2 2 0 0 0-2 2v6h-6v-3H2v8h8v-3h6v6a2 2 0 0 0 2 2h4v3h8v-8h-8v3h-4V9h4v3ZM8 18H4v-4h4Zm16 4h4v4h-4Zm0-16h4v4h-4Z'

const paths = {
    branch: decisionTreePath,
    spark: 'm12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5L12 3 M20 2v4m-2-2h4',
    search: 'M10 3a7 7 0 1 0 0 14a7 7 0 1 0 0-14 M15 15l6 6',
    plus: 'M12 5v14M5 12h14',
    play: 'm8 5 11 7-11 7V5',
    star: 'm12 3 2.8 5.7 6.2.9-4.5 4.4 1.1 6.2-5.6-3-5.6 3 1.1-6.2L3 9.6l6.2-.9L12 3',
    check: 'm5 12 4 4 10-10',
    chevron: 'm6 9 6 6 6-6',
    info: 'M8.499 7.5a.5.5 0 1 0-1 0v3a.5.5 0 0 0 1 0zm.25-2a.749.749 0 1 1-1.499 0a.749.749 0 0 1 1.498 0M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1M2 8a6 6 0 1 1 12 0A6 6 0 0 1 2 8',
    warning: 'M10.3 4.6 2.6 18a2 2 0 0 0 1.7 3h15.4a2 2 0 0 0 1.7-3L13.7 4.6a2 2 0 0 0-3.4 0M12 9v5m0 3v.1',
    code: 'm8 6-6 6 6 6m8-12 6 6-6 6m-3-15-2 18',
    clock: 'M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18 M12 7v5l3 2',
    arrow: 'M5 12h14m-5-5 5 5-5 5',
    close: 'm6 6 12 12M6 18 18 6',
    trash: 'M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7',
    file: 'M14 2H6v20h12V6l-4-4v5h4M9 12h6m-6 4h6',
    import: 'm6 18l1.41 1.41L15 11.83V30h2V11.83l7.59 7.58L26 18L16 8zM6 8V4h20v4h2V4a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v4Z',
    share: 'M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8m-4-6-4-4-4 4m4-4v13',
}
export default {
    props: { name: String },
    template: `<svg :viewBox="viewBox" :fill="filled?'currentColor':'none'" :stroke="filled?'none':'currentColor'" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"  data-jev-icon class="shrink-0"><path :d="path"/></svg>`,
    computed: {
        filled() {
            return ['branch', 'info', 'import'].includes(this.name)
        },
        viewBox() {
            return this.name === 'info' ? '0 0 16 16' : ['branch', 'import'].includes(this.name) ? '0 0 32 32' : '0 0 24 24'
        },
        path() {
            return paths[this.name] || paths.file
        },
    },
}
