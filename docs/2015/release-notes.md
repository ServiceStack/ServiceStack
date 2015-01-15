# v4.0.36 Release Notes

## Xamarin Unified API Support

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xamarin-unifiedapi.png" align="right" vspace="10" width="300" />

We have a short release cycle this release to be able to release the [ServiceStack PCL ServiceClients](https://github.com/ServiceStackApps/HelloMobile) support for 
[Xamarin's Unified API](http://developer.xamarin.com/guides/cross-platform/macios/unified/) to everyone as quickly as possible. As [announced on their blog](http://blog.xamarin.com/xamarin.ios-unified-api-with-64-bit-support/), Xamarin has released the stable build of Xamarin.iOS Unified API with 64-bit support. As per [Apple's deadlines](https://developer.apple.com/news/?id=12172014b) **new iOS Apps** published after **February 1st** must include 64-bit support, this deadline extends to updates of **existing Apps** on **June 1st**. One of the benefits of upgrading is being able to share code between iOS and OSX Apps with Xamarin.Mac.

Support for Unified API was added in addition to the existing 32bit monotouch.dll which used the **MonoTouch** NuGet profile. Xamarin Unified API instead uses the new **Xamarin.iOS10** NuGet profile. For new Apps this works transparently where you can add a NuGet package reference and it will automatically reference the appropriate build. 

    PM> Install-Package ServiceStack.Client

Existing iOS proejcts should follow Xamarin's [Updating Existing iOS Apps](http://developer.xamarin.com/guides/cross-platform/macios/updating_ios_apps/) docs, whilst the HelloMobile project has docs on using [ServiceStack's ServiceClients with iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client).

## Add ServiceStack Reference meets Xamarin Studio!

Our enhancements to [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) continue, this time extended to support [Xamarin Studio!](http://xamarin.com/studio)

With the new [ServiceStackXS Add-In](http://addins.monodevelop.com/Project/Index/154) your Service Consumers can now generate typed DTO's of your remote ServiceStack Services directly from within Xamarin Studio, which together with the **ServiceStack.Client** NuGet package provides an effortless way to enable an end-to-end Typed API from within Xamarin C# projects.

### Installing ServiceStackXS

Installation is straightforward if you've installed Xamarin Add-ins before, just go to `Xamarin Studio -> Add-In Manager...` from the Menu and then search for `ServiceStack` from the **Gallery**:

![](https://github.com/ServiceStack/Assets/blob/master/img/servicestackvs/servicestack%20reference/ssxs-mac-install.gif)

### Adding a ServiceStack Reference

Once installed, adding a ServiceStack Reference is very similar to [ServiceStackVS in VS.NET](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#add-servicestack-reference) where you can just click on `Add -> Add ServiceStack Reference...` on the project's context menu to bring up the familiar Add Reference dialog. After adding the `BaseUrl` of the remote ServiceStack instance, click OK to add the generated DTO's to your project using the name specified:

![](https://github.com/ServiceStack/Assets/blob/master/img/servicestackvs/servicestack%20reference/ssxs-mac-add-reference.gif)

### Updating the ServiceStack Reference

As file watching isn't supported yet, to refresh the generated DTO's you'll need to click on its `Update ServiceStack Reference` from the items context menu.

### Developing with pleasure on Linux!

One of the nice benefits of creating an Xamarin Studio Add-in is that we're also able to bring the same experience to .NET Developers on Linux! Which works similar to OSX where you can install ServiceStackXS from the Add-in Gallery - Here's an example using Ubuntu:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/servicestack%20reference/ssxs-ubuntu-install.gif)

Then **Add ServiceStack Reference** is accessible in the same way:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/servicestack%20reference/ssxs-ubuntu-add-ref.gif)

## Sitemap Feature

A good SEO technique for helping Search Engines index your website is to tell them where the can find all your content using [Sitemaps](https://support.google.com/webmasters/answer/156184?hl=en). Sitemaps are basic xml documents but they can be tedious to maintain manually, more so for database-driven dynamic websites. 

The `SitemapFeature` reduces the effort required by letting you add Site Urls to a .NET collection of `SitemapUrl` POCO's. 
In its most basic usage you can populate a single Sitemap with urls of your Website Routes, e.g:

```csharp
Plugins.Add(new SitemapFeature
{
    UrlSet = db.Select<TechnologyStack>()
        .ConvertAll(x => new SitemapUrl {
            Location = new ClientTechnologyStack { Slug = x.Slug }.ToAbsoluteUri(),
            LastModified = x.LastModified,
            ChangeFrequency = SitemapFrequency.Weekly,
        })
});
```

The above example uses [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) to generate a collection of `SitemapUrl` entries containing Absolute Urls for all [techstacks.io Technology Pages](http://techstacks.io/tech). This is another good showcase for the [Reverse Routing available on Request DTO's](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing) which provides a Typed API for generating Urls without any additional effort.

Once populated your sitemap will be available at `/sitemap.xml` which looks like:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
<url>
  <loc>http://techstacks.io/the-guardian</loc>
  <lastmod>2015-01-14</lastmod>
  <changefreq>weekly</changefreq>
</url>
...
</urlset>
```

Which you can checkout in this [live Sitemap example](http://techstacks.io/sitemap-techstacks.xml).

### Multiple Sitemap Indexes

For larger websites, Sitemaps also support multiple [Sitemap indexes](https://support.google.com/webmasters/answer/75712?hl=en) which lets you split sitemap urls across multiple files. To take advantage of this in `SitemapFeature` you would instead populate the `SitemapIndex` collection with multiple `Sitemap` entries. An example of this is in the full [Sitemap used by techstacks.io](https://github.com/ServiceStackApps/TechStacks/blob/a114348e905b4334e93a5408c2fb76c5fb589501/src/TechStacks/TechStacks/AppHost.cs#L90-L128):

```csharp
Plugins.Add(new SitemapFeature
{
    SitemapIndex = {
        new Sitemap {
            AtPath = "/sitemap-techstacks.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<TechnologyStack>(q => q.OrderByDescending(x => x.LastModified))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientTechnologyStack { Slug = x.Slug }.ToAbsoluteUri(),
                    LastModified = x.LastModified,
                    ChangeFrequency = SitemapFrequency.Weekly,
                }),
        },
        new Sitemap {
            AtPath = "/sitemap-technologies.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<Technology>(q => q.OrderByDescending(x => x.LastModified))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientTechnology { Slug = x.Slug }.ToAbsoluteUri(),
                    LastModified = x.LastModified,
                    ChangeFrequency = SitemapFrequency.Weekly,
                })
        },
        new Sitemap
        {
            AtPath = "/sitemap-users.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<CustomUserAuth>(q => q.OrderByDescending(x => x.ModifiedDate))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientUser { UserName = x.UserName }.ToAbsoluteUri(),
                    LastModified = x.ModifiedDate,
                    ChangeFrequency = SitemapFrequency.Weekly,
                })
        }
    }
});
```

Which now generates the following `<sitemapindex/>` at [/sitemap.xml](http://techstacks.io/sitemap.xml):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
<sitemap>
  <loc>http://techstacks.io/sitemap-techstacks.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
<sitemap>
  <loc>http://techstacks.io/sitemap-technologies.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
<sitemap>
  <loc>http://techstacks.io/sitemap-users.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
</sitemapindex>
```

