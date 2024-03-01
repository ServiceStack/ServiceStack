---
title: Migrating from Jekyll to VitePress
summary: Since Jekyll support has been officially sunset, we decided to migrate our docs site to VitePress
author: Lucy Bates
tags: [docs, markdown]
image: https://images.unsplash.com/photo-1524668951403-d44b28200ce0?crop=entropy&fit=crop&h=1000&w=2000
---

<style>
table {
    margin-bottom: 2em;
}
th, td {
    padding: 1em 1.5em;
}
tbody tr:nth-child(odd) {
    --tw-bg-opacity: 1;
    background-color: rgba(243, 244, 246, var(--tw-bg-opacity));
}
</style>

Since Jekyll support has been officially sunset, we decided to migrate our docs site to VitePress. VitePress is a new static site generator based on Vite and Vue 3. It is a cut down version of VuePress but using the blisteringly fast Vite build tool.

This has given us the ability to update our docs locally with instant on save update to view the final result quickly while we edit/create docs.

<div class="my-8 ml-20 flex justify-center">
    <img style="max-height:150px" src="./img/posts/jekyll-to-vitepress/vite.svg" alt="Vite logo">
    <img style="max-height:150px" src="./img/posts/jekyll-to-vitepress/vuepress.png" alt="VitePress logo">
</div>


We wanted to share our experience with this process in the hope it might help others that are performing the same migration, document common errors and help other developer to get an idea about what is involved before you might undertake such a task.

## Jekyll vs VitePress

Below is not an exhaustive list of features but more focusing on pros and cons when it comes to comparing these two options for use as the static site generator for a documentation site.

| Features           | Jekyll            | VitePress                      |
|:-------------------|:------------------|:-------------------------------|
| Native Language    | Ruby              | JavaScript                     |
| Template Syntax    | Liquid            | Vue                            |
| Update Time        | 6-30 seconds      | 30-500 ms                      |
| Themes             | ✅ - mature market | ✅ - limited                    |
| Extensible         | ✅                 | ✅ (only themes + vite plugins) |
| Client framework   | None              | Vue 3                          |
| Maintained         | No longer         | New project                    |
| 1.0+ release       | ✅                 | ➖                              |
| Permalink          | ✅                 | ➖ (depends on filename)        |
| Markdown support   | ✅                 | ✅                              |
| HTML support       | ✅                 | ✅ (must use Vue component)     |
| Sitemap generation | ✅                 | ➖                              |
| Tags               | ✅                 | ➖                              |
| Clean URLs         | ✅                 | ➖                              |

This list might look bad for VitePress, but it comes down to a young library that is still in active development. The default theme for VitePress is also centered around technical documentation making it quick to get up and running looking good for this use case.

## Killer feature - Performance

The stand out feature and one of the most compelling reason for us to undertake this migration was not the default theme (although that helped) but the user experience when editing documentation locally.

![](img/posts/jekyll-to-vitepress/vitepress-update-large.gif)

In contrast, editing this blog post for our main site which is still currently using Jekyll, it takes between 6-8 seconds for a small single page change. Previously our docs page with over 300 pages could take over a minute depending on the type of change.

```
Regenerating: 1 file(s) changed at 2021-10-29 16:17:00
                    _blog/posts/2021-10-29-jekyll-migration.md
                    ...done in 6.3882767 seconds.
```

Having a statically generated site that is hard to preview or has a slow iteration cycle can be extremely frustrating to work with so we were looking for something that a pleasure to work with and well suited to documentation.

## Common Migration Problems

When we started this task to migrate, we want to first have a proof of concept of migrating all the content to the same URLs.
This required us to achieve a few things before we could get stuck into fixing content/syntax related changes.

- Page URLs must be the same.
- Project must be able to run locally and deploy.
- Side menu links must be present.

These were our minimum requirements before we wanted to commit to making all the changes required to migrate.

## File name vs `slug`

The first one surfaced a design difference straight away. While Jekyll created HTML output based on frontmatter `slug` property, VitePress only works off the MarkDown file name.

We would commonly create a MarkDown file called one thing but change our mind on the URL and change the frontmatter `permalink` to something else. This broke all our existing paths, so we needed a way to parse the markdown files, and produce copies with the updated name using the `slug` value in the frontmatter.

Since we were still evaluating this process, we created a quick C# script that would take in a file, extract the `slug` value and copy a file with that name back out into a separate directory.

```csharp
static void Main(string[] args)
{
    var filename = args[0];
    var fileLines = File.ReadAllLines(filename).ToList();
    if (!Directory.Exists("updated"))
    {
        Directory.CreateDirectory("updated");
    }
    foreach (var line in fileLines)
    {
        if (line.StartsWith("slug:"))
        {
            var newName = line.Split(":")[1].Trim();
            File.WriteAllLines("./updated/" + newName + ".md", fileLines);
        }
    }
}
```

