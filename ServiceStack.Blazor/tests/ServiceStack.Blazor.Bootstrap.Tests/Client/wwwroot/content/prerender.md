---
title: Improving UX with Prerendering
---

> Why does this page load so fast?

### Blazor WASM trade-offs

Blazor WASM enables reuse of C# skills, tooling & libraries offers a compelling advantage for .NET teams, so much so
it's become our preferred technology for developing internal LOB applications as it's better able to reuse existing
C# investments in an integrated SPA Framework utilizing a single toolchain.

It does however comes at a cost of a larger initial download size and performance cost resulting in a high Time-To-First-Render (TTFR)
and an overall poor initial User Experience when served over the Internet, that's further exacerbated over low speed Mobile connections.

This is likely an acceptable trade-off for most LOB applications served over high-speed local networks but may not be a
suitable choice for public Internet sites _(an area our other [jamstacks.net](https://jamstacks.net) templates may serve better)_.

As an SPA it also suffers from poor SEO as content isn't included in the initial page and needs to be rendered in the browser after 
the App has initialized. For some content heavy sites this can be a deal breaker either requiring proxy rules so content pages 
are served by a different SEO friendly site or otherwise prohibits using Blazor WASM entirely.

### Improving Startup Performance

The solution to both issues is fairly straightforward, by utilizing the mainstay solution behind
[Jamstack Frameworks](https://jamstack.org/generators/) and prerender content at build time.

We know what needs to be done, but how best to do it in Blazor WASM? Unfortunately the
[official Blazor WASM prerendering guide](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/prerendering-and-integration?view=aspnetcore-6.0&pivots=webassembly)
isn't actually a prerendering solution, as is typically used to describe
[Static Site Generators (SSG)](https://www.netlify.com/blog/2020/04/14/what-is-a-static-site-generator-and-3-ways-to-find-the-best-one/)
prerendering static content at build-time, whilst Blazor WASM prerendering docs instead describes
a [Server-Side-Rendering (SSR)](https://www.omnisci.com/technical-glossary/server-side-renderings) solution mandating the additional 
complexity of maintaining your Apps dependencies in both client and server projects. Unfortunately this approach also wont yield an 
optimal result since prerendering is typically used so Apps can host their SSG content on static file hosts, instead SSR does the 
opposite whose forced runtime coupling to the .NET Server Host prohibits Blazor WASM Apps from being served from a CDN.

As this defeats [many of the benefits](hosting) of a Blazor WASM Jamstack App in the first place, we've instead opted for a more optimal 
solution that doesn't compromise its CDN hostability.

### Increasing Perceived Performance

We've little opportunity over improving the startup time of the real C# Blazor App beyond hosting its static assets on CDN edge caches,
but ultimately what matters is [perceived performance](https://marvelapp.com/blog/a-designers-guide-to-perceived-performance/) which
we do have control over given the screen for a default Blazor WASM project is a glaring white screen flash:

![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/jamstack/blazor-wasm/loading-default.png)

The longer users have to wait looking at this black loading screen without signs of progress, the more they'll associate your site
with taking forever to load.

One technique many popular sites are using to increase perceived performance is to use content placeholders in place of real-content
which gives the impression that the site has almost loaded and only requires a few moments more for the latest live data to be slotted in. 

As an example here's what YouTube content placeholders mimicking the page layout looks like before the real site has loaded:

![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/jamstack/youtube-placeholder.png)

But we can do even better than an inert content placeholder, and load a temporary chrome of our App. But as this needs to be done
before Blazor has loaded we need to implement this with a sprinkling of HTML + JS.

First thing we need to do is move the scoped styles of our Apps
[MainLayout](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/Shared/MainLayout.razor) and
[NavMenu](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/Shared/NavMenu.razor) into an external
[main-layout.css](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/wwwroot/css/main-layout.css) so our temp
App chrome can use it.

Then in our [/wwwroot/index.html](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/wwwroot/index.html) anything
between `<div id="app"></div>` is displayed whilst our Blazor App is loading, before it's replaced with the real App.

So Here we just paste in the **MainLayout** markup:

```html
<div id="app">
    <!-- loading: render temp static app chrome to improve perceived performance -->
    <div id="app-loading" class="main-layout page">
        <div class="sidebar">
            <div class="top-row navbar navbar-dark">
                <a class="navbar-brand ps-4" href="/">MyApp</a>
                <button class="navbar-toggler">
                    <span class="navbar-toggler-icon"></span>
                </button>
            </div>
            <div class="collapse">
                <ul class="nav flex-column"></ul>
            </div>
        </div>
        <div class="main">
            <div class="main-top-row px-4">
                <ul class="nav nav-pills"></ul>
                <a href="signin?return=docs/deploy" class="btn btn-outline-primary">
                    Login
                </a>
            </div>
            <div class="content px-4">
                <!--PAGE-->
                <div class="spinner-border float-start mt-2 me-2" role="status">
                    <span class="sr-only"></span>
                </div>
                <h1 style="font-size:36px">
                    Loading...
                </h1>
                <!--/PAGE-->
            </div>
        </div>
    </div>
</div>
```

Less our App's navigation menus which we'll dynamically generate with the splash of JS below:

```js
const SIDEBAR = `
    Home,home,/$
    Counter,plus,/counter
    Todos,clipboard,/todomvc
    Bookings CRUD,calendar,/bookings-crud
    Call Hello,transfer,/hello$
    Call HelloSecure,shield,/hello-secure
    Fetch data,list-rich,/fetchdata
    Admin,lock-locked,/admin
    Login,account-login,/signin
`
const TOP = `
    0.40 /mo,dollar,/docs/hosting
    Prerendering,loop-circular,/docs/prerender
    Deployments,cloud-upload,/docs/deploy
`

const path = location.pathname
const NAV = ({ label, cls, icon, route, exact }) => `<li class="nav-item${cls}">
    <a href="${route}" class="nav-link${(exact ? path==route : path.startsWith(route)) ? ' active' : ''}">
        <span class="oi oi-${icon}" aria-hidden="true"></span> ${label}
    </a></li>`
const renderNav = (csv,f) => csv.trim().split(/\r?\n/g).map(s => NAV(f.apply(null,s.split(',')))).join('')
const $1 = s => document.querySelector(s)

$1('#app-loading .sidebar .nav').innerHTML = renderNav(SIDEBAR, (label, icon, route) => ({
    label, cls: ` px-3${route == SIDEBAR[0].route ? ' pt-3' : ''}`,
    icon, route: route.replace(/\$$/, ''), exact: route.endsWith('$')
}))

$1('#app-loading .main-top-row .nav').innerHTML = renderNav(TOP, (label, icon, route) => ({
    label, cls: '', icon, route: route.replace(/\$$/, ''), exact: route.endsWith('$')
}))
```

Which takes care of both rendering the top and sidebar menus as well as highlighting the active menu for the active
nav item being loaded, and because we're rendering our real App navigation with real links, users will be able to navigate
to the page they want before our App has loaded.

So you can distinguish a prerendered page from a Blazor rendered page we've added a **subtle box shadow** to prerendered content
which you'll see initially before being reverting to a flat border when the Blazor App takes over and replaces the entire page:

```html
<style>
#app-loading .content { box-shadow: inset 0 4px 4px 0 rgb(0 0 0 / 0.05) }
</style>
```

With just this, every page now benefits from an instant App chrome to give the perception that our App has loaded instantly
before any C# in our Blazor App is run. E.g. here's what the [Blazor Counter](/counter) page looks like while it's loading:

![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/jamstack/blazor-wasm/loading.png)

If you click refresh the [/counter](/counter) page a few times you'll see the new loading screen prior to the Counter page being available.

Our App is now in a pretty shippable state with decent UX of a loading page that looks like it loaded instantly instead
of the "under construction" Loading... page from the default Blazor WASM project template.

It's not quite a zero maintenance solution but still fairly low maintenance as only the `SIDEBAR` and `TOP` csv lists
need updating when add/removing menu items.

### Improving UX with Prerendering

We'll now turn our focus to the most important page in our App, the [Home Page](/) which is the page most people will see
when loading the App from the first time.

With the above temp App chrome already in place, a simple generic pre-rendering solution to be able to load any prerendered
page is to check if any prerendered content exists in the
[/prerender](https://github.com/NetCoreTemplates/blazor-wasm/tree/gh-pages/prerender)
folder for the current path, then if it does replace the default index.html `Loading...` page with it:

```js
const pagePath = path.endsWith('/') 
    ? path.substring(0, path.length - 2) + '/index.html' 
    : path
fetch(`/prerender${pagePath}`)
    .then(r => r.text())
    .then(html => {
        if (html.indexOf('<!DOCTYPE html>') >= 0) return // ignore CDN 404.html
        const pageBody = $1('#app-loading .content')
        if (pageBody) 
            pageBody.innerHTML = `<i hidden data-prerender="${path}"></i>` + html
    })
    .catch(/* no prerendered content found for this path */)
```

We also tag which path the prerendered content is for and provide a JS function to fetch the prerendered content
which we'll need to access later in our App:

```html
<script>
/* Loading */
window.prerenderedPage = function () {
    const el = document.querySelector('#app-loading .content')
    return el && el.innerHTML || ''
}
</script>
```

We now have a solution in place to load pre-rendered content from the `/prerender` folder, but still need some way of generating it.

The solution is technology independent in that you can you use any solution your most comfortable with, (even manually construct
each prerendered page if preferred), although it's less maintenance if you automate and get your CI to regenerate it when it publishes
your App.

Which ever tool you choose would also need to be installed in your CI/GitHub Action if that's where it's run, so we've opted for
a dependency-free & least invasive solution by utilizing the existing Tests project, which has both great IDE tooling support and
can easily be run from the command-line and importantly is supported by the [bUnit](https://bunit.dev) testing library which we'll
be using to render component fragments in isolation.

To distinguish prerendering tasks from our other Tests we've tagged 
[PrerenderTasks.cs](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Tests/PrerenderTasks.cs)
with the `prerender` Test category. The only configuration the tasks require is the location of the `ClientDir` WASM Project 
defined in [appsettings.json](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Tests/appsettings.json) 
that's setup in the constructor.

The `Render<T>()` method renders the Blazor Page inside a `Bunit.TestContext` which it saves at the location 
specified by its `@page` directive.

```csharp
[TestFixture, Category("prerender")]
public class PrerenderTasks
{
    Bunit.TestContext Context;
    string ClientDir;
    string WwrootDir => ClientDir.CombineWith("wwwroot");
    string PrerenderDir => WwrootDir.CombineWith("prerender");

    public PrerenderTasks()
    {
        Context = new();
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        ClientDir = config[nameof(ClientDir)] 
            ?? throw new Exception($"{nameof(ClientDir)} not defined in appsettings.json");
        FileSystemVirtualFiles.RecreateDirectory(PrerenderDir);
    }

    void Render<T>(params ComponentParameter[] parameters) where T : IComponent
    {
        WriteLine($"Rendering: {typeof(T).FullName}...");
        var component = Context.RenderComponent<T>(parameters);
        var route = typeof(T).GetCustomAttribute<RouteAttribute>()?.Template;
        if (string.IsNullOrEmpty(route))
            throw new Exception($"Couldn't infer @page for component {typeof(T).Name}");

        var fileName = route.EndsWith("/") ? route + "index.html" : $"{route}.html";

        var writeTo = Path.GetFullPath(PrerenderDir.CombineWith(fileName));
        WriteLine($"Written to {writeTo}");
        File.WriteAllText(writeTo, component.Markup);
    }

    [Test]
    public void PrerenderPages()
    {
        Render<Client.Pages.Index>();
        // Add Pages to prerender...
    }
}
```

Being a unit test gives it a number of different ways it can be run, using any of the NUnit test runners, from the GUI 
integrated in C# IDEs or via command-line test runners like `dotnet test` which can be done with:

```bash
$ dotnet test --filter TestCategory=prerender 
```

To have CI automatically run it when it creates a production build of our App we'll add it to our Host `.csproj`:

```xml
<PropertyGroup>
    <TestsDir>$(MSBuildProjectDirectory)/../MyApp.Tests</TestsDir>
</PropertyGroup>
<Target Name="AppTasks" AfterTargets="Build" Condition="$(APP_TASKS) != ''">
    <CallTarget Targets="Prerender" Condition="$(APP_TASKS.Contains('prerender'))" />
</Target>
<Target Name="Prerender">
    <Exec Command="dotnet test --filter TestCategory=prerender --logger:&quot;console;verbosity=detailed&quot;" 
            WorkingDirectory="$(TestsDir)" />
</Target>
```

Which allows [GitHub Actions to run it](https://github.com/NetCoreTemplates/blazor-wasm/blob/9460ebf57d3e46af1680eb3a2ff5080e59d33a54/.github/workflows/release.yml#L80)
when it publishes the App with:

```bash
$ dotnet publish -c Release /p:APP_TASKS=prerender
```

Now when we next commit code, the GitHub CI Action will run the above task to generate our
[/prerender/index.html](https://github.com/NetCoreTemplates/blazor-wasm/blob/gh-pages/prerender/index.html) page
that now loads our [Home Page](/) instantly!

[![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/jamstack/blazor-wasm/home-prerendered.png)](/)

The only issue now is that the default Blazor template behavior will yank our pre-rendered page, once during loading
and another during Authorization. To stop the unwanted yanking we've updated the
[<Loading/>](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/Shared/Loading.razor) component
to instead load the prerendered page content if it's **for the current path**:

```razor
@inject IJSRuntime JsRuntime
@inject NavigationManager NavigationManager

@if (!string.IsNullOrEmpty(prerenderedHtml))
{
    @((MarkupString)prerenderedHtml)
}
else
{
    <div class=@CssUtils.ClassNames("spinner-border float-start mt-2 me-2", @class)>
        <span class="sr-only"></span>
    </div>
    <h1 style="font-size:36px">
        Loading...
    </h1>
}

@code {
    [Parameter]
    public string Message { get; set; } = "Loading...";

    [Parameter]
    public string @class { get; set; } = "";

    public string prerenderedHtml { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        var html = await JsRuntime.InvokeAsync<string>("prerenderedPage") ?? "";
        var currentPath = new Uri(NavigationManager.Uri).AbsolutePath;
        if (html.IndexOf($"data-prerender=\"{currentPath}\"") >= 0)
            prerenderedHtml = html;
    }
}
```

Whilst to prevent yanking by the Authorization component we'll also include the current page when rendering
the alternate layout with an `Authenticating...` text that will appear under the Login/Logout buttons on the top-right:

```xml
<AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
  <Authorizing>
    <p class="text-muted" style="float:right;margin:1rem 1rem 0 0">Authenticating...</p>
    <RouteView RouteData="@routeData" />
  </Authorizing>
</AuthorizeRouteView>
```

This last change brings us to the optimal UX we have now with the home page loading instantly whilst our Blazor App
is loading in the background that'll eventually replace the home page with its identical looking C# version except
for the **box-shadow under the top navigation** so you can tell when you're looking at the pre-rendered version
instead of the C# Blazor version.

### Prerendering Markdown Content

The other pages that would greatly benefit from prerendering are the Markdown `/docs/*` pages (like this one) that's implemented in
[Docs.razor](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/Pages/Docs.razor).

However to enable SEO friendly content our `fetch(/prerender/*)` solution isn't good enough as the initial page download
needs to contain the prerendered content, i.e. instead of being downloaded in after.

### PrerenderMarkdown Task

To do this our `PrerenderMarkdown` Task scans all `*.md` pages in the 
[content](https://github.com/NetCoreTemplates/blazor-wasm/tree/main/MyApp.Client/wwwroot/content)
directory and uses the same
[/MyApp.Client/MarkdownUtils.cs](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/MarkdownUtils.cs)
implementation [Docs.razor](https://github.com/NetCoreTemplates/blazor-wasm/blob/main/MyApp.Client/Pages/Docs.razor)
uses to generate the markdown and embeds it into the `index.html` loading page to generate the pre-rendered page:

```csharp
[Test]
public async Task PrerenderMarkdown()
{
    var srcDir = WwrootDir.CombineWith("content").Replace('\\', '/');
    var dstDir = WwrootDir.CombineWith("docs").Replace('\\', '/');
            
    var indexPage = PageTemplate.Create(WwrootDir.CombineWith("index.html"));
    if (!Directory.Exists(srcDir)) throw new Exception($"{Path.GetFullPath(srcDir)} does not exist");
    FileSystemVirtualFiles.RecreateDirectory(dstDir);

    foreach (var file in new DirectoryInfo(srcDir).GetFiles("*.md", SearchOption.AllDirectories))
    {
        WriteLine($"Converting {file.FullName} ...");

        var name = file.Name.WithoutExtension();
        var docRender = await Client.MarkdownUtils.LoadDocumentAsync(name, doc =>
            Task.FromResult(File.ReadAllText(file.FullName)));

        if (docRender.Failed)
        {
            WriteLine($"Failed: {docRender.ErrorMessage}");
            continue;
        }

        var dirName = dstDir.IndexOf("wwwroot") >= 0
            ? dstDir.LastRightPart("wwwroot").Replace('\\', '/')
            : new DirectoryInfo(dstDir).Name;
        var path = dirName.CombineWith(name == "index" ? "" : name);

        var mdBody = @$"
<div class=""prose lg:prose-xl min-vh-100 m-3"" data-prerender=""{path}"">
    <div class=""markdown-body"">
        {docRender.Response!.Preview!}
    </div>
</div>";
        var prerenderedPage = indexPage.Render(mdBody);
        string htmlPath = Path.GetFullPath(Path.Combine(dstDir, $"{name}.html"));
        File.WriteAllText(htmlPath, prerenderedPage);
        WriteLine($"Written to {htmlPath}");
    }
}

public class PageTemplate
{
    string? Header { get; set; }
    string? Footer { get; set; }

    public PageTemplate(string? header, string? footer)
    {
        Header = header;
        Footer = footer;
    }

    public static PageTemplate Create(string indexPath)
    {
        if (!File.Exists(indexPath))
            throw new Exception($"{Path.GetFullPath(indexPath)} does not exist");

        string? header = null;
        string? footer = null;

        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines(indexPath))
        {
            if (header == null)
            {
                if (line.Contains("<!--PAGE-->"))
                {
                    header = sb.ToString(); // capture up to start page marker
                    sb.Clear();
                }
                else sb.AppendLine(line);
            }
            else
            {
                if (sb.Length == 0)
                {
                    if (line.Contains("<!--/PAGE-->")) // discard up to end page marker
                    {
                        sb.AppendLine();
                        continue;
                    }
                }
                else sb.AppendLine(line);
            }
        }
        footer = sb.ToString();

        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(footer))
            throw new Exception($"Parsing {indexPath} failed, missing <!--PAGE-->...<!--/PAGE--> markers");

        return new PageTemplate(header, footer);
    }

    public string Render(string body) => Header + body + Footer;
}
```

Whilst the `wwwroot/index.html` is parsed with `PageTemplate` above who uses the resulting layout to generate pages 
within `<!--PAGE--><!--/PAGE-->` markers.

After it's also executed by the same MSBuild task run by GitHub Actions it prerenders all `/wwwroot/content/*.md` pages 
which are written to the [/wwwroot/docs/*.html](https://github.com/NetCoreTemplates/blazor-wasm/tree/gh-pages/docs) folder.

This results in the path to the pre-generated markdown docs i.e. [/docs/prerender](/docs/prerender) having the **exact same path** 
as its route in the Blazor App, which when exists, CDNs give priority to over the SPA fallback the Blazor App is loaded with.

It shares similar behavior as the home page where its pre-rendered content is initially loaded before it's replaced with the
C# version once the Blazor App loads. The difference is that it prerenders "complete pages" for better SEO & TTFR.

> Why does this page load so fast?

So to answer the initial question, this page loads so fast because a prerendered version is initially loaded from a CDN edge cache,
i.e. the same reason why [Jamstack.org](https://jamstack.org) SSG templates like our modern 
[nextjs.jamstacks.net](https://nextjs.jamstacks.net) and [vue-ssg.jamstacks.net](https://vue-ssg.jamstacks.net) 
exhibit such great performance and UX out-of-the-box.

We hope this technique serves useful in greatly improving the initial UX of Blazor Apps, a new Blazor App
with all this integrated can be created on the [Home Page](/)
