import { promises as fs } from 'fs'
import path from 'path'

function copyTo(all, dir) {
    all.forEach(async src => {
        const dest = path.join(dir,src)
        console.log(src, dest)
        await fs.copyFile(src, dest)
    })
}

await copyTo([
    'Markdown.Blog.cs',
    'Markdown.Meta.cs',
    'Markdown.Pages.cs',
    'Markdown.Videos.cs',
    'Markdown.WhatsNew.cs',
    'MarkdownPagesBase.cs',
    'MarkdownTagHelper.cs',
], '../../../../servicestack.net/MyApp')

await copyTo([
    'Markdown.Blog.cs',
    'Markdown.Meta.cs',
    'Markdown.Pages.cs',
    'Markdown.Videos.cs',
    'Markdown.WhatsNew.cs',
    'MarkdownPagesBase.cs',
    'MarkdownTagHelper.cs',
], '../../../../../NetCoreTemplates/blazor-vue/MyApp')

await copyTo([
    'Markdown.Pages.cs',
    'Markdown.Videos.cs',
    'MarkdownPagesBase.cs',
], '../../../../../NetCoreTemplates/blazor/MyApp')