Not very elegant, but it did the job. This was published as a `single file` executable targeting linux so it could be easily used with tools like `find` with `--exec`.

```shell
find *.md -maxdepth 1 -type f -exec ./RenameMd {} \;
```

All round pretty hacky, but this was also while we were still evaluating VitePress, so it was considered a throw away script.

> This was run in each directory as needed, if `slug` or `permalink` is controlling your nested pathing, this problem will be more complex to handle.

This was run for our main folder of docs as well as our `releases` folder and we have successfully renamed files.

## Broken links are build failures

VitePress is more strict with issues than Jekyll. This is actually a good thing, especially as your site content grows. VitePress will fail building your site if in your markdown to link to a relative link that it can't see is a file.

This comes from the above design decision of not aliasing files to output paths. Markdown links like `[My link](/my-cool-page)` needs to be able to see `my-cool-page.md`. This means if you move or rename a file, it will break if something else links to it. Jekyll got around this by allowing the use of `permalink` and `slug` which is great for flexibility, but means at build time it can't be sure (without a lot more work) if the relative path won't be valid.

There are drawbacks to this though. If you host multiple resources under the same root path as your VitePress site and you want to reference this, I'm not sure you will be able to. You might have to resort to absolute URLs to link out to resources like this. And since VitePress doesn't alias any paths, it means your hosting environment will need to do this.

## Syntax issues

Jekyll is very forgiving when it comes to content that is passed around as straight html and put in various places using Liquid. For example if you have the following HTML in an `include` for Jekyll.

```html
<p>This solution is <50 lines of code</p>
```

Jekyll will just copy it and not bother you about the invalid HTML issues of having a `less-than (<)` in the middle of a `<p>` element. VitePress won't however, and you'll need to correctly use `&lt;` and `&gt;` encoded symbols appropriately.

## Include HTML

Another issue is the difference of how to reuse content. In Jekyll, you would use `{% include my/path/to/file.html %}`. This will likely show up in errors like `[vite:vue] Duplicate attribute`.

Instead in VitePress, an include of straight HTML will require migrating that content to a Vue component.

For example, if we have content file `catchphrase.html` like the following.

```html
<div>
    <h4>Catchphrase</h4>
    <p>It's.. what I do..</p>
</div>
```

We would need to wrap this in a Vue component like `catchphrase.vue`:

```html
<template>
    <div>
        <h4>Catchphrase</h4>
        <p>It's.. what I do..</p>
    </div>
</template>
<script>
    export default {
        name: "catchphrase"
    }
</script>

<style scoped>

</style>
```

Then it would need to be imported. This can be declared globally in the vitepress theme config or adhoc in the consuming Markdown file itself.

```markdown
<script setup>
import catchphrase from './catchphrase.vue';
</script>

<catchphrase />
```

The `<catchphrase />` is where it is injected into the output. For HTML so simple, this could be instead converted to Markdown and used the same way.

```markdown
## Catchphrase
it's.. what I do..
```

And then used:

```markdown
<script setup>
import catchphrase from './catchphrase.md';
</script>

<catchphrase />
```

## Jekyll markdownify redundant
Something similar is done in Jekyll, but with the use of Liquid filters.

```markdown
{% capture projects %}
{% include web-new-netfx.md %}
{% endcapture %} 
{{ projects | markdownify }}
```

This use of `capture` and passing the content to be converted is done by default when importing.

```markdown
<script setup>
import netfxtable from './.vitepress/includes/web-new-netfx.md';
</script>

<netfxtable />
```

If the module is declared global, then only the `<netfxtable />` is needed anywhere in your site to embed the content.

## Templating syntax the same but different

When moving from Jekyll to VitePress, I came across errors like `Cannot read property 'X' of undefined`. It was referring to some example code in a page we had that looked something like this.

```markdown
Content text here with templating code example below.

    Value: {{X.prop}}

More explanation here.
```

This error came about because we didn't religiously fence our code examples. Jekyll let us get away with this and actually produced the visuals we wanted without trying to render value in the handlebars `{{ }}` syntax.

VitePress only ignores these if they are in a code fence using the triple tilda syntax OR if the content is within a `:::v-pre` block.

## Replacing `raw` and `endraw`

Since some of our documentation used handlebar syntax in example code, we needed a way for Jekyll to ignore these statements and just present our code.
`raw` and `endraw` were used which were usually wrapping code blocks. VitePress doesn't have a problem with this syntax which means the `{% raw %}` statements were included in our page which we didn't want.