With each entry linking to the urlset for each Sitemap:

 - [techstacks.io/sitemap-techstacks.xml](http://techstacks.io/sitemap-techstacks.xml)
 - [techstacks.io/sitemap-technologies.xml](http://techstacks.io/sitemap-technologies.xml)
 - [techstacks.io/sitemap-users.xml](http://techstacks.io/sitemap-users.xml)

## Razor

Services can now specify to return Content Pages for HTML clients (i.e. browsers) by providing the `/path/info` to the Razor Page:

```csharp
public object Any(Request request)
{
    ...
    return new HttpResult(responseDto) {
        View = "/content-page.cshtml"
    }
}
```

`HttpResult.View` was previously limited to names of Razor Views in the `/Views` folder.

## [Techstacks](http://techstacks.io)

Whilst not specifically Framework features, we've added some features to [techstacks.io](http://techstacks.io) that may be interesting for ServiceStack Single Page App developers:

### Server Generated HTML Pages

Whilst we believe Single Page Apps offer the more responsive UI, we've also added a server html version of [techstacks.io](http://techstacks.io) which we serve to WebCrawlers like **Googlebot** so they're better able to properly index content in the AngularJS SPA website. It also provides a good insight into the UX difference between a Single Page App vs Server HTML generated websites. Since [techstacks.io](http://techstacks.io) is running on modest hardware (i.e. IIS on shared **m1.small** EC2 instance with a shared **micro** RDS PostgreSQL backend) the differences are more visible with the AngularJS version still being able to yield a snappy App-like experience whilst the full-page reloads of the Server HTML version is clearly visible on each request.

The code to enable this is in [ClientRoutesService.cs](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks.ServiceInterface/ClientRoutesService.cs) which illustrates a simple technique used to show different versions of your website which by default is enabled implicitly for `Googlebot` User Agents, or can be toggled explicitly between by visiting the routes below:

  - [techstacks.io?html=client](http://techstacks.io?html=client)
  - [techstacks.io?html=server](http://techstacks.io?html=server)

These links determine whether you'll be shown the AngularJS version or the Server HTML Generated version of the Website. We can see how this works by exploring how the technology pages are implemented which handle both the technology index:

  - http://techstacks.io/tech

as well as individual technology pages, e.g:

  - http://techstacks.io/tech/redis
  - http://techstacks.io/tech/servicestack

First we need to create empty Request DTO's to capture the client routes (as they were only previously configured in AngularJS routes):

```csharp
[Route("/tech")]
public class ClientAllTechnologies {}

[Route("/tech/{Slug}")]
public class ClientTechnology
{
    public string Slug { get; set; }
}
```

Then we implement ServiceStack Services for these routes. The `ShowServerHtml()` helper method is used to determine whether 
to show the AngularJS or Server HTML version of the website which it does by setting a permanent cookie when 
`techstacks.io?html=server` is requested (or if the UserAgent is `Googlebot`). 
Every subsequent request then contains the `html=server` Cookie and so will show the Server HTML version. 
Users can then go to `techstacks.io?html=client` to delete the cookie and resume viewing the default AngularJS version:

```csharp
public class ClientRoutesService : Service
{
    public bool ShowServerHtml()
    {
        if (Request.GetParam("html") == "client")
        {
            Response.DeleteCookie("html");
            return false;
        }

        var serverHtml = Request.UserAgent.Contains("Googlebot")
            || Request.GetParam("html") == "server";

        if (serverHtml)
            Response.SetPermanentCookie("html", "server");

        return serverHtml;
    }

    public object AngularJsApp()
    {
        return new HttpResult {
            View = "/default.cshtml"
        };
    }

    public object Any(ClientAllTechnologies request)
    {
        return !ShowServerHtml()
            ? AngularJsApp()
            : new HttpResult(base.ExecuteRequest(new GetAllTechnologies())) {
                View = "AllTech"
            };
    }

    public object Any(ClientTechnology request)
    {
        return !ShowServerHtml()
            ? AngularJsApp()
            : new HttpResult(base.ExecuteRequest(new GetTechnology { Reload = true, Slug = request.Slug })) {
                View = "Tech"
            };
    }
}
```

The difference between which Website to display boils down to which Razor page to render, where for AngularJS we return the `/default.cshtml` 
Home Page where the client routes then get handled by AngularJS. Whereas for the Server HTML version, it just renders the appropriate Razor View for that request.

The `base.ExecuteRequest(new GetAllTechnologies())` API lets you execute a ServiceStack Service internally by just passing the 
`GetAllTechnologies` Request DTO. The Resposne DTO returned by the Service is then passed as a view model to the `/Views/AllTech.cshtml` Razor View. 

AngularJS declarative HTML pages holds an advantage when maintaining multiple versions of a websites as porting AngularJS views to Razor is relatively 
straight-forward process, basically consisting of converting Angular `ng-attributes` to `@Razor` statements, as can be seen in the client vs server 
versions of [techstacks.io/tech](http://techstacks.io/tech) index page:

  - [/partials/tech/latest.html](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks/partials/tech/latest.html)
  - [/Views/Tech/AllTech.cshtml](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks/Views/Tech/AllTech.cshtml)

### Twitter Updates

Another way to increase user engagement of your website is by posting Twitter Updates, [techstacks.io](http://techstacks.io) does this whenever anyone adds a new Technology or Technology Stack by posting a status update to [@webstacks](https://twitter.com/webstacks). The [code to make authorized Twitter API requests](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks.ServiceInterface/TwitterUpdates.cs) ends up being fairly lightweight as it can take advantage of ServiceStack's built-in support for Twitter OAuth.

> We'd also love for others to Sign In and add their Company's Technology Stack on [techstacks.io](http://techstacks.io) so everyone can get a better idea what technologies everyone's using.

## ServiceStack.Text

CSV Serializer now supports serializing `List<dynamic>`:

```csharp
int i = 0;
List<dynamic> rows = new[] { "Foo", "Bar" }.Map(x => (object) new { Id = i++, Name = x });
rows.ToCsv().Print();
```

Or `List<object>`:

```csharp
List<object> rows = new[] { "Foo", "Bar" }.Map(x => (object) new { Id = i++, Name = x });
rows.ToCsv().Print();
```

Both will Print:

    Id,Name
    0,Foo
    1,Bar

## ServiceStackVS Updated 

[ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) received another minor bump, the latest version can be [downloaded from the Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7).

## Breaking Changes

### AuthProvider Validation moved to AuthFeature

Like other Plugin options the configuration of validating unique Emails as been moved from `AuthProvider.ValidateUniqueEmails` to:

```csharp
Plugins.Add(new AuthFeature(...) {
    ValidateUniqueEmails = true,
    ValidateUniqueUserNames = false
});
```

This includes the new `ValidateUniqueUserNames` option to specify whether or not the UserNames from different OAuth Providers should be unique (validation is disabled by default).

### PooledRedisClientsManager Db is nullable

In order to be able to specify what redis **DB** the `PooledRedisClientsManager` should use on the connection string (e.g: `localhost?db=1`) we've changed `PooledRedisClientsManager.Db` to be an optional `long?`. If you're switching between multiple Redis DB's in your Redis Clients you should explicitly specify what Db should be the default so that Redis Clients retrieved from the pool are automatically reset to that DB, with either:

```csharp
new PooledRedisClientsManager(initialDb:1);
```

or via the connection string:

```csharp
new PooledRedisClientsManager("localhost?db=1");
```

---

## [2014 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2014/release-notes.md)


