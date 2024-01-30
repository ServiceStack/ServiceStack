import { promises as fs } from 'fs'
import path from 'path'

const all = [
    'Markdown.Blog.cs',
    'Markdown.Meta.cs',
    'Markdown.Pages.cs',
    'Markdown.Videos.cs',
    'Markdown.WhatsNew.cs',
    'MarkdownPagesBase.cs',
    'MarkdownTagHelper.cs',
]

function copyTo(all, dir) {
    all.forEach(async src => {
        const dest = path.join(dir,src)
        console.log(src, dest)
        await fs.copyFile(src, dest)
    })
}

await copyTo(all, '../../../../servicestack.net/MyApp')

