// Parse unified patches without treating file headers as changed source lines.
export function parseUnifiedDiff(patch) {
    const rows = []
    let oldLine = 0, newLine = 0, inHunk = false
    const lines = (patch || '').split('\n')
    if (lines.at(-1) === '') lines.pop()
    for (const raw of lines) {
        const line = raw.endsWith('\r') ? raw.slice(0, -1) : raw
        const hunk = /^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@/.exec(line)
        if (hunk) {
            oldLine = Number(hunk[1]); newLine = Number(hunk[2]); inHunk = true
            rows.push({ type: 'hunk', text: line })
        } else if (inHunk && line.startsWith('+')) {
            rows.push({ type: 'added', newLine: newLine++, prefix: '+', text: line.slice(1) })
        } else if (inHunk && line.startsWith('-')) {
            rows.push({ type: 'removed', oldLine: oldLine++, prefix: '−', text: line.slice(1) })
        } else if (inHunk && line.startsWith(' ')) {
            rows.push({ type: 'context', oldLine: oldLine++, newLine: newLine++, prefix: ' ', text: line.slice(1) })
        } else if (!inHunk && /^(diff --git |index |--- |\+\+\+ )/.test(line)) {
            continue
        } else {
            rows.push({ type: 'meta', text: line })
        }
    }
    return rows
}