This was a matter of finding where these were used in all our documents and replace them with `::: v-pre` blocks as needed.

## Sidebar

This was a different situation. In Jekyll, we had created a `SideBar.md` file to help us render a left hand side menu of contents for the docs site. 
In VitePress's default theme, we could provide a JSON representation and the client would then dynamically populate it further using different levels of headings.

To do this, we used a simple NodeJs script that used the `markdown-it` library to parse the MarkDown itself and produce the expected JSON.

Loading the file and extracting the elements we needed was quite straight forward.

```js
let fs = require('fs')
let MarkdownIt = require('markdown-it');
let md = new MarkdownIt();

let content = fs.readFileSync('SideBar.md','utf8')
let res = md.parse(content).filter((element, index) => {
    return (element.level == '3' || element.level == '5') && element.type == 'inline';
});
```

Now we had `res` that contained all the information we would need, we can iterate the array, transforming the data as we go.

```js
let sidebarObjs = {
    '/': []
};
var lastHeading = null;
var lastHeadingIndex = -1;
for(let i = 0; i < res.length; i++) {
    let item = res[i];
    if(item.level == '3') {
        lastHeading = item.content;
        sidebarObjs['/'].push({
            text: lastHeading,
            children: []
        })
        lastHeadingIndex++;
        continue;
    }
    let text = item.children[1].content;
    let link = item.children[0].attrs[0][1];
    sidebarObjs['/'][lastHeadingIndex].children.push({
        text: text,
        link: link
    })
}

fs.writeFileSync('SideBar_format.json',JSON.stringify(sidebarObjs),'utf-8')
```

Once working, we later split this into multiple menus and condensed what we already had to make the menu more manageable.

## Problems we worked around

That covers the bulk of the changes we made that prevented us from running, building and deploying our application, however there were some short comings that we had to work around outside of VitePress itself.
There were 2 main sticking points for which we had to come up with a creative work around.

- Client router always appending `.html` (breaking clean URLs)
- Failing to route to paths with addition dots (`.`) in the URL

Hosting clean URLs can be done from the server side, so we used AWS CloudWatch functions to rewrite the request to append the `.html` in the backend if it was not provided.
However, VitePress still generated all page links with `.html`, creating multiple paths for the single document which can cause problems with other processes such as search indexing.

Since our documentation is something we edit quite frequently, we didn't want to be stuck in limbo due to these two problems while we workout and propose how VitePress itself might allow for such a setup.
Our solution **is NOT recommended** since it will be quite brittle which we have accepted. We found the 2 locations in the VitePress source that were causing this problem during static site generation. 
Thankfully, the logic is simple and we can remove 2 lines of code during the `npm install` phase out of our `node_modules` directory so that our CI builds would be consistent.

```js
const fs = require('fs');
const glob = require('glob');
let js = 'node_modules/vitepress/dist/client/app/router.js';
fs.writeFileSync(js, fs.readFileSync(js, 'utf8').replace("url.pathname += '.html';", ''))

glob('node_modules/vitepress/dist/node/serve-*.js',{},(err,files) =>{
    let file = files[0];
    fs.writeFileSync(file,fs.readFileSync(file,'utf8').replace("cleanUrl += \".html\";",''))
})

console.log('Completed post install process...')
```
> NOT RECOMMENDED

If you have standard `.html` paths on your existing site, you won't need the above. It is just a temporary workaround for getting clean URLs, which is again, *not recommended*.

## Verdict

While we have made the jump to VitePress, it is still young and under heavy development.
There is a good chance that some of the behaviour and lacking features will make this not a viable option for migrating off Jekyll.

And we knew this going in as it is very clearly outlined on the front page of their docs:

![](img/posts/jekyll-to-vitepress/vitepress-warning.png)

However, while there are still outstanding issues, the developer experience of Vue 3 combined with Vite and an SSG theme 
aimed at producing documentation is extremely compelling.
The work Even You and the VitePress community are doing is something to look out for as currently we believe it offers 
one of the best content heavy site development experiences currently possible.

## ServiceStack a Sponsor of Vue's Evan You

As maintainers of several [Vue & .NET Project Templates](https://docs.servicestack.net/templates-vue), we're big fans of 
Evan's work creating Vue, Vite and his overall stewardship of their surrounding ecosystems which greatly benefits from his 
master design skills and fine attention to detail in both library and UI design who has a talent in creating inherently 
simple technologies that progressively scales up to handle the complexest of Apps.

We believe in and are excited for the future of Vue and Vite that to show our support ServiceStack is now a sponsor of
[Evan You on GitHub sponsors](https://github.com/sponsors/yyx990803) 🎉 🎉 
