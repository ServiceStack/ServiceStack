## Go to [2016 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2016/release-notes.md)

---

# v4.0.50 Release Notes

This is primarily a bug fix release to 
[resolve issues from the last release](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2015/release-notes.md#v4048-issues)
that we wanted to get out before the holidays. This release also contains a number of performance improvements 
added in OrmLite to speed up your Data Access and 
[AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) results. 

Other changes in this release include:

#### New OnSessionFilter

You can intercept sessions after they've been resolved from the cache and modify them before they're used in
ServiceStack or other application code by overriding `OnSessionFilter()` in your AppHost, e.g:

```csharp
public override IAuthSession OnSessionFilter(IAuthSession session, string withSessionId)
{
    // Update User Session
    return base.OnSessionFilter(session, withSessionId);
}
```

This comes in useful when migrating existing sessions and populating properties with custom values.

#### Registered Type Filters on IAppHost

To make it easier for plugins to register 
[Typed Filters](https://github.com/ServiceStack/ServiceStack/wiki/Request-and-response-filters#typed-request-filters)
, their Registration APIs are now available on IAppHost as well, e.g:

```csharp
public interface IAppHost
{    
    /// <summary>
    /// Add Request Filter for a specific Request DTO Type
    /// </summary>
    void RegisterTypedRequestFilter<T>(Action<IRequest, IResponse, T> filterFn);

    /// <summary>
    /// Add Request Filter for a specific Response DTO Type
    /// </summary>
    void RegisterTypedResponseFilter<T>(Action<IRequest, IResponse, T> filterFn);

    /// <summary>
    /// Add Request Filter for a specific MQ Request DTO Type
    /// </summary>
    void RegisterTypedMessageRequestFilter<T>(Action<IRequest, IResponse, T> filterFn);

    /// <summary>
    /// Add Request Filter for a specific MQ Response DTO Type
    /// </summary>
    void RegisterTypedMessageResponseFilter<T>(Action<IRequest, IResponse, T> filterFn);
}
```

## [RedisReact](https://github.com/ServiceStackApps/RedisReact)

New Windows, OSX, Linux binaries published and 
[http://redisreact.servicestack.net](http://redisreact.servicestack.net) 
Live Demo updated with this November Release:

#### Connections with Authentication

Added support for password authentication when establishing connections with redis.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/updates/add-authentication.png)

#### Custom key console links

The **console** link now populates the console with the most appropriate command for each key type, e.g. clicking **console**
ok a Sorted Set Key (ZSET) populates the Web Console with `ZRANGE key 0 -1 WITHSCORES`.

## ServiceStack.Redis

#### RedisConfig.DefaultMaxPoolSize

You can easily configure the default pool size for `RedisManagerPool` and `PooledRedisClientManager` with
a global static configuration, e.g:

```csharp
RedisConfig.DefaultMaxPoolSize = 200;
```

**Changes:**

The `RedisManagerPool.MaxPoolSize` property is now read-only to reflect proper usage where it needs to be 
specified in the constructor otherwise it's ignored.

### New Redis APIs

New API's added to typed Redis Client to make available API's to resolve cache key for specific types, 
deprecate `SetEntry*` API's and replace them with more appropriately named `SetValue*`, allow typed API
to store and expire typed POCO's in 1 operation:

```csharp
public interface IRedisClient
{
    //Resolve cache key for specific Type and Id
    string UrnKey<T>(T value);
    string UrnKey<T>(object id);
    string UrnKey(Type type, object id);
}

public interface IRedisTypedClient
{
    //resolve cache key used for a typed instance
    string UrnKey(T value);
    
    //Deprecate SetEntry* API's 
    [Obsolete("Use SetValue()")]
    void SetEntry(string key, T value);
    [Obsolete("Use SetValue()")]
    void SetEntry(string key, T value, TimeSpan expireIn);
    [Obsolete("Use SetValueIfNotExists()")]
    bool SetEntryIfNotExists(string key, T value);

    //Replaces above SetEntry* API's
    void SetValue(string key, T entity);
    void SetValue(string key, T entity, TimeSpan expireIn);
    bool SetValueIfNotExists(string key, T entity);
    bool SetValueIfExists(string key, T entity);

    //Save and expire an entity in 1 operation
    T Store(T entity, TimeSpan expireIn);
}
```

### ServiceStack.Text

To improve the usefulness of mocking HTTP Requests, the request body is now passed in the Results Filter
so the Request Body can be inspected, e.g:

```csharp
using (new HttpResultsFilter
{
    StringResultFn = (webReq, reqBody) =>
    {
        if (reqBody != null && reqBody.Contains("{\"a\":1}")) 
            return "mocked-by-body";

        return webReq.RequestUri.ToString().Contains("google")
            ? "mocked-google"
            : "mocked-yahoo";
    }
})
{
    "http://yahoo.com".PostJsonToUrl(json: "{\"a\":1}") //= mocked-by-body
    
    "http://google.com".GetJsonFromUrl() //= mocked-google
    "http://yahoo.com".GetJsonFromUrl()  //= mocked-yahoo
}
```

Previously [inspecting the Request Body was not possible](http://stackoverflow.com/a/31631039/85785). 
Thanks to [@georgehemmings](https://github.com/georgehemmings) for adding this feature.

# v4.0.48 Release Notes

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/servicestack-aws-banner-420.png)

In this release we've started increasing ServiceStack's value beyond its primary focus of simple, fast and productive 
libraries and started looking towards improving integration with outside environments for hosting ServiceStack Services. 
This release marks just the beginning, we'll continue enhancing the complete story around developing, deploying and 
hosting your ServiceStack solutions ensuring it provides seamless integration with the best Single Page Apps, Mobile/Desktop 
technologies and Cloud/Hosting Services that share our simplicity, performance and value-focused goals that we believe 
provide the best return on effort and have the most vibrant ecosystems. 

## .NET before Cloud Services

One thing we've missed from being based on .NET is its predisposition towards Windows-only technologies, missing out on
all the industrial strength server solutions that are being primarily developed for hosting on Linux. This puts .NET 
at a disadvantage to other platforms which have first-class support for using the best technologies at their discretion, 
which outside of .NET, are primarily running on Linux servers. We've historically ignored this bias in .NET and have
always focused on simple technologies we've evaluated that provide the best value. Often this means we've had to maintain 
rich .NET clients ourselves to get a great experience in .NET which is what led us to develop first-class 
[Redis client](https://github.com/ServiceStack/ServiceStack.Redis) and why 
[OrmLite](https://github.com/ServiceStack/ServiceStack.Redis)
has first-class support for major RDBMS running on Linux inc. PostgreSQL, MySql, Sqlite, Oracle and Firebird - 
all the while ensuring ServiceStack's libraries runs cross-platform on Mono/Linux.

## AWS's servicified platform and polyglot ecosystem

By building their managed platform behind platform-agnostic web services, Amazon have largely eroded this barrier. We
can finally tap into the same ecosystem [innovative Startups are using](http://techstacks.io/tech/amazon-ec2) with
nothing more than the complexity cost of a service call - the required effort even further reduced with native clients. 
Designing its services behind message-based APIs made it much easier for Amazon to enable a new polyglot world with 
[native clients for most popular platforms](https://aws.amazon.com/dynamodb/developer-resources/#SDK), putting .NET
on a level playing field with other platforms thanks to [AWS SDK for .NET's](http://aws.amazon.com/sdk-for-net/) 
well-maintained typed native clients. By providing its functionality behind well-defined services, for the first time 
we've seen in a long time, .NET developers are able to benefit from this new polyglot world where solutions and app 
logic written in other languages can be easily translated into .NET languages - a trait which has been invaluable whilst 
developing ServiceStack's integration support for AWS.

This also means features and improvements to reliability, performance and scalability added to its back-end servers benefit 
every language and ecosystem using them. .NET developers are no longer at a disadvantage and can now leverage the same 
platform Hacker Communities and next wave of technology leading Startups are built on, benefiting from the Tech Startup 
culture of sharing their knowledge and experiences and pushing the limits of what's possible today.

AWS offers unprecedented productivity for back-end developers, its servicified hardware and infrastructure encapsulates 
the complexity of managing servers at a high-level programmatic abstraction that's effortless to consume and automate. 
These productivity gains is why we've been running our public servers on AWS for more than 2 years. The vast array of 
services on offer means we have everything our solutions need within the AWS Console, our RDS managed PostgreSQL databases 
takes care of automated backups and software updates, ease of snapshots means we can encapsulate and backup the 
configuration of our servers and easily spawn new instances. AWS has made software developers more capable than ever, 
and with its first-class native client support leveling the playing field for .NET, there's no reason why 
[the next Instagram](http://highscalability.com/blog/2012/4/9/the-instagram-architecture-facebook-bought-for-a-cool-billio.html)
couldn't be built by a small team of talented .NET developers.

## ServiceStack + Amazon Web Services

We're excited to participate in AWS's vibrant ecosystem and provide first-class support and deep integration with AWS where 
ServiceStack's decoupled substitutable functionality now seamlessly integrates with popular AWS back-end technologies. 
It's now more productive than ever to develop and host ServiceStack solutions entirely on the managed AWS platform!

## ServiceStack.Aws

All of ServiceStack's support for AWS is encapsulated within the single **ServiceStack.Aws** NuGet package which 
references the latest modular AWSSDK **v3.1x** dependencies **.NET 4.5+** projects can install from NuGet with:

    PM> Install-Package ServiceStack.Aws

This **ServiceStack.Aws** NuGet package includes implementations for the following ServiceStack providers:

  - **[PocoDynamo](#pocodynamo)** - Exciting new declarative, code-first POCO client for DynamoDB with LINQ support
  - **[SqsMqServer](#sqsmqserver)** - A new [MQ Server](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) for invoking ServiceStack Services via Amazon SQS MQ Service
  - **[S3VirtualPathProvider](#S3virtualpathprovider)** - A read/write [Virtual FileSystem](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system) around Amazon's S3 Simple Storage Service
  - **[DynamoDbAuthRepository](#dynamodbauthrepository)** - A new [UserAuth repository](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization) storing UserAuth info in DynamoDB
  - **[DynamoDbAppSettings](#dynamodbappsettings)** - An [AppSettings provider](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings) storing App configuration in DynamoDB
  - **[DynamoDbCacheClient](#dynamodbcacheclient)** - A new [Caching Provider](https://github.com/ServiceStack/ServiceStack/wiki/Caching) for DynamoDB

> We'd like to give a big thanks to [Chad Boyd](https://github.com/boydc7) from Spruce Media for contributing the SqsMqServer implementation.

## [AWS Live Examples](http://awsapps.servicestack.net/)

To demonstrate the ease of which you can build AWS-powered solutions with ServiceStack we've rewritten 6 of our existing 
[Live Demos](https://github.com/ServiceStackApps/LiveDemos) to use a pure AWS managed backend using:

 - [Amazon DynamoDB](https://aws.amazon.com/dynamodb/) for data persistance
 - [Amazon S3](https://aws.amazon.com/s3/) for file storage
 - [Amazon SQS](https://aws.amazon.com/sqs/) for background processing of MQ requests 
 - [Amazon SES](https://aws.amazon.com/ses/) for sending emails
 
[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsapps.png)](http://awsapps.servicestack.net/)

### Simple AppHost Configuration

A good indication showing how simple it is to build ServiceStack + AWS solutions is the size of the 
[AppHost](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/AppHost.cs) which contains all the 
configuration for **5 different Apps** below utilizing all the AWS technologies listed above contained within a **single** 
ASP.NET Web Application where each application's UI and back-end Service implementation are encapsulated under 
their respective sub directories:

  - [/awsath](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/awsauth) -> [awsapps.servicestack.net/awsauth/](http://awsapps.servicestack.net/awsauth/)
  - [/emailcontacts](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/emailcontacts) -> [awsapps.servicestack.net/emailcontacts/](http://awsapps.servicestack.net/emailcontacts/)
  - [/imgur](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/imgur) -> [awsapps.servicestack.net/imgur/](http://awsapps.servicestack.net/imgur/)
  - [/restfiles](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/restfiles) -> [awsapps.servicestack.net/restfiles/](http://awsapps.servicestack.net/restfiles/)
  - [/todo](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/todo) -> [awsapps.servicestack.net/todo/](http://awsapps.servicestack.net/todo/)

## [AWS Razor Rockstars](http://awsrazor.servicestack.net/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsrazor.png)](http://awsrazor.servicestack.net/)

### Maintain Website Content in S3

The 
[implementation for AWS Razor Rockstars](https://github.com/ServiceStackApps/RazorRockstars/tree/master/src/RazorRockstars.S3) 
is kept with all the other ports of Razor Rockstars in the [RazorRockstars repository](https://github.com/ServiceStackApps/RazorRockstars).
The main difference that stands out with [RazorRockstars.S3](https://github.com/ServiceStackApps/RazorRockstars/tree/master/src/RazorRockstars.S3)
is that all the content for the App is **not** contained within project as all its Razor Views, Markdown Content, imgs, 
js, css, etc. are instead being served **directly from an S3 Bucket** :) 

This is simply enabled by overriding `GetVirtualFileSources()` and adding the new 
`S3VirtualPathProvider` to the list of file sources:

```csharp
public class AppHost : AppHostBase
{
    public override void Configure(Container container)
    {
        //All Razor Views, Markdown Content, imgs, js, css, etc are served from an S3 Bucket
        var s3 = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
        VirtualFiles = new S3VirtualPathProvider(s3, AwsConfig.S3BucketName, this);
    }
    
    public override List<IVirtualPathProvider> GetVirtualFileSources()
    {
        //Add S3 Bucket as lowest priority Virtual Path Provider 
        var pathProviders = base.GetVirtualFileSources();
        pathProviders.Add(VirtualFiles);
        return pathProviders;
    }
}
```

The code to import RazorRockstars content into an S3 bucket is trivial: we just use a local FileSystem provider to get 
all the files we're interested in from the main ASP.NET RazorRockstars projects folder, then write them to the configured 
S3 VirtualFiles Provider:

```csharp
var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
var s3 = new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, appHost);
            
var fs = new FileSystemVirtualPathProvider(appHost, "~/../RazorRockstars.WebHost".MapHostAbsolutePath());

var skipDirs = new[] { "bin", "obj" };
var matchingFileTypes = new[] { "cshtml", "md", "css", "js", "png", "jpg" };
//Update links to reference the new S3 AppHost.cs + RockstarsService.cs source code
var replaceHtmlTokens = new Dictionary<string, string> {  
    { "title-bg.png", "title-bg-aws.png" }, //S3 Title Background
    { "https://gist.github.com/3617557.js", "https://gist.github.com/mythz/396dbf54ce6079cc8b2d.js" },
    { "https://gist.github.com/3616766.js", "https://gist.github.com/mythz/ca524426715191b8059d.js" },
    { "RazorRockstars.WebHost/RockstarsService.cs", "RazorRockstars.S3/RockstarsService.cs" },        
};

foreach (var file in fs.GetAllFiles())
{
    if (skipDirs.Any(x => file.VirtualPath.StartsWith(x))) continue;
    if (!matchingFileTypes.Contains(file.Extension)) continue;

    if (file.Extension == "cshtml")
    {
        var html = file.ReadAllText();
        replaceHtmlTokens.Each(x => html = html.Replace(x.Key, x.Value));
        s3.WriteFile(file.VirtualPath, html);
    }
    else
    {
        s3.WriteFile(file);
    }
}
```

During the import we also update the links in the Razor `*.cshtml` pages to reference the new RazorRockstars.S3 content.

### Update S3 Bucket to enable LiveReload of Razor Views and Markdown

Another nice feature of having all content maintained in an S3 Bucket is that you can just change files in the S3 Bucket 
directly and have all App Servers immediately reload the Razor Views, Markdown content and static resources without redeploying. 

#### CheckLastModifiedForChanges

To enable this feature we just tell the Razor and Markdown plugins to check the source file for changes before displaying each page:

```csharp
GetPlugin<MarkdownFormat>().CheckLastModifiedForChanges = true;
Plugins.Add(new RazorFormat { CheckLastModifiedForChanges = true });
```

When this is enabled the View Engines checks the ETag of the source file to find out if it's changed, if it did,
it will rebuild and replace it with the new view before rendering it. 
Given [S3 supports object versioning](http://docs.aws.amazon.com/AmazonS3/latest/dev/Versioning.html) this feature
should enable a new class of use-cases for developing Content Heavy management sites with ServiceStack.

#### Explicit RefreshPage

One drawback of enabling `CheckLastModifiedForChanges` is that it forces a remote S3 call for each view before rendering it.
A more efficient approach is to instead notify the App Servers which files have changed so they can reload them once,
alleviating the need for multiple ETag checks at runtime, which is the approach we've taken with the 
[UpdateS3 Service](https://github.com/ServiceStackApps/RazorRockstars/blob/e159bb9d2e27eba7fc1a9ce1822b479602de8e0f/src/RazorRockstars.S3/RockstarsService.cs#L139):

```csharp
if (request.Razor)
{
    var kurtRazor = VirtualFiles.GetFile("stars/dead/cobain/default.cshtml");
    VirtualFiles.WriteFile(kurtRazor.VirtualPath, 
        UpdateContent("UPDATED RAZOR", kurtRazor.ReadAllText(), request.Clear));
    HostContext.GetPlugin<RazorFormat>().RefreshPage(kurtRazor.VirtualPath); //Force reload of Razor View
}

var kurtMarkdown = VirtualFiles.GetFile("stars/dead/cobain/Content.md");
VirtualFiles.WriteFile(kurtMarkdown.VirtualPath, 
    UpdateContent("UPDATED MARKDOWN", kurtMarkdown.ReadAllText(), request.Clear));
HostContext.GetPlugin<MarkdownFormat>().RefreshPage(kurtMarkdown.VirtualPath); //Force reload of Markdown
```

#### Live Reload Demo

You can test live reloading of the above Service with the routes below which modify Markdown and Razor views with the
current time:

  - [/updateS3](http://awsrazor.servicestack.net/updateS3) - Update Markdown Content
  - [/updateS3?razor=true](http://awsrazor.servicestack.net/updateS3?razor=true) - Update Razor View
  - [/updateS3?razor=true&clear=true](http://awsrazor.servicestack.net/updateS3?razor=true&clear=true) - Revert changes
  
> This forces a recompile of the modified views which greatly benefits from a fast CPU and is a bit slow on our 
Live Demos server that's running on a **m1.small** instance shared with 25 other ASP.NET Web Applications. 

## [AWS Imgur](http://awsapps.servicestack.net/imgur/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/imgur.png)](http://awsapps.servicestack.net/imgur/)

### S3VirtualPathProvider 

The backend 
[ImageService.cs](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/imgur/ImageService.cs) 
implementation for AWS Imgur has been rewritten to use the Virtual FileSystem instead of 
[accessing the FileSystem directly](https://github.com/ServiceStackApps/Imgur/blob/master/src/Imgur/Global.asax.cs).
The benefits of this approach is that with 
[2 lines of configuration](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/AppHost.cs#L44-L45)
we can have files written to an S3 Bucket instead:

```csharp
var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
VirtualFiles = new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, this);
```

If we comment out the above configuration any saved files are instead written to the local FileSystem (default).

The benefit of using managed S3 File Storage is better scalability as your App Servers can remain stateless, improved
performance as overhead of serving static assets can be offloaded by referencing the S3 Bucket directly and for even 
better responsiveness you can connect the S3 bucket to a CDN.

## [REST Files](http://awsapps.servicestack.net/restfiles/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/restfiles.png)](http://awsapps.servicestack.net/restfiles/)

REST Files GitHub-like explorer is another example that was 
[rewritten to use ServiceStack's Virtual File System](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/restfiles/FilesService.cs)
and now provides remote file management of an S3 Bucket behind a REST-ful API.

## [AWS Email Contacts](http://awsapps.servicestack.net/emailcontacts/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/emailcontacts.png)](http://awsapps.servicestack.net/emailcontacts/)

### SqsMqServer

The [AWS Email Contacts](http://awsapps.servicestack.net/emailcontacts/) example shows the same long-running 
[EmailContact Service](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/emailcontacts/EmailContactServices.cs#L81)
being executed from both HTTP and MQ Server by just 
[changing which url the HTML Form is posted to](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/emailcontacts/default.cshtml#L203):

```html
//html
<form id="form-emailcontact" method="POST"
    action="@(new EmailContact().ToPostUrl())" 
    data-action-alt="@(new EmailContact().ToOneWayUrl())">
    ...
    <div>
        <input type="checkbox" id="chkAction" data-click="toggleAction" />
        <label for="chkAction">Email via MQ</label>
    </div>
    ...   
</form>
```

> The urls are populated from a typed Request DTO using the [Reverse Routing Extension methods](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing)

Checking the **Email via MQ** checkbox fires the JavaScript handler below that's registered as [declarative event in ss-utils.js](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library#declarative-events):

```js
$(document).bindHandlers({
    toggleAction: function() {
        var $form = $(this).closest("form"), action = $form.attr("action");
        $form.attr("action", $form.data("action-alt"))
                .data("action-alt", action);
    }
});
```

The code to configure and start an SQS MQ Server is similar to [other MQ Servers](https://github.com/ServiceStack/ServiceStack/wiki/Messaging): 

```csharp
container.Register<IMessageService>(c => new SqsMqServer(
    AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1) {
    DisableBuffering = true, // Trade-off latency vs efficiency
});

var mqServer = container.Resolve<IMessageService>();
mqServer.RegisterHandler<EmailContacts.EmailContact>(ExecuteMessage);
mqServer.Start();
```

When an MQ Server is registered, ServiceStack automatically publishes Requests accepted on the "One Way" 
[pre-defined route](https://github.com/ServiceStack/ServiceStack/wiki/Routing#pre-defined-routes)
to the registered MQ broker. The message is later picked up and executed by a Message Handler on a background Thread.

## [AWS Auth](http://awsapps.servicestack.net/awsauth/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsauth.png)](http://awsapps.servicestack.net/awsauth/)

### DynamoDbAuthRepository

[AWS Auth](http://awsapps.servicestack.net/awsauth/) 
is an example showing how easy it is to enable multiple Auth Providers within the same App which allows Sign-Ins from 
Twitter, Facebook, GitHub, Google, Yahoo and LinkedIn OAuth providers, as well as HTTP Basic and Digest Auth and 
normal Registered User logins and Custom User Roles validation, all managed in DynamoDB Tables using 
the registered `DynamoDbAuthRepository` below: 

```csharp
container.Register<IAuthRepository>(new DynamoDbAuthRepository(db, initSchema:true));
```

Standard registration code is used to configure the `AuthFeature` with all the different Auth Providers AWS Auth wants 
to support:

```csharp
return new AuthFeature(() => new AuthUserSession(),
    new IAuthProvider[]
    {
        new CredentialsAuthProvider(),              //HTML Form post of UserName/Password credentials
        new BasicAuthProvider(),                    //Sign-in with HTTP Basic Auth
        new DigestAuthProvider(AppSettings),        //Sign-in with HTTP Digest Auth
        new TwitterAuthProvider(AppSettings),       //Sign-in with Twitter
        new FacebookAuthProvider(AppSettings),      //Sign-in with Facebook
        new YahooOpenIdOAuthProvider(AppSettings),  //Sign-in with Yahoo OpenId
        new OpenIdOAuthProvider(AppSettings),       //Sign-in with Custom OpenId
        new GoogleOAuth2Provider(AppSettings),      //Sign-in with Google OAuth2 Provider
        new LinkedInOAuth2Provider(AppSettings),    //Sign-in with LinkedIn OAuth2 Provider
        new GithubAuthProvider(AppSettings),        //Sign-in with GitHub OAuth Provider
    })
{
    HtmlRedirect = "/awsauth/",                     //Redirect back to AWS Auth app after OAuth sign in
    IncludeRegistrationService = true,              //Include ServiceStack's built-in RegisterService
};
```

### DynamoDbAppSettings

The AuthFeature looks for the OAuth settings for each AuthProvider in the registered
[AppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings), which for deployed **Release** builds 
gets them from multiple sources. Since `DynamoDbAppSettings` is registered first in a `MultiAppSettings` collection
it checks entries in the DynamoDB `ConfigSetting` Table first before falling back to local 
[Web.config appSettings](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/Web.config#L15): 

```csharp
#if !DEBUG
    AppSettings = new MultiAppSettings(
        new DynamoDbAppSettings(new PocoDynamo(AwsConfig.CreateAmazonDynamoDb()), initSchema:true),
        new AppSettings()); // fallback to Web.confg
#endif
```

Storing production config in DynamoDB reduces the effort for maintaining production settings decoupled from source code. 
The App Settings were populated in DynamoDB using
[this simple script](https://github.com/ServiceStackApps/AwsApps/blob/9d4d3c3dfbf127ce0890d0984c264e8b440abd3f/src/AwsApps/AdminTasks.cs#L58)
which imports its settings from a local [appsettings.txt file](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings#textfilesettings):

```csharp
var fileSettings = new TextFileSettings("~/../../deploy/appsettings.txt".MapHostAbsolutePath());
var dynamoSettings = new DynamoDbAppSettings(AwsConfig.CreatePocoDynamo());
dynamoSettings.InitSchema();

//dynamoSettings.Set("SmtpConfig", "{Username:REPLACE_USER,Password:REPLACE_PASS,Host:AWS_HOST,Port:587}");
foreach (var config in fileSettings.GetAll())
{
    dynamoSettings.Set(config.Key, config.Value);
}
```

#### ConfigSettings Table in DynamoDB

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/aws-configsettings.png)

## [AWS Todos](http://awsapps.servicestack.net/todo/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/todos.png)](http://awsapps.servicestack.net/todo/)

The [Backbone TODO App](http://todomvc.com/examples/backbone/) is a famous minimal example used as a "Hello, World" 
example to showcase and compare JavaScript client frameworks. The example also serves as a good illustration of the 
clean and minimal code it takes to build a simple CRUD Service utilizing a DynamoDB back-end with the new PocoDynamo client:

```csharp
public class TodoService : Service
{
    public IPocoDynamo Dynamo { get; set; }

    public object Get(Todo todo)
    {
        if (todo.Id != default(long))
            return Dynamo.GetItem<Todo>(todo.Id);

        return Dynamo.GetAll<Todo>();
    }

    public Todo Post(Todo todo)
    {
        Dynamo.PutItem(todo);
        return todo;
    }

    public Todo Put(Todo todo)
    {
        return Post(todo);
    }

    public void Delete(Todo todo)
    {
        Dynamo.DeleteItem<Todo>(todo.Id);
    }
}
```

As it's a clean POCO, the `Todo` model can be also reused as-is throughout ServiceStack in Redis, OrmLite, Caching, Config, DTO's, etc:

```csharp
public class Todo
{
    [AutoIncrement]
    public long Id { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }
    public bool Done { get; set; }
}
```

## [PocoDynamo](https://github.com/ServiceStack/PocoDynamo)

PocoDynamo is a highly productive, feature-rich, typed .NET client which extends 
[ServiceStack's Simple POCO life](http://stackoverflow.com/a/32940275/85785) 
by enabling re-use of your code-first data models with Amazon's industrial strength and highly-scalable 
NoSQL [DynamoDB](https://aws.amazon.com/dynamodb/).

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/pocodynamo/related-customer.png)

#### First class support for reusable, code-first POCOs

It works conceptually similar to ServiceStack's other code-first
[OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) and 
[Redis](https://github.com/ServiceStack/ServiceStack.Redis) clients by providing a high-fidelity, managed client that enhances
AWSSDK's low-level [IAmazonDynamoDB client](http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/UsingAWSsdkForDotNet.html), 
with rich, native support for intuitively mapping your re-usable code-first POCO Data models into 
[DynamoDB Data Types](http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Types.html). 

### PocoDynamo Features

#### Advanced idiomatic .NET client

PocoDynamo provides an idiomatic API that leverages .NET advanced language features with streaming API's returning
`IEnumerable<T>` lazily evaluated responses that transparently performs multi-paged requests behind-the-scenes as the 
result set is iterated. It high-level API's provides a clean lightweight adapter to
transparently map between .NET built-in data types and DynamoDB's low-level attribute values. Its efficient batched 
API's take advantage of DynamoDB's `BatchWriteItem` and `BatchGetItem` batch operations to perform the minimum number 
of requests required to implement each API.

#### Typed, LINQ provider for Query and Scan Operations

PocoDynamo also provides rich, typed LINQ-like querying support for constructing DynamoDB Query and Scan operations, 
dramatically reducing the effort to query DynamoDB, enhancing readability whilst benefiting from Type safety in .NET. 

#### Declarative Tables and Indexes

Behind the scenes DynamoDB is built on a dynamic schema which whilst open and flexible, can be cumbersome to work with 
directly in typed languages like C#. PocoDynamo bridges the gap and lets your app bind to impl-free and declarative POCO 
data models that provide an ideal high-level abstraction for your business logic, hiding a lot of the complexity of 
working with DynamoDB - dramatically reducing the code and effort required whilst increasing the readability and 
maintainability of your Apps business logic.

It includes optimal support for defining simple local indexes which only require declaratively annotating properties 
to index with an `[Index]` attribute.

Typed POCO Data Models can be used to define more complex Local and Global DynamoDB Indexes by implementing 
`IGlobalIndex<Poco>` or `ILocalIndex<Poco>` interfaces which PocoDynamo uses along with the POCOs class structure 
to construct Table indexes at the same time it creates the tables.

In this way the Type is used as a DSL to define DynamoDB indexes where the definition of the index is decoupled from 
the imperative code required to create and query it, reducing the effort to create them whilst improving the 
visualization and understanding of your DynamoDB architecture which can be inferred at a glance from the POCO's 
Type definition. PocoDynamo also includes first-class support for constructing and querying Global and Local Indexes 
using a familiar, typed LINQ provider.

#### Resilient

Each operation is called within a managed execution which transparently absorbs the variance in cloud services 
reliability with automatic retries of temporary errors, using an exponential backoff as recommended by Amazon. 

#### Enhances existing APIs

PocoDynamo API's are a lightweight layer modeled after DynamoDB API's making it predictable the DynamoDB operations 
each API calls under the hood, retaining your existing knowledge investment in DynamoDB. 
When more flexibility is needed you can access the low-level `AmazonDynamoDBclient` from the `IPocoDynamo.DynamoDb` 
property and talk with it directly.

Whilst PocoDynamo doesn't save you for needing to learn DynamoDB, its deep integration with .NET and rich support for 
POCO's smoothes out the impedance mismatches to enable an type-safe, idiomatic, productive development experience.

#### High-level features

PocoDynamo includes its own high-level features to improve the re-usability of your POCO models and the development 
experience of working with DynamoDB with support for Auto Incrementing sequences, Query expression builders, 
auto escaping and converting of Reserved Words to placeholder values, configurable converters, scoped client 
configurations, related items, conventions, aliases, dep-free data annotation attributes and more.

### Download

PocoDynamo is contained in ServiceStack's AWS NuGet package:

    PM> Install-Package ServiceStack.Aws
   
<sub>PocoDynamo has a 10 Tables [free-quota usage](https://servicestack.net/download#free-quotas) limit which is unlocked with a [license key](https://servicestack.net/pricing).</sub>
    
To get started we'll need to create an instance of `AmazonDynamoDBClient` with your AWS credentials and Region info:

```csharp
var awsDb = new AmazonDynamoDBClient(AWS_ACCESS_KEY, AWS_SECRET_KEY, RegionEndpoint.USEast1);
```

Then to create a PocoDynamo client pass the configured AmazonDynamoDBClient instance above:

```csharp
var db = new PocoDynamo(awsDb);
```

> Clients are Thread-Safe so you can register them as a singleton and share the same instance throughout your App

### Creating a Table with PocoDynamo

PocoDynamo enables a declarative code-first approach where it's able to create DynamoDB Table schemas from just your 
POCO class definition. Whilst you could call `db.CreateTable<Todo>()` API and create the Table directly, the recommended 
approach is instead to register all the tables your App uses with PocoDynamo on Startup, then just call `InitSchema()` 
which will go through and create all missing tables:

```csharp
//PocoDynamo
var db = new PocoDynamo(awsDb)
    .RegisterTable<Todo>();

db.InitSchema();

db.GetTableNames().PrintDump();
```

In this way your App ends up in the same state with all tables created if it was started with **no tables**, **all tables** 
or only a **partial list** of tables. After the tables are created we query DynamoDB to dump its entire list of Tables, 
which if you started with an empty DynamoDB instance would print the single **Todo** table name to the Console:

    [
        Todo
    ]

### Managed DynamoDB Client

Every request in PocoDynamo is invoked inside a managed execution where any temporary errors are retried using the 
[AWS recommended retries exponential backoff](http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ErrorHandling.html#APIRetries).

All PocoDynamo API's returning `IEnumerable<T>` returns a lazy evaluated stream which behind-the-scenes sends multiple
paged requests as needed whilst the sequence is being iterated. As LINQ APIs are also lazily evaluated you could use 
`Take()` to only download however the exact number results you need. So you can query the first 100 table names with:

```csharp
//PocoDynamo
var first100TableNames = db.GetTableNames().Take(100).ToList();
```

and PocoDynamo will only make the minimum number of requests required to fetch the first 100 results.

### PocoDynamo Examples

#### [DynamoDbCacheClient](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbCacheClient.cs)

We've been quick to benefit from the productivity advantages of PocoDynamo ourselves where we've used it to rewrite
[DynamoDbCacheClient](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbCacheClient.cs)
which is now just 2/3 the size and much easier to maintain than the existing 
[Community-contributed version](https://github.com/ServiceStack/ServiceStack/blob/22aca105d39997a8ea4c9dc20b242f78e07f36e0/src/ServiceStack.Caching.AwsDynamoDb/DynamoDbCacheClient.cs)
whilst at the same time extending it with even more functionality where it now implements the `ICacheClientExtended` API.

#### [DynamoDbAuthRepository](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbAuthRepository.cs)

PocoDynamo's code-first Typed API made it much easier to implement value-added DynamoDB functionality like the new
[DynamoDbAuthRepository](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbAuthRepository.cs)
which due sharing a similar code-first POCO approach to OrmLite, ended up being a straight-forward port of the existing
[OrmLiteAuthRepository](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Server/Auth/OrmLiteAuthRepository.cs)
where it was able to reuse the existing `UserAuth` and `UserAuthDetails` POCO data models.

#### [DynamoDbTests](https://github.com/ServiceStack/ServiceStack.Aws/tree/master/tests/ServiceStack.Aws.DynamoDbTests)

Despite its young age we've added a comprehensive test suite behind PocoDynamo which has become our exclusive client
for developing DynamoDB-powered Apps.

### [PocoDynamo Docs](https://github.com/ServiceStack/PocoDynamo)

This only scratches the surface of what PocoDynamo can do, comprehensive documentation is available in the 
[PocoDynamo project](https://github.com/ServiceStack/PocoDynamo) explaining how it compares to DynamoDB's AWSSDK client,
how to use it to store related data, how to query indexes and how to use its rich LINQ querying functionality to query
DynamoDB.

## [Getting started with AWS + ServiceStack Guides](https://github.com/ServiceStackApps/AwsGettingStarted)

Amazon offers managed hosting for a number of RDBMS and Caching servers which ServiceStack provides first-class
clients for. We've provided a number of guides to walk through setting up these services from your AWS account 
and connect to them with ServiceStack's typed .NET clients.

### [AWS RDS PostgreSQL and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/postgres-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-postgres-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/postgres-guide.md)

### [AWS RDS Aurora and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/aurora-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-aurora-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/aurora-guide.md)

### [AWS RDS MySQL and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-mysql-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

### [AWS RDS MariaDB and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mariadb-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-mariadb-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mariadb-guide.md)

### [AWS RDS SQL Server and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-sqlserver-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

### [AWS ElastiCache Redis and ServiceStack](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/redis-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-redis-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/redis-guide.md)

### [AWS ElastiCache Redis and ServiceStack](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/memcached-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-memcached-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/memcached-guide.md)

The source code used in each guide is also available in the [AwsGettingStarted](https://github.com/ServiceStackApps/AwsGettingStarted) repo.

# Community

We're excited to learn that [Andreas Niedermair](https://twitter.com/dittodhole) new ServiceStack book is now available!

## [Mastering ServiceStack](https://www.packtpub.com/application-development/mastering-servicestack) by [Andreas Niedermair](https://twitter.com/dittodhole)

[<img src="https://dz13w8afd47il.cloudfront.net/sites/default/files/imagecache/ppv4_main_book_cover/B02108_Mastering%20ServiceStack_.jpg" align=left vspace=10 hspace=20 />](https://www.packtpub.com/application-development/mastering-servicestack)

Mastering ServiceStack covers real-life problems that occur over the lifetime of a distributed system and how to 
solve them by deeply understanding the tools of ServiceStack. Distributed systems is the enterprise solution that 
provide flexibility, reliability, scaling, and performance. ServiceStack is an outstanding tool belt to create such a 
system in a frictionless manner, especially sophisticated designed and fun to use.

The book starts with an introduction covering the essentials, but assumes you are just refreshing, are a very fast 
learner, or are an expert in building web services. Then, the book explains ServiceStack's data transfer object patterns 
and teach you how it differs from other methods of building web services with different protocols, such as SOAP and SOA. 
It also introduces more low-level details such as how to extend the User Auth, message queues and concepts on how the 
technology works.

By the end of this book, you will understand the concepts, framework, issues, and resolutions related to ServiceStack.

## [TypeScript](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference)

![ServiceStack and TypeScript Banner](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/typescript-banner.png)

We've extended our existing TypeScript support for generating interface definitions for your DTO's with the much requested
`ExportAsTypes=true` option which instead generates non-ambient concrete TypeScript Types. This option is enabled at the 
new `/types/typescript` route so you can get both concrete types and interface defintions at:

  - [/types/typescript](http://test.servicestack.net/types/typescript) - for generating concrete module and classes
  - [/types/typescript.d](http://test.servicestack.net/types/typescript.d) - for generating ambient interface definitions
  
### Auto hyper-linked default HTML5 Pages

Any urls strings contained in default HTML5 Report Pages are automatically converted to a hyperlinks. 
We can see this in the new `/types` route which returns links to all different 
[Add ServiceStack Reference supported languages](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference#supported-languages):

```csharp
public object Any(TypeLinks request)
{
    var response = new TypeLinksResponse
    {
        Metadata = new TypesMetadata().ToAbsoluteUri(),
        Csharp = new TypesCSharp().ToAbsoluteUri(),
        Fsharp = new TypesFSharp().ToAbsoluteUri(),
        VbNet = new TypesVbNet().ToAbsoluteUri(),
        TypeScript = new TypesTypeScript().ToAbsoluteUri(),
        TypeScriptDefinition = new TypesTypeScriptDefinition().ToAbsoluteUri(),
        Swift = new TypesSwift().ToAbsoluteUri(),
        Java = new TypesJava().ToAbsoluteUri(),
    };
    return response;
}
```

Where any url returned are now converted into navigatable hyper links, e.g: http://test.servicestack.net/types

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/typelinks.png)](http://test.servicestack.net/types)

## New License Key Registration Option

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestack-license-env-var.png)

To simplify license key registration when developing and maintaining multiple ServiceStack solutions we've added a new
option where you can now just register your License Key once in `SERVICESTACK_LICENSE` Environment Variable. 
This lets you set it in one location on your local dev workstations and any CI and deployment Servers which then lets 
you freely create multiple ServiceStack solutions without having to register the license key with each project. 

> Note: you'll need to restart IIS or VS.NET to have them pickup the new Environment Variable.

## [Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system)

ServiceStack's 
[Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system)
provides a clean abstraction over file-systems enabling the flexibility to elegantly support a wide range of cascading 
file sources. We've extended this functionality even further in this release with a new read/write API that's 
now implemented in supported providers:

```csharp
public interface IVirtualFiles : IVirtualPathProvider
{
    void WriteFile(string filePath, string textContents);
    void WriteFile(string filePath, Stream stream);
    void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null);
    void DeleteFile(string filePath);
    void DeleteFiles(IEnumerable<string> filePaths);
    void DeleteFolder(string dirPath);
}
```

> Folders are implicitly created when writing a file to folders that don't exist

The new `IVirtualFiles` API is available in local FileSystem, In Memory and S3 Virtual path providers:

 - FileSystemVirtualPathProvider
 - InMemoryVirtualPathProvider
 - S3VirtualPathProvider

Whilst other read-only `IVirtualPathProvider` include:

 - ResourceVirtualPathProvider - .NET Embedded resources
 - MultiVirtualPathProvider - Combination of multiple cascading Virtual Path Providers

All `IVirtualFiles` providers share the same 
[VirtualPathProviderTests](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/tests/ServiceStack.Aws.Tests/S3/VirtualPathProviderTests.cs)
ensuring a consistent behavior where it's now possible to swap between different file storage backends with simple
configuration as seen in the [Imgur](#imgur) and [REST Files](#restfiles) examples.

### VirtualFiles vs VirtualFileSources

As typically when saving uploaded files you'd only want files written to a single explicit File Storage provider,
ServiceStack keeps a distinction between the existing read-only Virtual File Sources it uses internally whenever a 
static file is requested and the new `IVirtualFiles` which is maintained in a separate `VirtualFiles` property on 
`IAppHost` and `Service` base class for easy accessibility:

```csharp
public class IAppHost
{
    // Read/Write Virtual FileSystem. Defaults to Local FileSystem.
    IVirtualFiles VirtualFiles { get; set; }
    
    // Cascading number of file sources, inc. Embedded Resources, File System, In Memory, S3.
    IVirtualPathProvider VirtualFileSources { get; set; }
}

public class Service : IService //ServiceStack's convenient concrete base class
{
    //...
    public IVirtualPathProvider VirtualFiles { get; }
    public IVirtualPathProvider VirtualFileSources { get; }
}
```

Internally ServiceStack only uses `VirtualFileSources` itself to serve static file requests. 
The new `IVirtualFiles` is a clean abstraction your Services can bind to when saving uploaded files which can be easily
substituted when you want to change file storage backends. If not specified, `VirtualFiles` defaults to your local 
filesystem at your host project's root directory.

### Changes

To provide clear and predictable naming. some of the existing APIs were deprecated in favor of the new nomenclature:

 - `IAppHost.VirtualPathProvider` deprecated, renamed to `IAppHost.VirtualFileSources`
 - `IAppHost.GetVirtualPathProviders()` deprecated, renamed to `IAppHost.GetVirtualFileSources()`
 - `IWriteableVirtualPathProvider` deprecated, renamed to `IVirtualFiles`
 - `IWriteableVirtualPathProvider.AddFile()` deprecated, renamed to `WriteFile()`
 - `VirtualPath` no longer returns paths prefixed with `/` and VirtualPath of a Root directory is `null` 
    This affects `Config.ScanSkipPaths` which should no longer start with `/`, e.g: "node_modules/", "bin/", "obj/" 

These old API's have been marked `[Obsolete]` and will be removed in a future version. 
If you're using them, please upgrade to the newer APIs.

## HttpResult

### Custom Serialized Responses

The new `IHttpResult.ResultScope` API provides an opportunity to execute serialization within a custom scope, e.g. this can
be used to customize the serialized response of adhoc services that's different from the default global configuration with:

```csharp
return new HttpResult(dto) {
    ResultScope = () => JsConfig.With(includeNullValues:true)
};
```

Which enables custom serialization behavior by performing the serialization within the custom scope, equivalent to:

```csharp
using (JsConfig.With(includeNullValues:true))
{
    var customSerializedResponse = Serialize(dto);
}
```

### Cookies

New cookies can be added to HttpResult’s new `IHttpResult.Cookies` collection.

### VirtualFile downloads

The a new constructor overload lets you return a `IVirtualFile` download with:

```csharp
return new HttpResult(VirtualFiles.GetFile(targetPath), asAttachment: true);
```

### HttpError changes

The `HttpError` constructor that accepts a `HttpStatusCode` and a custom string description, e.g: 

```csharp
return new HttpError(HttpStatusCode.NotFound, "Custom Description");
return HttpError.NotFound("Custom Description");
```

Now returns the `HttpStatusCode` string as the ErrorCode instead of duplicating the error message, 
so now the populated `ResponseStatus` for the above custom HttpError returns:

```csharp
ResponseStatus {
    ErrorCode = "NotFound", // previous: Custom Description 
    Message = "Custom Description"
}
```

## [AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query)

We've added a new `%!` and `%NotEqualTo` implicit query convention which is now available on all Auto queries:

    /rockstars?age!=27
    /rockstars?AgeNotEqualTo=27
    
## Razor

New API's for explicitly refreshing a page:

    HostContext.GetPlugin<RazorFormat>().RefreshPage(file.VirtualPath);

Also available in Markdown Format:

    HostContext.GetPlugin<MarkdownFormat>().RefreshPage(file.VirtualPath);

New Error API's in base Razor Views for inspecting error responses: 

 - `GetErrorStatus()` - get populated Error `ResponseStatus`
 - `GetErrorHtml()` - get error info marked up in a structured html fragment

Both API's return `null` if there were no errors.

### Improved support for Xamarin.Mac

The ServiceStack.Client PCL provider for Xamarin.Mac now has an explicit reference **Xamarin.Mac.dll** which allows it 
to work in projects with the **Link SDK Assemblies Only**.

## Redis

Allow `StoreAsHash` behavior converting a POCO object into a string Dictionary to be overridden to control how POCOs
are stored in a Redis Hash type, e.g:

```csharp
RedisClient.ConvertToHashFn = o =>
{
    var map = new Dictionary<string, string>();
    o.ToObjectDictionary().Each(x => map[x.Key] = (x.Value ?? "").ToJsv());
    return map;
};

Redis.StoreAsHash(dto); //Uses above implementation
```

## RedisReact Browser Updates

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/order.png)

We've continued to add enhancements to Redis React Browser based on your feedback:

### Delete Actions

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/updates/delete-actions.png)

Delete links added on each key. Use the **delete** link to delete a single key or the **all** link to delete all
related keys currently being displayed.

### Expanded Prompt

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/updates/expanded-prompt.png)

Keys can now be edited in a larger text area which uses the full height of the screen real-estate available - 
this is now the default view for editing a key. Click the collapse icon when finished to return to the 
console for execution.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/updates/expand-prompt.png)

All Redis Console commands are now be edited in the expanded text area by clicking on the Expand icon 
on the right of the console.

### Clear Search

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/updates/clear-search.png)

Use the **X** icon in the search box to quickly clear the current search.

## OrmLite

### System Variables and Default Values

To provide richer support for non-standard default values, each RDBMS Dialect Provider now contains a 
`OrmLiteDialectProvider.Variables` placeholder dictionary for storing common, but non-standard RDBMS functionality. 
We can use this to declaratively define non-standard default values that works across all supported RDBMS's 
like automatically populating a column with the RDBMS UTC Date when Inserted with a `default(T)` Value: 

```csharp
public class Poco
{
    [Default(OrmLiteVariables.SystemUtc)]  //= {SYSTEM_UTC}
    public DateTime CreatedTimeUtc { get; set; }
}
```

OrmLite variables need to be surrounded with `{}` braces to identify that it's a placeholder variable, e.g `{SYSTEM_UTC}`.

### Parameterized SqlExpressions

Preview of a new parameterized SqlExpression provider that's now available for SQL Server and Oracle, opt-in with:

```csharp
OrmLiteConfig.UseParameterizeSqlExpressions = true;
```

When enabled, OrmLite instead uses Typed SQL Expressions inheriting from `ParameterizedSqlExpression<T>` to convert 
most LINQ expression arguments into parameterized variables instead of inline SQL Literals for improved RDBMS query 
profiling and performance. Note: this is a beta feature that's subject to change.

### Custom Load References

[Johann Klemmack](https://github.com/jklemmack) added support for selectively specifying which references you want to load, e.g:

```csharp
var customerWithAddress = db.LoadSingleById<Customer>(customer.Id, include: new[] { "PrimaryAddress" });

//Alternative
var customerWithAddress = db.LoadSingleById<Customer>(customer.Id, include: x => new { x.PrimaryAddress });
```

### T4 Templates

OrmLite's T4 templates for generating POCO types from an existing database schema has improved support for generating 
stored procedures thanks to [Richard Safier](https://github.com/rsafier).

## ServiceStack.Text

The new `ConvertTo<T>` on `JsonObject` helps dynamically parsing JSON into typed object, e.g. you can dynamically 
parse the following OData json response:

```json
{  
    "odata.metadata":"...",
    "value":[
        {  
            "odata.id":"...",
            "QuotaPolicy@odata.navigationLinkUrl":"...",
            "#SetQuotaPolicyFromLevel": { "target":"..." },
            "Id":"1111",
            "UserName":"testuser",
            "DisplayName":"testuser Large",
            "Email":"testuser@testuser.ca"
        }
    ]
}
```

By navigating down a JSON object graph with `JsonObject` then using `ConvertTo<T>` to convert a unstructured JSON object 
into a concrete POCO Type, e.g:

```csharp
var users = JsonObject.Parse(json)
    .ArrayObjects("value")
        .Map(x => x.ConvertTo<User>());

```

## Minor Features

- Server Events `/event-subscribers` route now returns all channel subscribers
- **PATCH** method is allowed in CORS Feature by default
- Swagger Summary for all servies under a particular route can be specified in `SwaggerFeature.RouteSummary` Dictionary
- New requested `FacebookAuthProvider.Fields` can be customized with `oauth.facebook.Fields`
- Added Swift support for `TreatTypesAsStrings` for returning specific .NET Types as Strings
- New `IAppSettings.GetAll()` added on all [AppSetting](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings) sources fetches all App config in a single call
- [ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) updated with ATS exception for React Desktop OSX Apps
- External NuGet packages updated to their latest stable version

## New Signed Packages

 - ServiceStack.Mvc.Signed
 - ServiceStack.Authentication.OAuth2.Signed

## v4.0.48 Issues

### TypeScript missing BaseUrl

The TypeScript Add ServiceStack Reference feature was missing the BaseUrl in header comments preventing updates. It's now been resolved 
[from this commit](https://github.com/ServiceStack/ServiceStack/commit/10728d1e746b0bf2f84e081eb6319d88ae974677) that's now
available in our [pre-release v4.0.49 MyGet packages](https://github.com/ServiceStack/ServiceStack/wiki/MyGet).

### ServiceStack.Mvc incorrectly references ServiceStack.Signed

The **ServiceStack.Mvc** project had a invalid dependency on **ServiceStack.Signed** which has been resolved 
[from this commit](https://github.com/ServiceStack/ServiceStack/commit/2f0946e8cb755103082de24949e35fc70f9f72ae) that's now
available in our [pre-release v4.0.49 MyGet packages](https://github.com/ServiceStack/ServiceStack/wiki/MyGet).

You can workaround this by manually removing the **ServiceStack.Signed** packages and adding the **ServiceStack** packages instead.

### Config.ScanSkipPaths not ignoring folders

The change of removing `/` prefixes from Virtual Paths meant folders ignored in `Config.ScanSkipPaths` were no
longer being ignored. It's important node.js-based Single Page App templates ignore `node_modules/` since trying
to scan it throws an error on StartUp when it reaches paths greater than Windows **260 char limit**. This is
fixed in the [pre-release v4.0.49 MyGet packages](https://github.com/ServiceStack/ServiceStack/wiki/MyGet).

You can also work around this issue in v4.0.48 by removing the prefix from `Config.ScanSkipPaths` folders 
in `AppHost.Configure()` manually with:

```csharp
SetConfig(new HostConfig { ... });

for (int i = 0; i < Config.ScanSkipPaths.Count; i++)
{
    Config.ScanSkipPaths[i] = Config.ScanSkipPaths[i].TrimStart('/');
}
```

# v4.0.46 Release Notes

## [React Desktop Apps!](https://github.com/ServiceStackApps/ReactDesktopApps)

We're super excited to announce React Desktop Apps which lets you re-use your Web Development skills to develop multi-platform 
Desktop Apps with React, ServiceStack and .NET.

React Desktop Apps take advantage of the adaptability, navigation and deep-linking benefits of a Web-based UI, the productivity 
and responsiveness of the [React framework](https://facebook.github.io/react/),
the performance, rich features and functionality contained in 
[ServiceStack](https://github.com/ServiceStack/ServiceStack/wiki) and the .NET Framework combined with the native experience and 
OS Integration possible from a Native Desktop App - all within a single VS .NET template!

![React Desktop Apps](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/react-desktop-splash.png)

The new **React Desktop Apps** template in 
[ServiceStackVS](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7) 
provides everything you need to package your **ServiceStack ASP.NET Web App** into a native Windows **Winforms App**, 
an OSX **Cocoa App** or cross-platform **Windows/OSX/Linux Console App** which instead of being 
embedded inside a Native UI, runs "headless" and launches the User's prefered Web Browser for its Web UI.

This Hybrid model of developing Desktop Apps with modern WebKit technologies offers a more productive and reusable alternative 
with greater development effort ROI than existing bespoke WPF Apps in XAML or Cocoa OSX Apps with Xcode. 
It enables full code reuse of your Web App whilst still allowing for platform specific .js, .css and C# specialization when needed. 
These advantages are also why GitHub also adopted a similar approach for their new cross-platform UI in their flagship 
[Windows and OSX Desktop Apps](http://githubengineering.com/cross-platform-ui-in-github-desktop/).

### Single Installer-less Executable

Each application is compiled into a **single xcopy-able executable** that's runnable directly without a 
Software install. The only pre-requisite is the .NET 4.5 Framework on Windows
(pre-installed on recent versions) or 
[Mono for Linux](http://www.mono-project.com/docs/getting-started/install/linux/). 
The OSX Cocoa [Xamarin.Mac](https://xamarin.com/mac) App has the option to bundle the Mono runtime alleviating the need for 
users to have an existing install of Mono.

The default template includes **ServiceStack.Server** NuGet packages which includes all of ServiceStack as well as 
Redis, OrmLite and other high-level functionality depending on OrmLite and Redis including 
[AutoQuery](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query), 
[Redis Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Redis-Server-Events), 
[Redis MQ](https://github.com/ServiceStack/ServiceStack/wiki/Messaging-and-Redis), 
etc. Despite its features, ServiceStack is super lean where the working Empty Template Console App project which includes 
jQuery, Bootstrap, React and its CSS, JS and Font resources compiles down into a single .NET .exe that only weighs
**1.5 MB** zipped or **4.7 MB** uncompressed. 

### [Modern Web Application Development](https://github.com/ServiceStackApps/ReactDesktopApps#react-web-development)

The React Desktop Template is structured for optimal developer productivity, fast iterations, maximum re-use, 
easy customizability and optimal runtime performance that's driven by a pre-configured automated workflow. 
It also maximizes skill re-use where most development time will be spent developing a normal ASP.NET 
React Web Application without any consideration for the different platforms the tooling creates. 

The template follows the same
[Modern React Apps with .NET](https://github.com/ServiceStackApps/Chat-React#modern-reactjs-apps-with-net)
as ServiceStack's other Single Page App templates which uses node's rich npm ecosystem to enable access to its premier
Web technologies including [bower](http://bower.io/) for client dependencies and pre-configured
[Grunt](http://gruntjs.com) and [Gulp](http://gulpjs.com) tasks to take care of website bundling, optimization,
application packaging and ASP.NET Website deployemnts.

The entire React application is hosted within a single static 
[default.html](https://github.com/ServiceStackApps/ReactDesktopApps/blob/master/src/DefaultApp/DefaultApp/DefaultApp/default.html)
which is itself only used to structure the websites resources into logical groups where 3rd Party 
JavaScript libraries and CSS are kept isolated from your own Application's source code. The groups are defined
by HTML comments which instruct 
[Gulps userref](https://www.npmjs.com/package/gulp-useref) plugin on how to minify and optimize your 
Apps resources.

### React Desktop App VS.NET Template

The **React Desktop Apps** template is pre-configured with the necessary tools to package your Web Application 
into multiple platforms using the provided Grunt build tasks. The Desktop Apps are also debuggable
allowing for a simplified and iterative dev workflow by running any of the Host Projects:

- **Web** - ASP.NET Web Application
- **Windows** - Native Windows application embedded in a CefSharp Chromium browser
- **OSX** - Native OS X Cocoa App embedded in a WebView control (requires Xamarin.Mac)
- **Console** - Single portable, cross platform executable that launches the user's prefered browser

## Project Structure

The resulting project structure is the same as the 
[React App](https://github.com/ServiceStackApps/Chat-React#modern-reactjs-apps-with-net) VS.NET Template, 
but with 3 additional projects for hosting the new Desktop and Console Apps and a Common **Resources** project
shared by Host projects containing all the ASP.NET resources (e.g. .css, .js, images, etc) as embedded
resources. 

It's kept in-sync with the primary **DefaultApp** project with the `01-bundle-all` (or `default`) Grunt task.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/react-desktop-apps/combined-project-structure.png)

### DefaultApp.sln 

- **DefaultApp** - Complete Web application, inc. all Web App's .js, .css, images, etc.
- **DefaultApp.AppConsole** - Console Host Project
- **DefaultApp.AppWinForms** - WinForms Host Project
- **DefaultApp.Resources** - Shared Embedded resources sourced from **DefaultApp** 
- **DefaultApp.ServiceInterface** - ServiceStack Service Implementations
- **DefaultApp.ServiceModel** - Request and Response DTO's
- **DefaultApp.Tests** - NUnit tests

### DefaultAppMac.sln    

 - **DefaultApp.AppMac** - OSX Cocoa Host project

This is a Xamarin Studio project which can be built with Xamarin.Mac and uses the compiled embedded resources
`lib\DefaultApp.Resources.dll` created by the **01-bundle-all** Grunt task.

### DefaultApp Project

The primary **DefaultApp** project contains the complete React Web App hosted in an ASP.NET Project. 
It includes `gruntfile.js` which provides the necessary Grunt tasks to bundle and optimize the Wep Application 
ready for deployment as well as Grunt tasks to minify the Web Applications assets and publishes them 
embedded resources into the shared **DefaultApp.Resources** project. This project is how the React WebApp
is made available to the alternative Desktop and Console Apps.

The primary Grunt Tasks you'll use to package and deploy your App are contained in **Alias Tasks** group
which is easily runnable from VS .NET's 
[Task Runner Explorer](https://visualstudiogallery.msdn.microsoft.com/8e1b4368-4afb-467a-bc13-9650572db708)
which is built into VS 2015:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/gap/react-desktop-tasks.png)

- **default** - Runs `01-bundle-all` and creates packages for `02-package-console` and `03-package-winforms`
- [**01-bundle-all**](#01-bundle-all) - optimizes and packages Web App the into `wwwroot` and `Resources` project
- [**02-package-console**](#02-package-console) - Packages the Console App in `wwwroot_build\apps`
- [**03-package-winforms**](#03-package-winforms) - Packages the Winforms App in `wwwroot_build\apps`
- [**04-deploy-webapp**](#04-deploy-webapp) - deploys the Web App in `wwwroot` with MS WebDeploy to any IIS Server using config `wwwroot_build\publish\config.json`

The template also includes the **ILMerge** tool to merge all .NET .dlls (inc. Resources.dll) into a single,
cross-platform Console Application .exe that's runnable as-is on any Windows, OSX or Linux server with .NET
or Mono pre-installed. 

### [Incredible Reuse and Highly Customizable](https://github.com/ServiceStackApps/ReactDesktopApps#host-projects) 

Customizations for each platform is available by modifying the individual `platform.css` and `project.js` files at the 
base of each Host folder for adding unique platform-specific JavaScript or CSS. 

In addition, an easy way to limit which HTML elements are displayed is to use the `platform` class to initially 
hide the element, then specify which platforms it should be displayed in by listing the specific platforms, e.g:

```html
<ul className="nav navbar-nav pull-right">
    <li><a onClick={this.handleAbout}>About</a></li>
    <li className="platform winforms">
        <a onClick={this.handleToggleWindow}>Toggle Window</a>
    </li>
    <li className="platform winforms mac">
        <a onClick={this.handleQuit}>Close</a>
    </li>
</ul>
```

In this example the **About** link is shown on every platform, **Toggle Window** is specific to Windows and **Close** is available 
in both Winforms or OSX Cocoa Desktop Applications.

Since each host is just a normal C# project you also have complete freedom in enhancing each platform with specific functionality 
native to that platform. E.g. you can add 3rd party dependencies or create Services that are only available to that platform.

### Downloads for the DefaultApp VS.NET Template

Windows Winforms App:

#### [DefaultApp-winforms.exe](https://github.com/ServiceStackApps/ReactDesktopApps/raw/master/dist/DefaultApp-winforms.exe) (23.7 MB)

OSX Cocoa App: 

#### [DefaultApp.AppMac.app.zip](https://github.com/ServiceStackApps/ReactDesktopApps/raw/master/dist/DefaultApp.AppMac.app.zip) (4.1 MB)

Console App (Windows/OSX/Linux):

#### [DefaultApp-console.exe](https://github.com/ServiceStackApps/ReactDesktopApps/raw/master/dist/DefaultApp-console.exe) (4.8 MB) or [DefaultApp-console.zip](https://github.com/ServiceStackApps/ReactDesktopApps/raw/master/dist/DefaultApp-console.zip) (1.5 MB)

## [React Chat Desktop App](https://github.com/ServiceStackApps/ReactChatApps)

To illustrate the potential of React Desktop Apps we've developed a couple of Basic Examples to show how quick and easy it is 
to create highly-interactive Desktop Applications for every major Operating System. 

![WinForms application with loading splash screen](https://github.com/ServiceStack/Assets/raw/master/img/livedemos/react-desktop-apps/redis-chat-app.gif)

React Chat shows the features and interactivity possible when you have all of ServiceStack available in a Desktop App. React Chat uses 
[Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events) for its real-time notifications allowing ServiceStack 
Services to notify the client of events instantly. In React Chat each command is sent by Ajax to a normal ServiceStack Service which
effectively just relays it back to the client via a Server Event. 

After the Server Event reaches the client it calls the registered JavaScript handler, which in the case of `/cmd.toggleFormBorder` calls 
[nativeHost.toggleFormBorder()](https://github.com/ServiceStackApps/ReactChatApps/blob/master/src/ReactChat/ReactChat/js/components/ChatApp.jsx#L65).

In Winforms, nativeHost is registered a C# object courtesy of 
[CefSharp's JavaScript Interop](https://github.com/ServiceStackApps/ReactDesktopApps#winforms-native-host) 
feature where JavaScript can call C# directly, which for ToggleFormBorder() just toggles the Window's Chrome on/off:

```csharp
ChromiumBrowser.RegisterJsObject("nativeHost", new NativeHost(this));

public class NativeHost
{
    //...
    public void ToggleFormBorder()
    {
        formMain.InvokeOnUiThreadIfRequired(() => {
            formMain.FormBorderStyle = formMain.FormBorderStyle == FormBorderStyle.None
                ? FormBorderStyle.Sizable
                : FormBorderStyle.None;
        });
    }
}
```

This is also an example of a Windows only feature that only appears when the React Web App hosted in Winforms. 

### Controlling multiple Windows with Server Events

A nice benefit for using Server Events for real-time communication with JavaScript is that you're able 
to control multiple window clients naturally just by having each Windows Application subscribe to the same 
remote `/event-stream` url. You can do in React Chat just by opening multiple windows as all subesquent 
Windows Apps opened listen to the self-hosting listener of the first one that was opened. 

The `/windows.dance` chat message provides a nice demonstration of this in action :)

#### [YouTube Live Demo](https://youtu.be/-9kVqdPbqOM)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/react-desktop-apps/dancing-windows.png)](https://youtu.be/-9kVqdPbqOM)

> In addition to the Default template, ReactChat also has Razor enabled to also generate dynamic server pages

You can play around with React Chat with the download for your Operating System below:

### Downloads for the React Chat

Windows Winforms App:

#### [ReactChat-winforms.exe](https://github.com/ServiceStackApps/ReactChatApps/raw/master/dist/ReactChat-winforms.exe) (23.6 MB)

OSX Cocoa App: 

#### [ReactChat.AppMac.mono.app.zip](https://github.com/ServiceStackApps/ReactChatApps/raw/master/dist/ReactChat.AppMac.mono.app.zip) (16.9 MB) or without Mono [ReactChat.AppMac.app.zip](https://github.com/ServiceStackApps/ReactChatApps/raw/master/dist/ReactChat.AppMac.app.zip) (4.51 MB)

Console App (Windows/OSX/Linux):

#### [ReactChat-console.exe](https://github.com/ServiceStackApps/ReactChatApps/raw/master/dist/ReactChat-console.exe) (5.33 MB) or [DefaultApp-console.zip](https://github.com/ServiceStackApps/ReactChatApps/raw/master/dist/ReactChatApps-console.zip) (1.93MB)

## [Introducing Redis React!](https://github.com/ServiceStackApps/RedisReact)

We're also excited to announce Redis React, which we believe is a good example showing an ideal use-case for React Desktop Apps:

Redis React is a simple user-friendly UI for browsing data in Redis servers that leverages the navigation and deep-linking 
benefits of a Web-based UI, the productivity and responsiveness of the [React framework](http://facebook.github.io/react/) 
and the deep Integration possible from a Native App.

## [Live Demo](http://redisreact.servicestack.net/#/)

The Redis React App has been packaged for multiple platforms inc. the ASP.NET Live Demo 
[redisreact.servicestack.net](http://redisreact.servicestack.net/#/) deployed on AWS which you can use to preview Redis React 
browsing a redis server populated with the 
[Northwind Dataset](http://northwind.servicestack.net/) persisted as JSON following the
[Complex Type Conventions](http://stackoverflow.com/a/8919931/85785) built into the 
[C# ServiceStack.Redis Client](https://github.com/ServiceStack/ServiceStack.Redis).

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/home.png)](http://redisreact.servicestack.net/#/)

## Download

Use Redis React to browse your internal Redis Server by downloading the appropriate download for your platform:

### Windows

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/download-windows10.png)](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact-winforms.exe)

To run on Windows, download the self-extracting Winforms App:

#### [RedisReact-winforms.exe](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact-winforms.exe) (23.9MB)

> Windows requires .NET 4.5 installed which is pre-installed on recent version of Windows

### OSX

To run on OSX, download the Cocoa OSX App:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/download-osx.png)](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact.AppMac.app.zip)

#### [RedisReact.AppMac.mono.app.zip](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact.AppMac.mono.app.zip) (16.5 MB) or without mono [RedisReact.AppMac.app.zip](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact.AppMac.app.zip) (4.1 MB)

> The Cocoa OSX App was built with [Xamarin.Mac](https://developer.xamarin.com/guides/mac/getting_started/hello,_mac/)
and includes an embedded version of Mono which doesn't require an existing install of Mono 

### Linux

To run on Linux, download the cross-platform Console App:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/download-linux.png)](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact-console.exe)

#### [RedisReact-console.exe](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact-console.exe) (5.4MB) or [RedisReact-console.exe.zip](https://github.com/ServiceStackApps/RedisReact/raw/master/dist/RedisReact-console.exe.zip) (1.7MB)

**RedisReact-console.exe** is a headless Console Application that can run on Windows, OSX and Linux 
platforms with .NET or Mono installed. 

See the instructions for [Installing Mono on Linux](http://www.mono-project.com/docs/getting-started/install/linux/).
If installing via apt-get, it needs the **mono-complete** package to run.

### Rich support for JSON

Redis React is especially useful for browsing JSON values which includes a 
[human friendly view of JSON data](#category-item) and the ability to view multiple related keys together 
in a [tabular data grid](#view-as-grid) enabling fast inspection of redis data. 

At anytime you can click on the JSON preview to reveal the raw JSON string, or use the Global `t` 
shortcut key to toggle between preview mode and raw mode of JSON data. 

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/order.png)](http://redisreact.servicestack.net/#/keys?id=urn%3Aorder%3A10860&type=string)

It also takes advantages of the POCO conventions built into the 
[C# ServiceStack.Redis Client](https://github.com/ServiceStack/ServiceStack.Redis) where it will 
automatically display any related entities for the current value, as seen with the related 
**Customer** the **Order** was for and the **Employee** who created it.

It works by scanning the JSON fields for names ending with **Id** then taking the prefix and using
it to predict the referenced key, e.g:

    CustomerId:FRANR => urn:customer:FRANR 

It then fetches all the values with the calculated key and displays them below the selected Order.
Clicking the **Customer** or **Employee** Key will navigate to that record, providing nice navigation
for quickly viewing a record and its related entities.

## [View as Grid](http://redisreact.servicestack.net/#/search?q=urn%3Acategory)

When keys share the same schema, clicking on the **view as grid** link lets you see multiple search results
displayed in a tabular data grid, e.g:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/category-grid.png)](http://redisreact.servicestack.net/#/search?q=urn%3Acategory)

## [Web Console](http://redisreact.servicestack.net/#/console)

The built-in Console takes advantage of a Web Based UI to provide some nice enhancements. E.g. each 
command is displayed on top of the result it returns, where clicking the command populates the text box
making it easy to execute or modify existing commands. Any **OK** Success responses are in green, whilst
any error responses are in red. Also just like JSON values above, it shows a human-friendly view for 
JSON data which can be clicked to toggle on/off individually: 

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/livedemos/redis-react/console.png)](http://redisreact.servicestack.net/#/console)

Redis React is packed with a number of other features, checkout the 
[project home page](https://github.com/ServiceStackApps/RedisReact) for an Overview and try it out today!

## [Swift 2.0 Support!](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference)

We're also happy to announce support for the new and much improved 
[Swift 2.0](https://developer.apple.com/swift/) that's now shipping in 
[Xcode 7](https://developer.apple.com/xcode/download/) which is now available as a free download for everyone to enjoy.

![Swift iOS, XCode and OSX Banner](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/swift-logo-banner.jpg)

We're happy to report that Swift has improved substantially and has also had a positive impact on our Swift Client library, 
e.g. Error Handling is more familiar thanks to 
[Swift 2.0 new Error Handling](https://developer.apple.com/library/prerelease/ios/documentation/Swift/Conceptual/Swift_Programming_Language/ErrorHandling.html)
utilizing a `do/try/catch` block where you can now access ServiceStack Service Exceptions inside a catch block, e.g:

```swift
let client = JsonServiceClient(baseUrl: "http://test.servicestack.net")

let request = ThrowValidation()
request.email = "invalidemail"

do {
    let response = try client.post(request)
} catch let responseError as NSError {
    
    let status:ResponseStatus = responseError.convertUserInfo()!
    status.errors.count //= 3
    
    status.errors[0].errorCode! //= InclusiveBetween
    status.errors[0].fieldName! //= Age
    status.errors[0].message!   //= 'Age' must be between 1 and 120. You entered 0.
}
```

The addition of the new `catch` keyword means the the previous Async promise error handling has now been renamed to `error`
which now looks like:

```swift
let request = ThrowValidation()
request.email = "invalidemail"

client.postAsync(request)
    .error { responseError in
        let status:ResponseStatus = responseError.convertUserInfo()!
        status.errors.count //= 3
        //...
    }

```

There's improved type inference where before you had to include the full type signature in the closure continuation:

```swift
client.getAsync(AppOverview())
    .then(body:{(r:AppOverviewResponse) -> Void in 
        r.topTechnologies.count //= 100
        ... 
    })
``` 

In Swift 2.0 this has now been reduced to the absolute minimum code required:

```swift
client.getAsync(AppOverview())
    .then { 
        $0.topTechnologies.count //= 100
        //... 
    }
```

If preferred you can continue marking it up with as much additional Type information or optional syntax as you'd like, e.g: 

```swift
client.getAsync(AppOverview())
    .then({ r in
        r.topTechnologies.count //= 100
        //... 
    })
```

### Download ServiceStack Xcode 7 Plugin 

[![ServiceStackXCode.dmg download](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-dmg.png)](https://github.com/ServiceStack/ServiceStack.Swift/raw/master/dist/ServiceStackXcode.dmg)

Once opened, the ServiceStack XCode Plugin can be installed by dragging it to the XCode Plugins directory:

![ServiceStackXCode.dmg Installer](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-installer.png)

### Swift 2.0 Changes

Swift 2.0 is a major upgrade that introduced a number of breaking changes which required updating both Server generated DTO's as 
well as `JsonServiceClient` client library. Whilst the POCO DTO definition remained exactly the same, i.e: 

```swift
public class AllCollectionTypes
{
    required public init(){}
    public var intArray:[Int] = []
    public var intList:[Int] = []
    public var stringArray:[String] = []
    public var stringList:[String] = []
    public var pocoArray:[Poco] = []
    public var pocoList:[Poco] = []
    public var pocoLookup:[String:[Poco]] = [:]
    public var pocoLookupMap:[String:[String:Poco]] = [:]
}
```

#### Old Type Extensions

In Swift 1.x the amount of boilerplate required for transparent JSON serialization without being able to use Swift's incomplete
reflection support required this amount of boilerplate repeated for every type:

```swift
extension AllCollectionTypes : JsonSerializable
{
    public class var typeName:String { return "AllCollectionTypes" }
    public class func reflect() -> Type<AllCollectionTypes> {
        return TypeConfig.config() ?? TypeConfig.configure(Type<AllCollectionTypes>(
            properties: [
                Type<AllCollectionTypes>.arrayProperty("intArray", get: { $0.intArray }, set: { $0.intArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("intList", get: { $0.intList }, set: { $0.intList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringArray", get: { $0.stringArray }, set: { $0.stringArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringList", get: { $0.stringList }, set: { $0.stringList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoArray", get: { $0.pocoArray }, set: { $0.pocoArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoList", get: { $0.pocoList }, set: { $0.pocoList = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookup", get: { $0.pocoLookup }, set: { $0.pocoLookup = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookupMap", get: { $0.pocoLookupMap }, set: { $0.pocoLookupMap = $1 }),
            ]))
    }
    public func toJson() -> String {
        return AllCollectionTypes.reflect().toJson(self)
    }
    public class func fromJson(json:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromJson(AllCollectionTypes(), json: json)
    }
    public class func fromObject(any:AnyObject) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromObject(AllCollectionTypes(), any:any)
    }
    public func toString() -> String {
        return AllCollectionTypes.reflect().toString(self)
    }
    public class func fromString(string:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromString(AllCollectionTypes(), string: string)
    }
}
```

#### New Type Extensions

This has now been greatly reduced in Swift 2.0 thanks to Protocol Extensions (aka traits) where it's down to just:

```swift
extension AllCollectionTypes : JsonSerializable
{
    public static var typeName:String { return "AllCollectionTypes" }
    public static var metadata = Metadata.create([
            Type<AllCollectionTypes>.arrayProperty("intArray", get: { $0.intArray }, set: { $0.intArray = $1 }),
            Type<AllCollectionTypes>.arrayProperty("intList", get: { $0.intList }, set: { $0.intList = $1 }),
            Type<AllCollectionTypes>.arrayProperty("stringArray", get: { $0.stringArray }, set: { $0.stringArray = $1 }),
            Type<AllCollectionTypes>.arrayProperty("stringList", get: { $0.stringList }, set: { $0.stringList = $1 }),
            Type<AllCollectionTypes>.arrayProperty("pocoArray", get: { $0.pocoArray }, set: { $0.pocoArray = $1 }),
            Type<AllCollectionTypes>.arrayProperty("pocoList", get: { $0.pocoList }, set: { $0.pocoList = $1 }),
            Type<AllCollectionTypes>.objectProperty("pocoLookup", get: { $0.pocoLookup }, set: { $0.pocoLookup = $1 }),
            Type<AllCollectionTypes>.objectProperty("pocoLookupMap", get: { $0.pocoLookupMap }, set: { $0.pocoLookupMap = $1 }),
        ])
}
```

This still doesn't make use any reflection so JSON serialization should continue to perform exceptionally well.

### New Service Client Features

During the upgrade we've also added a number of new features to the `JsonServiceClient` where it's public `ServiceClient`
protocol has been expanded to:

```swift
public protocol ServiceClient
{
    func get(request:T) throws -> T.Return
    func get(request:T) throws -> Void
    func get(request:T, query:[String:String]) throws -> T.Return
    func get(relativeUrl:String) throws -> T
    func getAsync(request:T) -> Promise<T.Return>
    func getAsync(request:T) -> Promise<Void>
    func getAsync(request:T, query:[String:String]) -> Promise<T.Return>
    func getAsync(relativeUrl:String) -> Promise<T>
    
    func post(request:T) throws -> T.Return
    func post(request:T) throws -> Void
    func post(relativeUrl:String, request:Request?) throws -> Response
    func postAsync(request:T) -> Promise<T.Return>
    func postAsync(request:T) -> Promise<Void>
    func postAsync(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func put(request:T) throws -> T.Return
    func put(request:T) throws -> Void
    func put(relativeUrl:String, request:Request?) throws -> Response
    func putAsync(request:T) -> Promise<T.Return>
    func putAsync(request:T) -> Promise<Void>
    func putAsync(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func delete(request:T) throws -> T.Return
    func delete(request:T) throws -> Void
    func delete(request:T, query:[String:String]) throws -> T.Return
    func delete(relativeUrl:String) throws -> T
    func deleteAsync(request:T) -> Promise<T.Return>
    func deleteAsync(request:T) -> Promise<Void>
    func deleteAsync(request:T, query:[String:String]) -> Promise<T.Return>
    func deleteAsync(relativeUrl:String) -> Promise<T>
    
    func patch(request:T) throws -> T.Return
    func patch(request:T) throws -> Void
    func patch(relativeUrl:String, request:Request?) throws -> Response
    func patchAsync(request:T) -> Promise<T.Return>
    func patchAsync(request:T) -> Promise<Void>
    func patchAsync(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func send(request:T) throws -> T.Return
    func send(request:T) throws -> Void
    func send(intoResponse:T, request:NSMutableURLRequest) throws -> T
    func sendAsync(intoResponse:T, request:NSMutableURLRequest) -> Promise<T>
    
    func getData(url:String) throws -> NSData
    func getDataAsync(url:String) -> Promise<NSData>
}
```

Where new support has been added for `IReturnVoid` and **PATCH** Requests. 

### Swift HTTP Marker Interfaces

The new `send*` API's take advantage of the HTTP Verb Interface Markers described below to send the Request DTO using the 
annotated HTTP Method, e.g:

```swift
public class HelloByGet : IReturn, IGet 
{
    public typealias Return = HelloResponse
    public var name:String?
}
public class HelloByPut : IReturn, IPut 
{
    public typealias Return = HelloResponse
    public var name:String?
}

let response = try client.send(HelloByGet())  //GET

client.sendAsync(HelloByPut())                //PUT
    .then { }
```

ServiceStack's Error Response Status DTO's are now included in the **JsonServiceClient.swift** source instead of being repeated 
within each ServiceStack Reference added to your project, mitigating any duplicate DTO conflicts.

## [Java ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/android-studio-splash.png)](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

The Java Android Studio and Eclipse Plugins have also been improved in this release, the new versions which can be downloaded below:

### [Download ServiceStack IDEA Android Studio Plugin](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio)

### [Download ServiceStack Eclipse Plugin](https://github.com/ServiceStack/ServiceStack.Java/tree/master/src/ServiceStackEclipse#eclipse-integration-with-servicestack) 

### Send Raw String or byte[] Requests

You can easily get the raw string Response from Request DTO's that return are annotated with `IReturn<string>`, e.g:
 
```java
public static class HelloString implements IReturn<String> { ... }

String response = client.get(new HelloString().setName("World"));
```

You can also specify that you want the raw UTF-8 `byte[]` or `String` response instead of a the deserialized Response DTO by specifying
the Response class you want returned, e.g:

```java
byte[] response = client.get("/hello?Name=World", byte[].class);
```

### Java HTTP Marker Interfaces

Like the .NET and Swift Service Clients, the HTTP Interface markers are also annotated on Java DTO's and let you use the same
`send` API to send Requests via different HTTP Verbs, e.g:  

```java
public static class HelloByGet implements IReturn<HelloResponse>, IGet { ... }
public static class HelloByPut implements IReturn<HelloResponse>, IPut { ... }

HelloResponse response = client.send(new HelloByGet().setName("World")); //GET

client.sendAsync(new HelloByPut().setName("World"),                         //PUT
    new AsyncResult<HelloResponse>() {
        @Override
        public void success(HelloResponse response) { }
    });
```

### IReturnVoid Support

New Sync/Async overloads have been added for `IReturnVoid` Request DTO's:

```java
client.delete(new DeleteCustomer().setId(1));
```

### Java Annotations 

The built-in Java Annotations now have their metadata available at runtime as they're now annotated with:

```java 
@Retention(RetentionPolicy.RUNTIME)
public @interface Api { ... }
```


## [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

### Export Types

By default ServiceStack's Native Type Feature doesn't emit any System types built into the .NET Framework, these can now be 
emitted for non-.NET Languages with the new `ExportTypes` list, e.g. if your DTO's exposes the `DayOfWeek` System Enum it can
be exported by adding it to the pre-registered NativeTypesFeature's Plugin with:

```csharp
 var nativeTypes = this.GetPlugin<NativeTypesFeature>();
nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
```

Now if any of your DTO's has a `DayOfWeek` property it will emitted in the generated DTO's, Java example:

```java
public static enum DayOfWeek
{
    Sunday,
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday;
}
```

## Service Clients

### HTTP Verb Interface Markers

You can decorate your Request DTO's using the `IGet`, `IPost`, `IPut`, `IDelete` and `IPatch` interface markers and the `Send` and 
`SendAsync` API's will use it to automatically send the Request using the selected HTTP Method. E.g:

```csharp
public class HelloByGet : IReturn<HelloResponse>, IGet 
{
    public string Name { get; set; }
}
public class HelloByPut : IReturn<HelloResponse>, IPut 
{
    public string Name { get; set; }
}

var response = client.Send(new HelloByGet { Name = "World" }); //GET

await client.SendAsync(new HelloByPut { Name = "World" }); //PUT
```

This was feature was previously only implemented in 
[StripeGateway](https://github.com/ServiceStack/Stripe), but is now available in all 
[.NET Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) as well as the latest 
[Java JsonServiceClient](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference) and
[Swift JsonServiceClient](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference). 

Whilst a simple feature, it enables treating your remote services as a message-based API 
[yielding its many inherent advantages](https://github.com/ServiceStack/ServiceStack/wiki/Advantages-of-message-based-web-services#advantages-of-message-based-designs) 
where your Application API's need only pass Request DTO models around to be able to invoke remote Services, decoupling the 
Service Request from its implementation which can be now easily managed by a high-level adapter that takes care of proxying the 
Request to the underlying Service Client. The adapter could also add high-level functionality of it's own including auto retrying of 
failed requests, generic error handling, logging/telemetrics, event notification, throttling, offline queuing/syncing, etc.

## [Async StripeGatway](https://github.com/ServiceStack/Stripe)

Perhaps a clearer indication of the simplicity and generic functionality possible from a message-based API's is how it's possible 
to add [Async support to **all** Stripe Gateway API's](https://github.com/ServiceStack/Stripe/issues/20) in **<1 hour** from initial
feature request to [implementation](https://github.com/ServiceStack/Stripe/commit/aa4023ef4d0cbe74187b72af567038f688fd9920) 
and published to our 
[MyGet pre-release NuGet packages](https://github.com/ServiceStack/ServiceStack/wiki/MyGet) where it's available for immediate use:

```csharp
var charge = await gateway.PostAsync(new ChargeStripeCustomer {
    Customer = customer.Id,
    Amount = 100,
    Currency = "usd",
    Description = "Test Charge Customer",
});
```

The Stripe Gateway provides a typed .NET message-based API to 
[Stripe's REST Services](https://stripe.com/docs/api#intro) which as it's inspired by Ruby conventions, uses a `snake_case` naming 
convention so it's a good example of viewing the benefits of a message-based API's in isolation, i.e. independent from the features 
ServiceStack adds around it's own .NET Services.

Unlike other Stripe Client implementations the [StripeGateway.cs](https://github.com/ServiceStack/Stripe/blob/master/src/Stripe/StripeGateway.cs) is

 - **Small** - Fits in a single class where the majority of the code-base contains Stripe's Typed DTO's and Currency Info
 - **Simple** - Its tiny code-base has great re-use, requiring less effort to create, maintain, extend and test 
 - **Highly Testable** - Its small surface area implements the typed, mockable 
 [IRestGateway](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/IRestGateway.cs)
 - **Open Ended** - Users can use their own Request DTO's to call new Stripe Services that StripeGateway has no knowledge about 

### Declarative Message-based APIs

The only custom code required to implement the `ChargeStripeCustomer` above is this single, clean, declarative Request DTO:

```csharp
[Route("/charges")]
public class ChargeStripeCustomer : IPost, IReturn<StripeCharge>
{
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string Customer { get; set; }
    public string Card { get; set; }
    public string Description { get; set; }
    public bool? Capture { get; set; }
    public int? ApplicationFee { get; set; }
}
```  

Which contains all the information needed to call the Stripe Service including the `/charges` relative url, using the **POST** 
HTTP method and the typed `StripeCharge` DTO it returns. To charge a Customer the Request DTO can either use the explicit
`Post/PostAsync` or universal `Send/SendAsync` StripeGateway methods. 

We hope this example makes it clearer why we continue to encourage others to adopt a message-based API for accessing any remote 
Services and how we're able to continue to deliver rich generic functionality and value to all ServiceStack Services with 
minimal code, effort, friction and complexity as a result.

## Debuggable Razor Views

Razor Views are now debuggable for 
[Debug builds](https://github.com/ServiceStack/ServiceStack/wiki/Debugging#debugmode) by default, it can also be explicitly specified on:

```csharp
Plugins.Add(new RazorFormat {
    IncludeDebugInformation = true,
    CompileFilter = compileParams => ...
})
```

> The new `CompileFilter` is an advanced option that lets modify the `CompilerParameters` used by the C# CodeDom provider to compile
the Razor Views if needed. 


## [ss-utils.js](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library)

### $.fn.ajaxSubmit

ajaxSubmit has been decoupled from ServiceStack's 
[auto bindForm ajax and validation helper](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library#binding-html-forms)
so it can be used independently to submit a form on demand. This is used in the 
[connections.jsx](https://github.com/ServiceStackApps/RedisReact/blob/15dfc5cfea49d0502ec8c090b5b180d29051ea3a/src/RedisReact/RedisReact/js/components/connections.jsx#L31)
React Component of [Redis React's Connections Page](https://github.com/ServiceStackApps/RedisReact#connections)
to auto submit the form via ajax to the specified `/connection` url with the populated Form INPUT values. It also disables the 
`#btnConnect` submit button and adds a `.loading` class to the form whilst it's in transit which is used to temporarily show the 
loading sprite:

```js
var Connections = React.createClass({
    //...
    onSubmit: function (e) {
        e.preventDefault();

        var $this = this;
        $(e.target).ajaxSubmit({
            onSubmitDisable: $("#btnConnect"),
            success: function () {
                $this.setState({ successMessage: "Connection was changed" });
                Actions.loadConnection();
            }
        });
    },
    render: function () {
        var conn = this.state.connection;
        return (
          <div id="connections-page">
            <div className="content">
                <form id="formConnection" className="form-inline" onSubmit={this.onSubmit} action="/connection">
                    <h2>Redis Connection</h2>
                    <div className="form-group">
                        <input name="host" type="text" />
                        <input name="port" type="text" className="form-control" />
                        <input name="db" type="text" className="form-control" />
                    </div>
                    <p className="actions">
                        <img className="loader" src="/img/ajax-loader.gif" />
                        <button id="btnConnect" className="btn btn-default btn-primary">Change Connection</button>
                    </p>
                    <p className="bg-success">{this.state.successMessage}</p>
                    <p className="bg-danger error-summary"></p>
                </form>
            </div>
          </div>
        );
    }
}; 
``` 

### $.ss.parseResponseStatus

Lets you easily parse the raw text of a Ajax Error Response into a responseStatus JavaScript object, example used in Redis React's 
[Console Component](https://github.com/ServiceStackApps/RedisReact/blob/15dfc5cfea49d0502ec8c090b5b180d29051ea3a/src/RedisReact/RedisReact/js/components/console.jsx#L103): 

```js
.fail(function (jq, jqStatus, statusDesc) {
    var status = $.ss.parseResponseStatus(jq.responseText, statusDesc);
    Actions.logEntry({
        cmd: cmd,
        result: status.message,
        stackTrace: status.stackTrace,
        type: 'err',
    });
});
```

### $.ss.bindAll

The new `bindAll` API is a simple helper for creating lightweight JavaScript objects by binding `this` for all functions of an 
object literal to the object instance, e.g:

```js
var Greeting = $.ss.bindAll({
    name: "World",
    sayHello: function() {
        alert("Hello, " + this.name);
    }
});

var fn = Greeting.sayHello;
fn(); // Hello, World
```

## Redis

### Improved LUA support

The new `ExecLua` API lets you execute LUA Script on a Redis server and returns any result in a generic `RedisText` Type which can 
be easily be inspected to access the LUA Script's complex type response. A good example of when to use a server-side LUA script 
is to reduce the network latency of chatty multi-request API's. 

A prime example of this is when using Redis's [SCAN](http://redis.io/commands/scan) API which provides a streaming, non-blocking 
API to search through the entire Redis KeySet. The number of API calls that's required is bounded to the size of the Redis
KeySet which could quickly result in a large number of Redis Operations yielding an unacceptable delay due to the latency of 
multiple dependent remote network calls. 

An easy solution is to instead have the multiple SCAN calls performed in-process on the Redis Server, eliminating the 
network latency of multiple SCAN calls, e.g:

```csharp
const string FastScanScript = @"
local limit = tonumber(ARGV[2])
local pattern = ARGV[1]
local cursor = 0
local len = 0
local results = {}
repeat
    local r = redis.call('scan', cursor, 'MATCH', pattern, 'COUNT', limit)
    cursor = tonumber(r[1])
    for k,v in ipairs(r[2]) do
        table.insert(results, v)
        len = len + 1
        if len == limit then break end
    end
until cursor == 0 or len == limit
return results";

RedisText r = redis.ExecLua(FastScanScript, "key:*", "10");
r.Children.Count.Print() //= 10
```

The `ExecLua` API returns this complex LUA table response in the `Children` collection of the `RedisText` Response. 

This above API is equivalent to C# API below which returns the first 10 results matching the `key:*` pattern:

```csharp
var keys = Redis.ScanAllKeys(pattern: "key:*", pageSize: 10)
    .Take(10).ToList();
```

However the C# Streaming API above would require an unknown number of Redis Operations to complete the request whereas 
only 1 call to Redis is required for the LUA Script. The number of SCAN calls can be reduced by choosing a higher pageSize 
to tell Redis to scan more keys.

### ExecCachedLua

ExecCachedLua is a convenient high-level API that eliminates the bookkeeping required for executing high-performance server LUA
Scripts which suffers from many of the problems that RDBMS stored procedures have which depends on pre-existing state in the RDBMS
that needs to be updated with the latest version of the Stored Procedure. 

With Redis LUA you either have the option to send, parse, load then execute the entire LUA script each time it's called or 
alternatively you could pre-load the LUA Script into Redis once on StartUp and then execute it using the Script's SHA1 hash. 
The issue with this is that if the Redis server is accidentally flushed you're left with a broken application relying on a 
pre-existing script that's no longer there. The new `ExecCachedLua` API provides the best of both worlds where it will always 
execute the compiled SHA1 script, saving bandwidth and CPU but will also re-create the LUA Script if it no longer exists.

You can now execute the compiled LUA script above by its SHA1 identifier, which continues to work regardless if it never existed 
or was removed at runtime, e.g:

```csharp
// #1: Loads LUA script and caches SHA1 hash in Redis Client
r = redis.ExecCachedLua(FastScanScript, sha1 =>
    redis.ExecLuaSha(sha1, "key:*", "10"));

// #2: Executes using cached SHA1 hash
r = redis.ExecCachedLua(FastScanScript, sha1 =>
    redis.ExecLuaSha(sha1, "key:*", "10"));

// Deletes all existing compiled LUA scripts 
redis.ScriptFlush();

// #3: Executes using cached SHA1 hash, gets NOSCRIPT Error, re-creates and re-executes with SHA1 hash
r = redis.ExecCachedLua(FastScanScript, sha1 =>
    redis.ExecLuaSha(sha1, "key:*", "10"));
```

### New Redis Client APIs

All new APIs added in this release:

#### IRedisClient
 - Type - returns key Type as string that can be used in pipelines
 - GetStringCount - returns STRLEN in a consistent API to fetch size of value, i.e `Get{DataType}Count()`
 - SetValues - Set multiple String values, alias for SetAll
 - ExecLua - Execute Lua Script that returns a generic `RedisText` result
 - ExecLuaSha - Execute Lua Script by SHA1 which returns a generic `RedisText` result
 - ExecCachedLua - Execute Lua Script by SHA1, re-creating it if it no longer exists 

#### IRedisNativeClient
 - Type - returns key Type as string that can be used in pipelines
 - EvalCommand - Execute Lua Script that returns a generic `RedisData` byte[] result
 - EvalShaCommand - Execute Lua Script by SHA1 which returns a generic `RedisData` byte[] result
 
## [Caching](https://github.com/ServiceStack/ServiceStack/wiki/Caching)

The [ICacheClientExtended](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Caching/ICacheClientExtended.cs)
API is used to to provide additional non-core functionality to our most popular 
[Caching providers](https://github.com/ServiceStack/ServiceStack/wiki/Caching):

 - Redis
 - OrmLite RDBMS
 - In Memory
 - AWS
 
The new API's are added as Extension methods on `ICacheClient` so they're easily accessible without casting, the new API's available include: 
  
  - GetKeysByPattern(pattern) - return keys matching a wildcard pattern
  - GetAllKeys() - return all keys in the caching provider
  - GetKeysStartingWith() - Streaming API to return all keys Starting with a prefix

With these new API's you can now easily get all active User Sessions using any of the supported Caching providers above with:

```csharp
var sessionPattern = IdUtils.CreateUrn<IAuthSession>(""); //= urn:iauthsession:
var sessionKeys = Cache.GetKeysStartingWith(sessionPattern).ToList();

var allSessions = Cache.GetAll<IAuthSession>(sessionKeys);
```

### Minor Features

 - All 3rd Party NuGet dependencies updated to latest stable version
 - C# `FormatException` return 400 by default 
 - New `AuthServerFilter` and `AuthClientFilter` available to all DotNetOpenAuth OAuth2 Providers
 - [Patch added on JavaScript JsonServiceClient](https://github.com/ServiceStack/ServiceStack/commit/5c9199ff6f66b86208efc20215d22c7b304048de)
by [@DeonHeyns](https://github.com/DeonHeyns)  
 - Added `UnsafeOrderBy()` to OrmLite SqlExpression to allow unchecked ordering by results by Custom SQL


# v4.0.44 Release Notes

## Highly Available Redis

[Redis Sentinel](http://redis.io/topics/sentinel) is the official recommendation for running a highly 
available Redis configuration by running a number of additional redis sentinel processes to actively monitor 
existing redis master and slave instances ensuring they're each working as expected. If by consensus it's
determined that the master is no longer available it will automatically failover and promote one of the 
replicated slaves as the new master. The sentinels also maintain an authoritative list of available 
redis instances providing clients a centeral repositorty to discover available instances they can connect to.

Support for Redis Sentinel has been added with the `RedisSentinel` class which listens to the available 
Sentinels to source its list of available master, slave and other sentinel redis instances which it uses
to configure and maintain the Redis Client Managers, initiating any failovers as they're reported.

### RedisSentinel Usage

To use the new Sentinel support, instead of populating the Redis Client Managers with the connection string 
of the master and slave instances you would create a single `RedisSentinel` instance configured with 
the connection string of the running Redis Sentinels:

```csharp
var sentinelHosts = new[]{ "sentinel1", "sentinel2:6390", "sentinel3" };
var sentinel = new RedisSentinel(sentinelHosts, masterName: "mymaster");
```

This shows a typical example of configuring a `RedisSentinel` which references 3 sentinel hosts (i.e. 
the minimum number for a highly available setup which can survive any node failing). 
It's also configured to look at the `mymaster` configuration set (the default master group). 

> Redis Sentinels can monitor more than 1 master / slave group, each with a different master group name. 

The default port for sentinels is **26379** (when unspecified) and as RedisSentinel can auto-discover 
other sentinels, the minimum configuration required is just: 

```csharp
var sentinel = new RedisSentinel("sentinel1");
```

> Scanning and auto discovering of other Sentinels can be disabled with `ScanForOtherSentinels=false`

### Start monitoring Sentinels

Once configured, you can start monitoring the Redis Sentinel servers and access the pre-configured
client manager with:

```csharp
IRedisClientsManager redisManager = sentinel.Start();
```

Which as before, can be registered in your preferred IOC as a **singleton** instance:

```csharp
container.Register<IRedisClientsManager>(c => sentinel.Start());
```

### Advanced Sentinel Configuration

RedisSentinel by default manages a configured `PooledRedisClientManager` instance which resolves both master 
Redis clients for read/write `GetClient()` and slaves for readonly `GetReadOnlyClient()` API's. 

This can be changed to use the newer `RedisManagerPool` with:

```csharp
sentinel.RedisManagerFactory = (master,slaves) => new RedisManagerPool(master);
```

### Custom Redis Connection String

The host the RedisSentinel is configured with only applies to that Sentinel Host, you can still use the flexibility of 
[Redis Connection Strings](https://github.com/ServiceStack/ServiceStack.Redis#redis-connection-strings)
to configure the individual Redis Clients by specifying a custom `HostFilter`:

```csharp
sentinel.HostFilter = host => "{0}?db=1&RetryTimeout=5000".Fmt(host);
```

This will return clients configured to use Database 1 and a Retry Timeout of 5 seconds (used in new 
Auto Retry feature).

### Other RedisSentinel Configuration

Whilst the above covers the popular Sentinel configuration that would typically be used, nearly every aspect
of `RedisSentinel` behavior is customizable with the configuration below:

<table>
    <tr>
        <td><b>OnSentinelMessageReceived</b></td><td>Fired when the Sentinel worker receives a message from the Sentinel Subscription</td>
    </tr>
    <tr>
        <td><b>OnFailover</b></td><td>Fired when Sentinel fails over the Redis Client Manager to a new master</td>
    </tr>
    <tr>
        <td><b>OnWorkerError</b></td><td>Fired when the Redis Sentinel Worker connection fails</td>
    </tr>
    <tr>
        <td><b>IpAddressMap</b></td><td>Map internal redis host IP's returned by Sentinels to its external IP</td>
    </tr>
    <tr>
        <td><b>ScanForOtherSentinels</b></td><td>Whether to routinely scan for other sentinel hosts (default true)</td>
    </tr>
    <tr>
        <td><b>RefreshSentinelHostsAfter</b></td><td>What interval to scan for other sentinel hosts (default 10 mins)</td>
    </tr>
    <tr>
        <td><b>WaitBetweenFailedHosts</b></td><td>How long to wait after failing before connecting to next redis instance (default 250ms)</td>
    </tr>
    <tr>
        <td><b>MaxWaitBetweenFailedHosts</b></td><td>How long to retry connecting to hosts before throwing (default 60s)</td>
    </tr>
    <tr>
        <td><b>WaitBeforeForcingMasterFailover</b></td><td>How long after consecutive failed attempts to force failover (default 60s)</td>
    </tr>
    <tr>
        <td><b>ResetWhenSubjectivelyDown</b></td><td>Reset clients when Sentinel reports redis is subjectively down (default true)</td>
    </tr>
    <tr>
        <td><b>ResetWhenObjectivelyDown</b></td><td>Reset clients when Sentinel reports redis is objectively down (default true)</td>
    </tr>
    <tr>
        <td><b>SentinelWorkerConnectTimeoutMs</b></td><td>The Max Connection time for Sentinel Worker (default 100ms)</td>
    </tr>
    <tr>
        <td><b>SentinelWorkerSendTimeoutMs</b></td><td>Max TCP Socket Send time for Sentinel Worker (default 100ms)</td>
    </tr>
    <tr>
        <td><b>SentinelWorkerReceiveTimeoutMs</b></td><td>Max TCP Socket Receive time for Sentinel Worker (default 100ms)</td>
    </tr>
</table>

## [Configure Redis Sentinel Servers](https://github.com/ServiceStack/redis-config)

[![Instant Redis Setup](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/redis/instant-sentinel-setup.png)](https://github.com/ServiceStack/redis-config)

We've also created the 
[redis config project](https://github.com/ServiceStack/redis-config) 
to simplify setting up and running a highly-available multi-node Redis Sentinel configuration including 
start/stop scripts for instantly setting up the minimal 
[highly available Redis Sentinel configuration](https://github.com/ServiceStack/redis-config/blob/master/README.md#3x-sentinels-monitoring-1x-master-and-2x-slaves)
on a single (or multiple) Windows, OSX or Linux servers. This single-server/multi-process configuration 
is ideal for setting up a working sentinel configuration on a single dev workstation or remote server.

The redis-config repository also includes the 
[MS OpenTech Windows redis binaries](https://github.com/ServiceStack/redis-windows#running-microsofts-native-port-of-redis)
and doesn't require any software installation. 

### Windows Usage 

To run the included Sentinel configuration, clone the redis-config repo on the server you want to run it on:

    git clone https://github.com/ServiceStack/redis-config.git

Then Start 1x Master, 2x Slaves and 3x Sentinel redis-servers with:

    cd redis-config\sentinel3\windows
    start-all.cmd

Shutdown started instances:

    stop-all.cmd

If you're running the redis processes locally on your dev workstation the minimal configuration to connect 
to the running instances is just:

```csharp
var sentinel = new RedisSentinel("127.0.0.1:26380");
container.Register(c => sentinel.Start());
```

### Localhost vs Network IP's

The sentinel configuration assumes all redis instances are running locally on **127.0.0.1**. 
If you're instead running it on a remote server that you want all developers in your network to be 
able to access, you'll need to either change the IP Address in the `*.conf` files to use the servers 
Network IP. Otherwise you can leave the defaults and use the `RedisSentinel` IP Address Map feature
to transparently map localhost IP's to the Network IP that each pc on your network can connect to.

E.g. if this is running on a remote server with a **10.0.0.9** Network IP, it can be configured with:

```csharp
var sentinel = new RedisSentinel("10.0.0.9:26380") {
    IpAddressMap = {
        {"127.0.0.1", "10.0.0.9"},
    }
};
container.Register(c => sentinel.Start());
```

### Google Cloud - [Click to Deploy Redis](https://github.com/ServiceStack/redis-config/blob/master/README.md#google-cloud---click-to-deploy-redis)

The easiest Cloud Service we've found that can instantly set up a multi node-Redis Sentinel Configuration 
is using Google Cloud's 
[click to deploy Redis feature](https://cloud.google.com/solutions/redis/click-to-deploy) 
available from the Google Cloud Console under **Deploy & Manage**:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/redis/sentinel3-gcloud-01.png)

Clicking **Deploy** button will let you configure the type, size and location where you want to deploy the 
Redis VM's. See the 
[full Click to Deploy Redis guide](https://github.com/ServiceStack/redis-config/blob/master/README.md#google-cloud---click-to-deploy-redis)
for a walk-through on setting up and inspecting a highly-available redis configuration on Google Cloud.

## Automatic Retries

Another feature we've added to improve reliability is Auto Retry where the RedisClient will transparently 
retry failed Redis operations due to Socket and I/O Exceptions in an exponential backoff starting from 
**10ms** up until the `RetryTimeout` of **3000ms**. These defaults can be tweaked with:

```csharp
RedisConfig.DefaultRetryTimeout = 3000;
RedisConfig.BackOffMultiplier = 10;
```

The `RetryTimeout` can also be configured on the connection string with `?RetryTimeout=3000`.

## RedisConfig

The `RedisConfig` static class has been expanded to provide an alternative to Redis Connection Strings to 
configure the default RedisClient settings. Each config option is 
[documented on the RedisConfig class](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/src/ServiceStack.Redis/RedisConfig.cs)
with the defaults shown below:

```csharp
class RedisConfig
{
    DefaultConnectTimeout = -1
    DefaultSendTimeout = -1
    DefaultReceiveTimeout = -1
    DefaultRetryTimeout = 3 * 1000
    DefaultIdleTimeOutSecs = 240
    BackOffMultiplier = 10
    BufferLength = 1450
    BufferPoolMaxSize = 500000
    VerifyMasterConnections = true
    HostLookupTimeoutMs = 200
    AssumeServerVersion = null
    DeactivatedClientsExpiry = TimeSpan.FromMinutes(1)
    DisableVerboseLogging = false
}
```

One option you may want to set is `AssumeServerVersion` with the version of Redis Server version you're running, e.g:

```csharp
RedisConfig.AssumeServerVersion = 2812; //2.8.12
RedisConfig.AssumeServerVersion = 3030; //3.0.3
```

This is used to change the behavior of a few API's to use the most optimal Redis Operation for their server 
version. Setting this will **save an additional INFO lookup** each time a new RedisClient Connection is opened.

## RedisStats

The new 
[RedisStats](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/src/ServiceStack.Redis/RedisStats.cs) 
class provides better visibility and introspection into your running instances:

<table>
    <tr>
        <td><b>TotalCommandsSent</b></td> <td>Total number of commands sent</td>
    </tr>
    <tr>
        <td><b>TotalFailovers</b></td> <td>Number of times the Redis Client Managers have FailoverTo() either by sentinel or manually</td>
    </tr>
    <tr>
        <td><b>TotalDeactivatedClients</b></td> <td>Number of times a Client was deactivated from the pool, either by FailoverTo() or exceptions on client</td>
    </tr>
    <tr>
        <td><b>TotalFailedSentinelWorkers</b></td> <td>Number of times connecting to a Sentinel has failed</td>
    </tr>
    <tr>
        <td><b>TotalForcedMasterFailovers</b></td> <td>Number of times we've forced Sentinel to failover to another master due to consecutive errors</td>
    </tr>
    <tr>
        <td><b>TotalInvalidMasters</b></td> <td>Number of times a connecting to a reported Master wasn't actually a Master</td>
    </tr>
    <tr>
        <td><b>TotalNoMastersFound</b></td> <td>Number of times no Masters could be found in any of the configured hosts</td>
    </tr>
    <tr>
        <td><b>TotalClientsCreated</b></td> <td>Number of Redis Client instances created with RedisConfig.ClientFactory</td>
    </tr>
    <tr>
        <td><b>TotalClientsCreatedOutsidePool</b></td> <td>Number of times a Redis Client was created outside of pool, either due to overflow or reserved slot was overridden</td>
    </tr>
    <tr>
        <td><b>TotalSubjectiveServersDown</b></td> <td>Number of times Redis Sentinel reported a Subjective Down (sdown)</td>
    </tr>
    <tr>
        <td><b>TotalObjectiveServersDown</b></td> <td>Number of times Redis Sentinel reported an Objective Down (odown)</td>
    </tr>
    <tr>
        <td><b>TotalRetryCount</b></td> <td>Number of times a Redis Request was retried due to Socket or Retryable exception</td>
    </tr>
    <tr>
        <td><b>TotalRetrySuccess</b></td> <td>Number of times a Request succeeded after it was retried</td>
    </tr>
    <tr>
        <td><b>TotalRetryTimedout</b></td> <td>Number of times a Retry Request failed after exceeding RetryTimeout</td>
    </tr>
    <tr>
        <td><b>TotalPendingDeactivatedClients</b></td> <td>Total number of deactivated clients that are pending being disposed</td>
    </tr>
</table>

You can get and print a dump of all the stats at anytime with:

```csharp
RedisStats.ToDictionary().PrintDump();
```

And Reset all Stats back to `0` with `RedisStats.Reset()`.

### Injectable Resolver Strategy 

To support the different host resolution behavior required for Redis Sentinel, we've decoupled the Redis 
Host Resolution behavior into an injectable strategy which can be overridden by implementing 
[IRedisResolver](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/src/ServiceStack.Redis/IRedisResolver.cs)
and injected into any of the Redis Client Managers with:

```csharp
redisManager.RedisResolver = new CustomHostResolver();
```

Whilst this an advanced customization option not expected to be used, it does allow using a custom strategy to 
change which Redis hosts to connect to. See the
[RedisResolverTests](https://github.com/ServiceStack/ServiceStack.Redis/blob/master/tests/ServiceStack.Redis.Tests.Sentinel/RedisResolverTests.cs)
for more info.

### New APIs

  - PopItemsFromSet()
  - DebugSleep()
  - GetServerRole()

The `RedisPubSubServer` is now able to listen to a **pattern** of multiple channels with:

```csharp
var redisPubSub = new RedisPubSubServer(redisManager) {
    ChannelsMatching = new[] { "events.in.*", "events.out." }
};
redisPubSub.Start();
```

### Deprecated APIs

 - The `SetEntry*` API's have been deprecated in favor of the more appropriately named `SetValue*` API's

## OrmLite Converters!

OrmLite has become a lot more customizable and extensible thanks to the internal redesign decoupling all 
custom logic for handling different Field Types into individual Type Converters. 
This redesign makes it possible to enhance or entirely replace how .NET Types are handled. OrmLite can now 
be extended to support new Types it has no knowledge about, a feature taken advantage of by the new support 
for SQL Server's `SqlGeography`, `SqlGeometry` and `SqlHierarchyId` Types!

Despite the scope of this internal refactor, OrmLite's existing test suite (and a number of new tests) continue 
to pass for each supported RDBMS. Whilst the **Firebird** and **VistaDB** providers having been greatly improved 
and now also pass the existing test suite (RowVersion's the only feature not implemented in VistaDB due 
to its lack of triggers).

![OrmLite Converters](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/ormlite-converters.png)

### Improved encapsulation, reuse, customization and debugging

Converters allows for greater re-use where the common functionality to support each type is maintained in the common
[ServiceStack.OrmLite/Converters](https://github.com/ServiceStack/ServiceStack.OrmLite/tree/master/src/ServiceStack.OrmLite/Converters)
whilst any RDBMS-specific functionality can inherit the common converters and provide any specialization 
required to support that type. E.g. SQL Server specific converters are maintained in 
[ServiceStack.OrmLite.SqlServer/Converters](https://github.com/ServiceStack/ServiceStack.OrmLite/tree/master/src/ServiceStack.OrmLite.SqlServer/Converters)
with each converter inheriting shared functionality and only adding custom logic required to support that 
Type in Sql Server. 

### Creating Converters

They also provide better encapsulation since everything relating to handling the field type is contained within 
a single class definition. A Converter is any class implementing
[IOrmLiteConverter](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/IOrmLiteConverter.cs)
although it's instead recommended to inherit from the `OrmLiteConverter` abstract class which allows
only the minimum API's needing to be overridden, namely the `ColumnDefinition` 
used when creating the Table definition and the ADO.NET `DbType` it should use in parameterized queries. 
An example of this is in 
[GuidConverter](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/Converters/GuidConverter.cs):

```csharp
public class GuidConverter : OrmLiteConverter
{
    public override string ColumnDefinition
    {
        get { return "GUID"; }
    }

    public override DbType DbType
    {
        get { return DbType.Guid; }
    }
}
```

But for this to work in SQL Server the `ColumnDefinition` should instead be **UniqueIdentifier** which is also
what it needs to be cast to, to be able to query Guid's in an SQL Statement. 
Therefore it requires a custom 
[SqlServerGuidConverter](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite.SqlServer/Converters/SqlServerGuidConverter.cs)
to support Guids in SQL Server:

```csharp
public class SqlServerGuidConverter : GuidConverter
{
    public override string ColumnDefinition
    {
        get { return "UniqueIdentifier"; }
    }

    public override string ToQuotedString(Type fieldType, object value)
    {
        var guidValue = (Guid)value;
        return string.Format("CAST('{0}' AS UNIQUEIDENTIFIER)", guidValue);
    }
}
```

### Registering Converters

To get OrmLite to use this new Custom Converter for SQL Server, the `SqlServerOrmLiteDialectProvider` just
[registers it in its constructor](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/41226795fc12f1b65d6af70c177a5ff57fa70334/src/ServiceStack.OrmLite.SqlServer/SqlServerOrmLiteDialectProvider.cs#L41):

```csharp
base.RegisterConverter<Guid>(new SqlServerGuidConverter());
```

Overriding the pre-registered `GuidConverter` to enable its extended functionality in SQL Server.

You'll also use the `RegisterConverter<T>()` API to register your own Custom GuidCoverter on the RDBMS 
provider you want it to apply to, e.g for SQL Server:

```csharp
SqlServerDialect.Provider.RegisterConverter<Guid>(new MyCustomGuidConverter());
```

### Resolving Converters

If needed, it can be later retrieved with:

```csharp
IOrmLiteConverter converter = SqlServerDialect.Provider.GetConverter<Guid>();
var myGuidConverter = (MyCustomGuidConverter)converter;
```

### Debugging Converters

Custom Converters can also enable a better debugging story where if you want to see what value gets retrieved 
from the database, you can override and add a breakpoint on the base method letting you inspect the value 
returned from the ADO.NET Data Reader:

```csharp
public class MyCustomGuidConverter : SqlServerGuidConverter
{
    public overridde object FromDbValue(Type fieldType, object value)
    {
        return base.FromDbValue(fieldType, value); //add breakpoint
    }
}
```

### Enhancing an existing Converter

An example of when you'd want to do this is if you wanted to use the `Guid` property in your POCO's on 
legacy tables which stored Guids in `VARCHAR` columns, in which case you can also add support for converting 
the returned strings back into Guid's with:

```csharp
public class MyCustomGuidConverter : SqlServerGuidConverter
{
    public overridde object FromDbValue(Type fieldType, object value)
    {
        var strValue = value as string; 
        return strValue != null
            ? new Guid(strValue);
            : base.FromDbValue(fieldType, value); 
    }
}
```

### Override handling of existing Types

Another popular Use Case now enabled with Converters is being able to override built-in functionality based
on preference. E.g. By default TimeSpans are stored in the database as Ticks in a `BIGINT` column since it's  
the most reliable way to retain the same TimeSpan value uniformly across all RDBMS's. 

E.g SQL Server's **TIME** data type can't store Times greater than 24 hours or with less precision than **3ms**. 
But if using a **TIME** column was preferred it can now be enabled by registering to use the new
[SqlServerTimeConverter](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite.SqlServer/Converters/SqlServerTimeConverter.cs) 
instead:

```csharp
SqlServerDialect.Provider.RegisterConverter<TimeSpan>(new SqlServerTimeConverter { 
   Precision = 7 
});
```

### Customizable Field Definitions

Another benefit is they allow for easy customization as seen with `Precision` property which will now 
create tables using the `TIME(7)` Column definition for TimeSpan properties.

For RDBMS's that don't have a native `Guid` type like Oracle or Firebird, you had an option to choose whether
you wanted to save them as text for better readability (default) or in a more efficient compact binary format. 
Previously this preference was maintained in a boolean flag along with multiple Guid implementations hard-coded 
at different entry points within each DialectProvider. This complexity has now been removed, now to store guids 
in a compact binary format you'll instead register the preferred Converter implementation, e.g:

```csharp
FirebirdDialect.Provider.RegisterConverter<Guid>(
    new FirebirdCompactGuidConverter());
```

### Changing String Column Behavior

This is another area improved with Converters where previously any available field customizations required 
maintaining state inside each provider. Now any customizations are encapsulated within each Converter and 
can be modified directly on its concrete Type without unnecessarily polluting the surface area of the primary 
[IOrmLiteDialectProvider](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/IOrmLiteDialectProvider.cs)
which used to create new API's every time a new customization option was added.

Now to customize the behavior of how strings are stored you can change them directly on the `StringConverter`, e.g:

```csharp
StringConverter converter = OrmLiteConfig.DialectProvider.GetStringConverter();
converter.UseUnicode = true;
converter.StringLength = 100;
```

Which will change the default column definitions for strings to use `NVARCHAR(100)` for RDBMS's that support
Unicode or `VARCHAR(100)` for those that don't. 

The `GetStringConverter()` API is just an extension method wrapping the generic `GetConverter()` API to return
a concrete type:

```csharp
public static StringConverter GetStringConverter(this IOrmLiteDialectProvider dialect)
{
    return (StringConverter)dialect.GetConverter(typeof(string));
}
```

Typed extension methods are also provided for other popular types offering additional customizations including
`GetDecimalConverter()` and `GetDateTimeConverter()`.

### Specify the DateKind in DateTimes

It's now much simpler and requires less effort to implement new features that maintain the same behavior 
across all supported RDBM's thanks to better cohesion, re-use and reduced internal state. One new feature
we've added as a result is the new `DateStyle` customization on `DateTimeConverter` which lets you change how 
Date's are persisted and populated, e.g: 

```csharp
DateTimeConverter converter = OrmLiteConfig.DialectProvider.GetDateTimeConverter();
converter.DateStyle = DateTimeKind.Local;
```

Will save `DateTime` in the database and populate them back on data models as LocalTime. 
This is also available for Utc:

```csharp
converter.DateStyle = DateTimeKind.Utc;
```

Default is `Unspecified` which doesn't do any conversions and just uses the DateTime returned by the ADO.NET provider.
Examples of the behavior of the different DateStyle's is available in
[DateTimeTests](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/tests/ServiceStack.OrmLite.Tests/DateTimeTests.cs).

### SQL Server Special Type Converters!

Just as the ground work for Converters were laid down, [@KevinHoward](https://github.com/KevinHoward) from the
ServiceStack Community noticed OrmLite could now be extended to support new Types and promptly contributed
Converters for SQL Server-specific 
[SqlGeography](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite.SqlServer.Converters/SqlServerGeographyTypeConverter.cs), 
[SqlGeometry](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite.SqlServer.Converters/SqlServerGeometryTypeConverter.cs) 
and 
[SqlHierarchyId](https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite.SqlServer.Converters/SqlServerHierarchyIdTypeConverter.cs) 
Types!

Since these Types require an external dependency to the **Microsoft.SqlServer.Types** NuGet package they're
contained in a separate NuGet package that can be installed with:

    PM> Install-Package ServiceStack.OrmLite.SqlServer.Converters

Alternative Strong-named version:

    PM> Install-Package ServiceStack.OrmLite.SqlServer.Converters.Signed

Once installed, all available SQL Server Types can be registered on your SQL Server Provider with:

```csharp
SqlServerConverters.Configure(SqlServer2012Dialect.Provider);
```

#### Example Usage

After the Converters are registered they can treated like a normal .NET Type, e.g:

**SqlHierarchyId** Example:

```csharp
public class Node {
    [AutoIncrement]
    public long Id { get; set; }
    public SqlHierarchyId TreeId { get; set; }
}

db.DropAndCreateTable<Node>();

var treeId = SqlHierarchyId.Parse("/1/1/3/"); // 0x5ADE is hex
db.Insert(new Node { TreeId = treeId });

var parent = db.Scalar<SqlHierarchyId>(db.From<Node>().Select("TreeId.GetAncestor(1)"));
parent.ToString().Print(); //= /1/1/
```

**SqlGeography** and **SqlGeometry** Example:

```csharp
public class GeoTest {
    public long Id { get; set; }
    public SqlGeography Location { get; set; }
    public SqlGeometry Shape { get; set; }
}

db.DropAndCreateTable<GeoTest>();

var geo = SqlGeography.Point(40.6898329,-74.0452177, 4326); // Statue of Liberty

// A simple line from (0,0) to (4,4)  Length = SQRT(2 * 4^2)
var wkt = new System.Data.SqlTypes.SqlChars("LINESTRING(0 0, 4 4)".ToCharArray());
var shape = SqlGeometry.STLineFromText(wkt, 0);

db.Insert(new GeoTestTable { Id = 1, Location = geo, Shape = shape });
var dbShape = db.SingleById<GeoTest>(1).Shape;

new { dbShape.STEndPoint().STX, dbShape.STEndPoint().STY }.PrintDump();
```

Output:

    {
        STX: 4,
        STY: 4
    }
    
### New SQL Server 2012 Dialect Provider

There's a new `SqlServer2012Dialect.Provider` to take advantage of optimizations available in recent versions 
of SQL Server, that's now recommended for use with SQL Server 2012 and later.

```csharp
container.Register<IDbConnectionFactory>(c => 
    new OrmLiteConnectionFactory(connString, SqlServer2012Dialect.Provider);
```

The new `SqlServer2012Dialect` takes advantage of SQL Server's new **OFFSET** and **FETCH** support to enable more 
[optimal paged queries](http://dbadiaries.com/new-t-sql-features-in-sql-server-2012-offset-and-fetch) 
that replaces the 
[Windowing Function hack](http://stackoverflow.com/a/2135449/85785) 
required to support earlier versions of SQL Server.

### Nested Typed Sub SqlExpressions

The `Sql.In()` API has been expanded by [Johann Klemmack](https://github.com/jklemmack) to support nesting
and combining of multiple Typed SQL Expressions together in a single SQL Query, e.g:
  
```csharp
var usaCustomerIds = db.From<Customer>(c => c.Country == "USA").Select(c => c.Id);
var usaCustomerOrders = db.Select(db.From<Order>()
    .Where(q => Sql.In(q.CustomerId, usaCustomerIds)));
```
 
### Descending Indexes

Descending composite Indexes can be declared with:
 
 ```csharp
[CompositeIndex("Field1", "Field2 DESC")]
public class Poco { ... }
```

### Dapper Updated

The embedded version of Dapper in the `ServiceStack.OrmLite.Dapper` namespace has been upgraded to the 
latest version of Dapper and also includes Dapper's Async API's in .NET 4.5 builds.

#### CSV Support for dynamic Dapper results

The CSV Serializer also added support for Dapper's dynamic results:

```csharp
IEnumerable<dynamic> results = db.Query("select * from Poco");
string csv = CsvSerializer.SerializeToCsv(results);
```

#### OrmLite CSV Example

OrmLite avoids `dynamic` and instead prefers the use of code-first POCO's, where the above example translates to:

```csharp
var results = db.Select<Poco>();
var csv = results.ToCsv();
```

To query untyped results in OrmLite when no POCO's exist, you can read them into a generic Dictionary:

```csharp 
var results = db.Select<Dictionary<string,object>>("select * from Poco");
var csv = results.ToCsv();
```

### Order By Random

The new `OrderByRandom()` API abstracts the differences in each RDBMS to return rows in a random order:

```csharp 
var randomRows = db.Select<Poco>(q => q.OrderByRandom());
```

### Other OrmLite Features

`CreateTableIfNotExists` returns `true` if a new table was created which is convenient for only populating
non-existing tables with new data on your Application StartUp, e.g:
 
 ```csharp
if (db.CreateTableIfNotExists<Poco>()) {
    AddSeedData(db);
}
 ```

 - OrmLite Debug Logging includes DB Param names and values  
 - Char fields now use CHAR(1)
  
## [Encrypted Messaging](https://github.com/ServiceStack/ServiceStack/wiki/Encrypted-Messaging)

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/encrypted-messaging.png)

### Encrypted Messages verified with HMAC SHA-256

The authenticity of Encrypted Messages are now being verified with HMAC SHA-256, essentially following an 
[Encrypt-then-MAC strategy](http://crypto.stackexchange.com/a/205/25652). The change to the existing process
is that a new AES 256 **Auth Key** is used to Authenticate the encrypted data which is then sent along
with the **Crypt Key**, encrypted with the Server's Public Key. 

An updated version of this process is now:

  1. Client creates a new `IEncryptedClient` configured with the Server **Public Key**
  2. Client uses the `IEncryptedClient` to create a EncryptedMessage Request DTO:
    1. Generates a new AES 256bit/CBC/PKCS7 Crypt Key **(Kc)**, Auth Key **(Ka)** and **IV**
    2. Encrypts Crypt Key **(Kc)**, Auth Key **(Ka)** with Servers Public Key padded with OAEP = **(Kc+Ka+P)e**
    3. Authenticates **(Kc+Ka+P)e** with **IV** using HMAC SHA-256 = **IV+(Kc+Ka+P)e+Tag**
    4. Serializes Request DTO to JSON packed with current `Timestamp`, `Verb` and `Operation` = **(M)**
    5. Encrypts **(M)** with Crypt Key **(Kc)** and **IV** = **(M)e**
    6. Authenticates **(M)e** with Auth Key **(Ka)** and **IV** = **IV+(M)e+Tag**
    7. Creates `EncryptedMessage` DTO with Servers `KeyId`, **IV+(Kc+Ka+P)e+Tag** and **IV+(M)e+Tag**
  3. Client uses the `IEncryptedClient` to send the populated `EncryptedMessage` to the remote Server

On the Server, the `EncryptedMessagingFeature` Request Converter processes the `EncryptedMessage` DTO:

  1. Uses Private Key identified by **KeyId** or the current Private Key if **KeyId** wasn't provided
    1. Request Converter Extracts **IV+(Kc+Ka+P)e+Tag** into **IV** and **(Kc+Ka+P)e+Tag**
    2. Decrypts **(Kc+Ka+P)e+Tag** with Private Key into **(Kc)** and **(Ka)**
    3. The **IV** is checked against the nonce Cache, verified it's never been used before, then cached
    4. The **IV+(Kc+Ka+P)e+Tag** is verified it hasn't been tampered with using Auth Key **(Ka)**
    5. The **IV+(M)e+Tag** is verified it hasn't been tampered with using Auth Key **(Ka)**
    6. The **IV+(M)e+Tag** is decrypted using Crypt Key **(Kc)** = **(M)**
    7. The **timestamp** is verified it's not older than `EncryptedMessagingFeature.MaxRequestAge`
    8. Any expired nonces are removed. (The **timestamp** and **IV** are used to prevent replay attacks)
    9. The JSON body is deserialized and resulting **Request DTO** returned from the Request Converter
  2. The converted **Request DTO** is executed in ServiceStack's Request Pipeline as normal
  3. The **Response DTO** is picked up by the EncryptedMessagingFeature **Response Converter**:
    1. Any **Cookies** set during the Request are removed
    2. The **Response DTO** is serialized with the **AES Key** and returned in an `EncryptedMessageResponse`
  4. The `IEncryptedClient` decrypts the `EncryptedMessageResponse` with the **AES Key**
    1. The **Response DTO** is extracted and returned to the caller

### Support for versioning Private Keys with Key Rotations

Another artifact introduced in the above process was the mention of a new `KeyId`. 
This is a human readable string used to identify the Servers Public Key using the first 7 characters
of the Public Key Modulus (visible when viewing the Private Key serialized as XML). 
This is automatically sent by `IEncryptedClient` to tell the `EncryptedMessagingFeature` which Private Key 
should be used to decrypt the AES Crypt and Auth Keys.

By supporting multiple private keys, the Encrypted Messaging feature allows the seamless transition to a 
new Private Key without affecting existing clients who have yet to adopt the latest Public Key. 

Transitioning to a new Private Key just involves taking the existing Private Key and adding it to the 
`FallbackPrivateKeys` collection whilst introducing a new Private Key, e.g:

```csharp
Plugins.Add(new EncryptedMessagesFeature
{
    PrivateKey = NewPrivateKey,
    FallbackPrivateKeys = {
        PreviousKey2015,
        PreviousKey2014,
    },
});
```

### Why Rotate Private Keys?

Since anyone who has a copy of the Private Key can decrypt encrypted messages, rotating the private key clients
use limits the amount of exposure an adversary who has managed to get a hold of a compromised private key has. 
i.e. if the current Private Key was somehow compromised, an attacker with access to the encrypted 
network packets will be able to read each message sent that was encrypted with the compromised private key 
up until the Server introduces a new Private Key which clients switches over to.

## [Swagger UI](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)

The Swagger Metadata backend has been upgraded to support the 
[Swagger 1.2 Spec](https://github.com/swagger-api/swagger-spec/blob/master/versions/1.2.md)

### Basic Auth added to Swagger UI

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/swagger-basicauth.png)

Users can call protected Services using the Username and Password fields in Swagger UI. 
Swagger sends these credentials with every API request using HTTP Basic Auth, 
which can be enabled in your AppHost with:

```csharp
Plugins.Add(new AuthFeature(...,
      new IAuthProvider[] { 
        new BasicAuthProvider(), //Allow Sign-ins with HTTP Basic Auth
      }));
```

Alternatively users can login outside of Swagger, to access protected Services in Swagger UI.
  
## Auth Info displayed in [Metadata Pages](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page)

Metadata pages now label protected Services. On the metadata index page it displays a yellow key next to
each Service requiring Authentication:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/metadata-auth-summary.png)

Hovering over the key will show which also permissions or roles the Service needs.

This information is also shown the metadata detail pages which will list which permissions/roles are required (if any), e.g:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/metadata-auth.png)

## [Java Native Types](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

### [Java Functional Utils](https://github.com/mythz/java-linq-examples)

The Core Java Functional Utils required to run 
[C#'s 101 LINQ Samples in Java](https://github.com/mythz/java-linq-examples) 
have been added to the **net.servicestack:client** Java package which as its compatible with Java 1.7, 
also runs on Android:

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/java/linq-examples-screenshot.png)](https://github.com/mythz/java-linq-examples)

Whilst noticeably more verbose than most languages, it enables a functional style of programming that provides
an alternative to imperative programming with mutating collections and eases porting efforts of functional code 
which can be mapped to its equivalent core functional method.

### New TreatTypesAsStrings option

Due to the [unusual encoding of Guid bytes](http://stackoverflow.com/a/18085116/85785) it may be instead be 
preferential to treat Guids as opaque strings so they are easier to compare back to their original C# Guids. 
This can be enabled with the new `TreatTypesAsStrings` option:

```
/* Options:
...
TreatTypesAsStrings: Guid

*/
```

Which will emit `String` data types for `Guid` properties that are deserialized back into .NET Guid's as strings.

## [Swift Native Types](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference)

All Swift reserved keywords are now escaped, allowing them to be used in DTO's.

## [Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client)

All .NET Service Clients (inc [JsonHttpClient](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client#jsonhttpclient)) 
can now be used to send raw `string`, `byte[]` or `Stream` Request bodies in their custom Sync or Async API's, e.g:
 
```csharp
string json = "{\"Key\":1}";
client.Post<SendRawResponse>("/sendraw", json);

byte[] bytes = json.ToUtf8Bytes();
client.Put<SendRawResponse>("/sendraw", bytes);

Stream stream = new MemoryStream(bytes);
await client.PostAsync<SendRawResponse>("/sendraw", stream);
```
 
## Authentication

### [Community Azure Active Directory Auth Provider](https://github.com/jfoshee/ServiceStack.Authentication.Aad)

[Jacob Foshee](https://github.com/jfoshee) from the ServiceStack Community has published a 
[ServiceStack AuthProvider for Azure Active Directory](https://github.com/jfoshee/ServiceStack.Authentication.Aad).

To get started, Install it from NuGet with:

    PM> Install-Package ServiceStack.Authentication.Aad

Then add the `AadAuthProvider` AuthProvider to your `AuthFeature` registration:

```csharp
Plugins.Add(new AuthFeature(..., 
    new IAuthProvider[] { 
        new AadAuthProvider(AppSettings) 
    });
```

See the docs on the 
[Projects Homepage](https://github.com/jfoshee/ServiceStack.Authentication.Aad)
for instructions on how to Configure the Azure Directory OAuth Provider in your applications `<appSettings/>`.

### MaxLoginAttempts

The `MaxLoginAttempts` feature has been moved out from `OrmLiteAuthRepository` into a global option in the 
`AuthFeature` plugin where this feature has now been added to all User Auth Repositories. 

E.g. you can lock a User Account after 5 invalid login attempts with:
 
```csharp
Plugins.Add(new AuthFeature(...) {
    MaxLoginAttempts = 5
});
```
 
### Generate New Session Cookies on Authentication 

Previously the Authentication provider only removed Users Cookies after they explicitly log out. 
The AuthFeature now also regenerates new Session Cookies each time users login. 
If you were previously relying on the user maintaining the same cookies 
(i.e. tracking anonymous user activity) this behavior can be disabled with:

```csharp
Plugins.Add(new AuthFeature(...) {
    GenerateNewSessionCookiesOnAuthentication = false
});
```

### ClientId and ClientSecret OAuth Config Aliases
 
OAuth Providers can now use `ClientId` and `ClientSecret` aliases instead of `ConsumerKey` and `ConsumerSecret`, e.g:

```xml 
<appSettings>
    <add key="oauth.twitter.ClientId" value="..." />
    <add key="oauth.twitter.ClientSecret" value="..." />
</appSettings>
```

## [Error Handling](https://github.com/ServiceStack/ServiceStack/wiki/Error-Handling)

### Custom Response Error Codes

In addition to customizing the HTTP Response Body of C# Exceptions with 
[IResponseStatusConvertible](https://github.com/ServiceStack/ServiceStack/wiki/Error-Handling#implementing-iresponsestatusconvertible), 
you can also customize the HTTP Status Code by implementing `IHasStatusCode`:

```csharp
public class Custom401Exception : Exception, IHasStatusCode
{
    public int StatusCode 
    { 
        get { return 401; } 
    }
}
```

Which is a more cohesive alternative that registering the equivalent 
[StatusCode Mapping](https://github.com/ServiceStack/ServiceStack/wiki/Error-Handling#custom-mapping-of-c-exceptions-to-http-error-status):

```csharp
SetConfig(new HostConfig { 
    MapExceptionToStatusCode = {
        { typeof(Custom401Exception), 401 },
    }
});
```

### Meta Dictionary on ResponseStatus and ResponseError

The [IMeta](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/IMeta.cs)
Dictionary has been added to `ResponseStatus` and `ResponseError` DTO's which provides a placeholder to 
be able to send additional context with errors. 

This Meta dictionary will be automatically populated for any `CustomState` on FluentValidation `ValidationFailure` 
that's populated with a `Dictionary<string, string>`.

## [Server Events](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)

The new `ServerEventsFeature.HouseKeepingInterval` option controls the minimum interval for how often SSE 
connections should be routinely scanned and expired subscriptions removed. The default is every 5 seconds.

> As there's no background Thread managing SSE connections, the cleanup happens in periodic SSE heartbeat handlers

Update: An [issue with this feature](https://github.com/ServiceStack/Issues/issues/345) has been resolved 
in the [v4.0.45 pre-release NuGet packages on MyGet](https://github.com/ServiceStack/ServiceStack/wiki/MyGet).

## ServiceStack.Text

There are new convenient extension methods for Converting any POCO to and from Object Dictionary, e.g:

```csharp
var dto = new User
{
    FirstName = "First",
    LastName = "Last",
    Car = new Car { Age = 10, Name = "ZCar" },
};

Dictionary<string,object> map = dtoUser.ToObjectDictionary();

User user = (User)map.FromObjectDictionary(typeof(User));
```

Like most Reflection API's in ServiceStack this is fairly efficient as it uses cached compiled delegates.

There's also an extension method for adding types to `List<Type>`, e.g:

```csharp
var types = new List<Type>()
    .Add<User>()
    .Add<Car>();
```

Which is a cleaner equivalent to:

```csharp
var types = new List<Type>();
types.Add(typeof(User));
types.Add(typeof(User));
```

# v4.0.42 Release Notes

## New JsonHttpClient!

The new `JsonHttpClient` is an alternative to the existing generic typed `JsonServiceClient` for consuming ServiceStack Services 
which instead of `HttpWebRequest` is based on Microsoft's latest async `HttpClient` (from [Microsoft.Net.Http](https://www.nuget.org/packages/Microsoft.Net.Http) on NuGet). 

`JsonHttpClient` implements the full [IServiceClient API](https://gist.github.com/mythz/4683438240820b522d39) making it an easy drop-in replacement for your existing `JsonServiceClient` 
where in most cases it can simply be renamed to `JsonHttpClient`, e.g:

```csharp
//IServiceClient client = new JsonServiceClient("http://techstacks.io");
IServiceClient client = new JsonHttpClient("http://techstacks.io");
```

Which can then be [used as normal](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client):

```csharp
var response = await client.GetAsync(new GetTechnology { Slug = "servicestack" });
```

### Install

JsonHttpClient can be downloaded from NuGet at:

    > Install-Package ServiceStack.HttpClient

### PCL Support

JsonHttpClient also comes in PCL flavour and can be used on the same platforms as the 
[existing PCL Service Clients](https://github.com/ServiceStackApps/HelloMobile) enabling the same clean and productive development experience on popular mobile platforms like 
[Xamarin.iOS](http://developer.xamarin.com/guides/ios/) and [Xamarin.Android](http://developer.xamarin.com/guides/android/).

### [ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient)

One of the primary benefits of being based on `HttpClient` is being able to make use of 
[ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient) which provides a thin wrapper around iOS's native `NSURLSession` or `OkHttp` client on Android, offering improved stability for 3G mobile connectivity.

To enable, install [ModernHttpClient](https://www.nuget.org/packages/ModernHttpClient) then set the 
Global HttpMessageHandler Factory to configure all `JsonHttpClient` instances to use ModernHttpClient's `NativeMessageHandler`: 

```csharp
JsonHttpClient.GlobalHttpMessageHandlerFactory = () => new NativeMessageHandler();
```

Alternatively, you can configure a single client instance to use ModernHttpClient with:

```csharp
client.HttpMessageHandler = new NativeMessageHandler();
```

### Differences with JsonServiceClient

Whilst our goal is to retain the same behavior in both clients, there are some differences resulting from using HttpClient where the Global and Instance Request and Response Filters are instead passed HttpClients `HttpRequestMessage` and `HttpResponseMessage`. 

Also, all API's are **Async** under-the-hood where any Sync API's that doesn't return a `Task<T>` just blocks on the Async `Task.Result` response. As this can dead-lock in certain environments we recommend sticking with the Async API's unless safe to do otherwise. 

## Encrypted Messaging!

One of the benefits of adopting a message-based design is being able to easily layer functionality and generically add value to all Services, we've seen this recently with [Auto Batched Requests](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Batched-Requests) which automatically enables each Service to be batched and executed in a single HTTP Request. Similarly the new Encrypted Messaging feature 
enables a secure channel for all Services (inc Auto Batched Requests :) offering protection to clients who can now easily send and receive encrypted messages over unsecured HTTP!

### Encrypted Messaging Overview

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/encrypted-messaging.png)

### Configuration

Encrypted Messaging support is enabled by registering the plugin:

```csharp
Plugins.Add(new EncryptedMessagesFeature {
    PrivateKeyXml = ServerRsaPrivateKeyXml
});
```

Where `PrivateKeyXml` is the Servers RSA Private Key Serialized as XML. If you don't have an existing one, a new one can be generated with:

```csharp
var rsaKeyPair = RsaUtils.CreatePublicAndPrivateKeyPair();
string ServerRsaPrivateKeyXml = rsaKeyPair.PrivateKey;
```

Once generated, it's important the Private Key is kept confidential as anyone with access will be able to decrypt 
the encrypted messages! Whilst most [obfuscation efforts are ultimately futile](http://stackoverflow.com/a/6018247/85785) the goal should be to contain the private key to your running Web Application, limiting access as much as possible.

Once registered, the EncryptedMessagesFeature enables the 2 Services below:

 - `GetPublicKey` - Returns the Serialized XML of your Public Key (extracted from the configured Private Key)
 - `EncryptedMessage` - The Request DTO which encapsulates all encrypted Requests (can't be called directly)

### Giving Clients the Public Key

To communicate clients need access to the Server's Public Key, it doesn't matter who has accessed the Public Key only that clients use the **real** Servers Public Key. It's therefore not advisable to download the Public Key over unsecure `http://` where traffic can potentially be intercepted and the key spoofed, subjecting them to a [Man-in-the-middle attack](https://en.wikipedia.org/wiki/Man-in-the-middle_attack). 

It's safer instead to download the public key over a trusted `https://` url where the servers origin is verified by a trusted [CA](https://en.wikipedia.org/wiki/Certificate_authority). Sharing the Public Key over Dropbox, Google Drive, OneDrive or other encrypted channels are also good options.

Since `GetPublicKey` is just a ServiceStack Service it's easily downloadable using a Service Client:

```csharp
var client = new JsonServiceClient(BaseUrl);
string publicKeyXml = client.Get(new GetPublicKey());
```

If the registered `EncryptedMessagesFeature.PublicKeyPath` has been changed from its default `/publickey`, it can be dowloaded with:

```csharp
string publicKeyXml = client.Get<string>("/custom-publickey"); // or with HttpUtils:
string publicKeyXml = BaseUrl.CombineWith("/custom-publickey").GetStringFromUrl();
```

> To help with verification the SHA256 Hash of the PublicKey is returned in `X-PublicKey-Hash` HTTP Header

### Encrypted Service Client

Once they have the Server's Public Key, clients can use it to get an `EncryptedServiceClient` via the `GetEncryptedClient()` extension method on `JsonServiceClient` or new `JsonHttpClient`, e.g:

```csharp
var client = new JsonServiceClient(BaseUrl);
IEncryptedClient encryptedClient = client.GetEncryptedClient(publicKeyXml);
```

Once configured, clients have access to the familiar typed Service Client API's and productive workflow they're used to with the generic Service Clients, sending typed Request DTO's and returning the typed Response DTO's - rendering the underlying encrypted messages a transparent implementation detail:

```csharp
HelloResponse response = encryptedClient.Send(new Hello { Name = "World" });
response.Result.Print(); //Hello, World!
```

REST Services Example:

```csharp
HelloResponse response = encryptedClient.Get(new Hello { Name = "World" });
```

Auto-Batched Requests Example:

```csharp
var requests = new[] { "Foo", "Bar", "Baz" }.Map(x => new HelloSecure { Name = x });
var responses = encryptedClient.SendAll(requests);
```

When using the `IEncryptedClient`, the entire Request and Response bodies are encrypted including Exceptions which continue to throw a populated `WebServiceException`:

```csharp
try
{
    var response = encryptedClient.Send(new Hello());
}
catch (WebServiceException ex)
{
    ex.ResponseStatus.ErrorCode.Print(); //= ArgumentNullException
    ex.ResponseStatus.Message.Print();   //= Value cannot be null. Parameter name: Name
}
```

### Authentication with Encrypted Messaging

Many encrypted messaging solutions use Client Certificates which Servers can use to cryptographically verify a client's identity - providing an alternative to HTTP-based Authentication. We've decided against using this as it would've forced an opinionated implementation and increased burden of PKI certificate management and configuration onto Clients and Servers - reducing the applicability and instant utility of this feature.

We can instead leverage the existing Session-based Authentication Model in ServiceStack letting clients continue to use the existing Auth functionality and Auth Providers they're already used to, e.g:

```csharp
var authResponse = encryptedClient.Send(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "test@gmail.com",
    Password = "p@55w0rd",
});
```

Encrypted Messages have their cookies stripped so they're no longer visible in the clear which minimizes their exposure to Session hijacking. This does pose the problem of how we can call authenticated Services if the encrypted HTTP Client is no longer sending Session Cookies? 

Without the use of clear-text Cookies or HTTP Headers there's no longer an *established Authenticated Session* for the `encryptedClient` to use to make subsequent Authenticated requests. What we can do  instead is pass the Session Id in the encrypted body for Request DTO's that implement the new `IHasSessionId` interface, e.g:

```csharp
[Authenticate]
public class HelloAuthenticated : IReturn<HelloAuthenticatedResponse>, IHasSessionId
{
    public string SessionId { get; set; }
    public string Name { get; set; }
}

var response = encryptedClient.Send(new HelloAuthenticated {
    SessionId = authResponse.SessionId,
    Name = "World"
});
```

Here we're injecting the returned Authenticated `SessionId` to access the `[Authenticate]` protected Request DTO. However remembering to do this for every authenticated request can get tedious, a nicer alternative is just setting it once on the `encryptedClient` which will then use it to automatically populate any `IHasSessionId` Request DTO's:

```csharp
encryptedClient.SessionId = authResponse.SessionId;

var response = encryptedClient.Send(new HelloAuthenticated {
    Name = "World"
});
```

> Incidentally this feature is now supported in **all Service Clients**

### Combined Authentication Strategy

Another potential use-case is to only use Encrypted Messaging when sending any sensitive information and the normal Service Client for other requests. In which case we can Authenticate and send the user's password with the `encryptedClient`:

```csharp
var authResponse = encryptedClient.Send(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "test@gmail.com",
    Password = "p@55w0rd",
});
```

But then fallback to using the normal `IServiceClient` for subsequent requests. But as the `encryptedClient` doesn't receive cookies we'd need to set it explicitly on the client ourselves with:

```csharp
client.SetCookie("ss-id", authResponse.SessionId);
```

After which the ServiceClient "establishes an authenticated session" and can be used to make Authenticated requests, e.g:

```csharp
var response = await client.GetAsync(new HelloAuthenticated { Name = "World" });
```

> Note: EncryptedServiceClient is unavailable in PCL Clients

### [Hybrid Encryption Scheme](https://en.wikipedia.org/wiki/Hybrid_cryptosystem)

The Encrypted Messaging Feature follows a [Hybrid Cryptosystem](https://en.wikipedia.org/wiki/Hybrid_cryptosystem) which uses RSA Public Keys for [Asymmetric Encryption](https://en.wikipedia.org/wiki/Public-key_cryptography) combined with the performance of AES [Symmetric Encryption](https://en.wikipedia.org/wiki/Symmetric-key_algorithm) making it suitable for encrypting large message payloads. 

The key steps in the process are outlined below:

  1. The Client creates a new `IEncryptedClient` configured with the Server **Public Key**
  2. The Client uses the `IEncryptedClient` to send a Request DTO:
    1. A new 256-bit Symmetric **AES Key** and [IV](https://en.wikipedia.org/wiki/Initialization_vector) is generated
    2. The **AES Key** and **IV** bytes are merged, encrypted with the Servers **Public Key** and Base64 encoded
    3. The Request DTO is serialized into JSON and packed with the current **Timestamp**, **Verb** and **Operation** and encrypted with the new **AES Key**
  3. The `IEncryptedClient` uses the underlying JSON Service Client to send the `EncryptedMessage` to the remote Server
  4. The `EncryptedMessage` is picked up and decrypted by the EncryptedMessagingFeature **Request Converter**:
    1. The **AES Key** is decrypted with the Servers **Private Key**
    2. The **IV** is checked against the nonce Cache, verified it's never been used before, then cached 
    3. The unencrypted **AES Key** is used to decrypt the **EncryptedBody**
    4. The **timestamp** is verified it's not older than `EncryptedMessagingFeature.MaxRequestAge`
    5. Any expired nonces are removed. (The **timestamp** and **IV** are used to prevent replay attacks)
    6. The JSON body is deserialized and resulting **Request DTO** returned from the Request Converter
  5. The converted **Request DTO** is executed in ServiceStack's Request Pipeline as normal
  6. The **Response DTO** is picked up by the EncryptedMessagingFeature **Response Converter**:
    1. Any **Cookies** set during the Request are removed
    2. The **Response DTO** is serialized with the **AES Key** and returned in an `EncryptedMessageResponse`
  7. The `IEncryptedClient` decrypts the `EncryptedMessageResponse` with the **AES Key**
    1. The **Response DTO** is extracted and returned to the caller

A visual of how this all fits together in captured in the high-level diagram below:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/encrypted-messaging.png)

 - Components in **Yellow** show the encapsulated Encrypted Messaging functionality where all encryption and decryption is performed
 - Components in **Blue** show Unencrypted DTO's
 - Components in **Green** show Encrypted content:
    - The AES Key and IV in **Dark Green** is encrypted by the client using the Server's Public Key
    - The EncryptedRequest in **Light Green** is encrypted with a new AES Key generated by the client on each Request
 - Components in **Dark Grey** depict existing ServiceStack functionality where Requests are executed as normal through the [Service Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) and [Request Pipeline](https://github.com/ServiceStack/ServiceStack/wiki/Order-of-Operations)

All Request and Response DTO's get encrypted and embedded in the `EncryptedMessage` and `EncryptedMessageResponse` DTO's below:

```csharp
public class EncryptedMessage : IReturn<EncryptedMessageResponse>
{
    public string EncryptedSymmetricKey { get; set; }
    public string EncryptedBody { get; set; }
}

public class EncryptedMessageResponse
{
    public string EncryptedBody { get; set; }
}
```

The diagram also expands the `EncryptedBody` Content containing the **EncryptedRequest** consisting of the following parts:

 - **Timestamp** - Unix Timestamp of the Request
 - **Verb** - Target HTTP Method
 - **Operation** - Request DTO Name
 - **JSON** - Request DTO serialized as JSON

### Source Code

 - The Client implementation is available in [EncryptedServiceClient.cs](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Client/EncryptedServiceClient.cs)
 - The Server implementation is available in [EncryptedMessagesFeature.cs](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/EncryptedMessagesFeature.cs)
 - The Crypto Utils used are available in the [RsaUtils.cs](https://github.com/ServiceStack/ServiceStack/blob/b4a2a1f74936c8a100b688cbdbca08ff5b212cbe/src/ServiceStack.Client/CryptUtils.cs#L31) and [AesUtils.cs](https://github.com/ServiceStack/ServiceStack/blob/b4a2a1f74936c8a100b688cbdbca08ff5b212cbe/src/ServiceStack.Client/CryptUtils.cs#L189)
 - Tests are available in [EncryptedMessagesTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/UseCases/EncryptedMessagesTests.cs)

## Request and Response Converters

The Encrypted Messaging Feature takes advantage of new Converters that let you change the Request DTO and Response DTO's that get used in ServiceStack's Request Pipeline where:

Request Converters are executed directly after any [Custom Request Binders](https://github.com/ServiceStack/ServiceStack/wiki/Serialization-deserialization#create-a-custom-request-dto-binder):

```csharp
appHost.RequestConverters.Add((req, requestDto) => {
    //Return alternative Request DTO or null to retain existing DTO
});
```

Response Converters are executed directly after the Service:

```csharp
appHost.ResponseConverters.Add((req, response) =>
    //Return alternative Response or null to retain existing Service response
});
```

In addition to the converters above, Plugins can now register new callbacks in `IAppHost.OnEndRequestCallbacks` which gets fired at the end of a request.

## [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

### Eclipse Integration!

We've further expanded our support for Java with our new **ServiceStackEclipse** plugin providing cross-platform [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) integration with Eclipse on Windows, OSX and Linux!

#### Install from Eclipse Marketplace

To install, search for **ServiceStack** in the Eclipse Marketplace at `Help > Eclipse Marketplace`:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/ss-eclipse-install-win.gif)

Find the **ServiceStackEclipse** plugin, click **Install** and follow the wizard to the end, restarting to launch Eclipse with the plugin loaded!

> **ServiceStackEclipse** is best used with Java Maven Projects where it automatically adds the **ServiceStack.Java** client library to your Maven Dependencies and when your project is set to **Build Automatically**, are then downloaded and registered, so you're ready to start consuming ServiceStack Services with the new `JsonServiceClient`!

#### Eclipse Add ServiceStack Reference

Just like Android Studio you can right-click on a Java Package to open the **Add ServiceStack Reference...** dialog from the Context Menu:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/add-reference-demo.gif)

Complete the dialog to add the remote Servers generated Java DTO's to your selected Java package and the `net.servicestack.client` dependency to your Maven dependencies.

#### Eclipse Update ServiceStack Reference

Updating a ServiceStack Reference works as normal where you can change any of the available options in the header comments, save, then right-click on the file in the File Explorer and click on **Update ServiceStack Reference** in the Context Menu:
 
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/update-reference-demo.gif)

### ServiceStack IDEA IntelliJ Plugin 

The **ServiceStackIDEA** plugin has added support for IntelliJ Maven projects giving Java devs a productive and familiar development experience whether they're creating Android Apps or pure cross-platform Java clients.

#### Install ServiceStack IDEA from the Plugin repository

The ServiceStack IDEA is now available to install directly from within IntelliJ or Android Studio IDE Plugins Repository, to Install Go to: 

 1. `File -> Settings...` Main Menu Item
 2. Select **Plugins** on left menu then click **Browse repositories...** at bottom
 3. Search for **ServiceStack** and click **Install plugin**
 4. Restart to load the installed ServiceStack IDEA plugin

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackidea/android-plugin-download.gif)

### ssutil.exe - Command line ServiceStack Reference tool

Add ServiceStack Reference is also moving beyond our growing list of supported IDEs and is now available in a single cross-platform .NET command-line **.exe** making it easy for build servers and automated tasks or command-line runners of your favorite text editors to easily Add and Update ServiceStack References!

To Get Started download **ssutil.exe** and open a command prompt to the containing directory:

#### Download [ssutil.exe](https://github.com/ServiceStack/ServiceStackVS/raw/master/dist/ssutil.exe)

#### ssutil.exe Usage:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/ssutil-help.png)

**Adding a new ServiceStack Reference**

To create a new ServiceStack Reference, pass the remote ServiceStack **BaseUrl** then specify both which `-file` and `-lang` you want, e.g:

    ssutil http://techstacks.io -file TechStacks -lang CSharp

Executing the above command fetches the C# DTOs and saves them in a local file named `TechStacks.dtos.cs`.

**Available Languages**

 - CSharp
 - FSharp
 - VbNet
 - Java
 - Swift
 - TypeScript.d

**Update existing ServiceStack Reference**

Updating a ServiceStack Reference is even easier we just specify the path to the existing generated DTO's. E.g. Update the `TechStacks.dtos.cs` we just created with:

    ssutil TechStacks.dtos.cs

### [Using Xamarin.Auth with ServiceStack](https://github.com/ServiceStackApps/TechStacksAuth)

[Xamarin.Auth](https://components.xamarin.com/gettingstarted/xamarin.auth) 
is an extensible Component and provides a good base for handling authenticating with ServiceStack from Xamarin platforms. To show how to make use of it we've created the [TechStacksAuth](https://github.com/ServiceStackApps/TechStacksAuth) example repository containing a custom `WebAuthenticator` we use to call our remote ServiceStack Web Application and reuse its existing OAuth integration. 

Here's an example using `TwitterAuthProvider`:

![](https://github.com/ServiceStack/Assets/raw/master/img/apps/TechStacks/xamarin-android-auth-demo.gif)

Checkout the [TechStacksAuth](https://github.com/ServiceStackApps/TechStacksAuth) repo for the docs and source code.

### Swift

Unfortunately the recent release of Xcode 6.4 and Swift 1.2 still haven't fixed the [earlier regression added in Xcode 6.3 and Swift 1.2](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2015/release-notes.md#swift-native-types-upgraded-to-swift-12) where the Swift compiler segfaults trying to compile Extensions to Types with a Generic Base Class. The [swift-compiler-crashes](https://github.com/practicalswift/swift-compiler-crashes) repository is reporting this is now fixed in Swift 2.0 / Xcode 7 beta but as that wont be due till later this year we've decided to improve the experience by not generating any types with the problematic Generic Base Types from the generated DTO's by default. This is configurable with:

```swift
//ExcludeGenericBaseTypes: True
```

Any types that were omitted from the generated DTO's will be emitted in comments, using the format:

```swift
//Excluded: {TypeName}
```

### C#, F#, VB.NET Service Reference

The C#, F# and VB.NET Native Type providers can emit `[GeneratedCode]` attributes with:

```csharp
AddGeneratedCodeAttributes: True
```

This is useful for skipping any internal Style Cop rules on generated code.

## Service Clients

### Custom Client Caching Strategy

New `ResultsFilter` and `ResultsFilterResponse` delegates have been added to all Service Clients allowing clients to employ a custom caching strategy. 

Here's a basic example implementing a cache for all **GET** Requests:

```csharp
var cache = new Dictionary<string, object>();

client.ResultsFilter = (type, method, uri, request) => {
    if (method != HttpMethods.Get) return null;
    object cachedResponse;
    cache.TryGetValue(uri, out cachedResponse);
    return cachedResponse;
};
client.ResultsFilterResponse = (webRes, response, method, uri, request) => {
    if (method != HttpMethods.Get) return;
    cache[uri] = response;
};

//Subsequent requests returns cached result
var response1 = client.Get(new GetCustomer { CustomerId = 5 });
var response2 = client.Get(new GetCustomer { CustomerId = 5 }); //cached response
```

The `ResultsFilter` delegate is executed with the context of the request before the request is made. Returning a value of type `TResponse` short-circuits the request and returns that response. Otherwise the request continues and its response passed into the `ResultsFilterResponse` delegate where it can be cached. 

#### New ServiceClient API's

The following new API's were added to all .NET Service Clients:

 - `SetCookie()` - Sets a Cookie on the clients `CookieContainer`
 - `GetCookieValues()` - Return all site Cookies in a string Dictionary
 - `CustomMethodAsync()` - Call any Custom HTTP Method Asynchronously

### Implicit Versioning

Similar to the behavior of `IHasSessionId` above, Service Clients that have specified a `Version` number, e.g:

```csharp
client.Version = 2;
```

Will populate that version number in all Request DTO's implementing `IHasVersion`, e.g:

```csharp
public class Hello : IReturn<HelloResponse>, IHasVersion {
    public int Version { get; set; }
    public string Name { get; set; }
}

client.Version = 2;
client.Get(new Hello { Name = "World" });  // Hello.Version=2
```

#### Version Abbreviation Convention

A popular convention for specifying versions in API requests is with the `?v=1` QueryString which ServiceStack now uses as a fallback for populating any Request DTO's that implement `IHasVersion` (as above).

> Note: as ServiceStack's message-based design promotes forward and backwards-compatible Service API designs, our recommendation is to only consider implementing versioning when necessary, at which point check out our [recommended versioning strategy](http://stackoverflow.com/a/12413091/85785).

## Cancellable Requests Feature

The new Cancellable Requests Feature makes it easy to design long-running Services that are cancellable with an external Web Service Request. To enable this feature, register the `CancellableRequestsFeature` plugin:

```csharp
Plugins.Add(new CancellableRequestsFeature());
```

### Designing a Cancellable Service

Then in your Service you can wrap your implementation within a disposable `ICancellableRequest` block which encapsulates a Cancellation Token that you can watch to determine if the Request has been cancelled, e.g: 

```csharp
public object Any(TestCancelRequest req)
{
    using (var cancellableRequest = base.Request.CreateCancellableRequest())
    {
        //Simulate long-running request
        while (true)
        {
            cancellableRequest.Token.ThrowIfCancellationRequested();
            Thread.Sleep(100);
        }
    }
}
```

### Cancelling a remote Service

To be able to cancel a Server request on the client, the client must first **Tag** the request which it does by assigning the `X-Tag` HTTP Header with a user-defined string in a Request Filter before calling a cancellable Service, e.g:

```csharp
var tag = Guid.NewGuid().ToString();
var client = new JsonServiceClient(baseUri) {
    RequestFilter = req => req.Headers[HttpHeaders.XTag] = tag
};

var responseTask = client.PostAsync(new TestCancelRequest());
```

Then at anytime whilst the Service is still executing the remote request can be cancelled by calling the `CancelRequest` Service with the specified **Tag**, e.g: 

```csharp
var cancelResponse = client.Post(new CancelRequest { Tag = tag });
```

If it was successfully cancelled it will return a `CancelRequestResponse` DTO with the elapsed time of how long the Service ran for. Otherwise if the remote Service had completed or never existed it will throw **404 Not Found** in a `WebServiceException`.

## Include Aggregates in AutoQuery

AutoQuery now supports running additional Aggregate queries on the queried result-set. 
To include aggregates in your Query's response specify them in the `Include` property of your AutoQuery Request DTO, e.g:

    var response = client.Get(new Query Rockstars { Include = "COUNT(*)" })

Or in the `Include` QueryString param if you're calling AutoQuery Services from a browser, e.g:

    /rockstars?include=COUNT(*)

The result is then published in the `QueryResponse<T>.Meta` String Dictionary and is accessible with:

    response.Meta["COUNT(*)"] //= 7

By default any of the functions in the SQL Aggregate whitelist can be referenced: 

    AVG, COUNT, FIRST, LAST, MAX, MIN, SUM

Which can be added to or removed from by modifying `SqlAggregateFunctions` collection, e.g, you can allow usage of a `CustomAggregate` SQL Function with:

    Plugins.Add(new AutoQueryFeature { 
        SqlAggregateFunctions = { "CustomAggregate" }
    })

### Aggregate Query Usage

The syntax for aggregate functions is modelled after their usage in SQL so they should be instantly familiar. 
At its most basic usage you can just specify the name of the aggregate function which will use `*` as a default argument so you can also query `COUNT(*)` with: 

    ?include=COUNT

It also supports SQL aliases:

    COUNT(*) Total
    COUNT(*) as Total

Which is used to change what key the result is saved into:

    response.Meta["Total"]

Columns can be referenced by name:

    COUNT(LivingStatus)

If an argument matches a column in the primary table the literal reference is used as-is, if it matches a column in a joined table it's replaced with its fully-qualified reference and when it doesn't match any column, Numbers are passed as-is otherwise its automatically escaped and quoted and passed in as a string literal.

The `DISTINCT` modifier can also be used, so a complex example looks like:

    COUNT(DISTINCT LivingStatus) as UniqueStatus

Which saves the result of the above function in:

    response.Meta["UniqueStatus"]

Any number of aggregate functions can be combined in a comma-delimited list:

    Count(*) Total, Min(Age), AVG(Age) AverageAge

Which returns results in:

    response.Meta["Total"]
    response.Meta["Min(Age)"]
    response.Meta["AverageAge"]

#### Aggregate Query Performance

Surprisingly AutoQuery is able to execute any number of Aggregate functions without performing any additional queries as previously to support paging, a `Total` needed to be executed for each AutoQuery. Now the `Total` query is combined with all other aggregate functions and executed in a single query.

### AutoQuery Response Filters

The Aggregate functions feature is built on the new `ResponseFilters` support in AutoQuery which provides a new extensibility option enabling customization and additional metadata to be attached to AutoQuery Responses. As the Aggregate Functions support is itself a Response Filter in can disabled by clearing them:

```csharp
Plugins.Add(new AutoQueryFeature {
    ResponseFilters = new List<Action<QueryFilterContext>>()
})
```

The Response Filters are executed after each AutoQuery and gets passed the full context of the executed query, i.e:

```csharp
class QueryFilterContext
{
    IDbConnection Db             // The ADO.NET DB Connection
    List<Command> Commands       // Tokenized list of commands
    IQuery Request               // The AutoQuery Request DTO
    ISqlExpression SqlExpression // The AutoQuery SqlExpression
    IQueryResponse Response      // The AutoQuery Response DTO
}
```

Where the `Commands` property contains the parsed list of commands from the `Include` property, tokenized into the structure below:

```csharp
class Command 
{
    string Name
    List<string> Args
    string Suffix
}
```

With this we could add basic calculator functionality to AutoQuery with the custom Response Filter below:

```csharp
Plugins.Add(new AutoQueryFeature {
    ResponseFilters = {
        ctx => {
            var supportedFns = new Dictionary<string, Func<int, int, int>>(StringComparer.OrdinalIgnoreCase)
            {
                {"ADD",      (a,b) => a + b },
                {"MULTIPLY", (a,b) => a * b },
                {"DIVIDE",   (a,b) => a / b },
                {"SUBTRACT", (a,b) => a - b },
            };
            var executedCmds = new List<Command>();
            foreach (var cmd in ctx.Commands)
            {
                Func<int, int, int> fn;
                if (!supportedFns.TryGetValue(cmd.Name, out fn)) continue;
                var label = !string.IsNullOrWhiteSpace(cmd.Suffix) ? cmd.Suffix.Trim() : cmd.ToString();
                ctx.Response.Meta[label] = fn(int.Parse(cmd.Args[0]), int.Parse(cmd.Args[1])).ToString();
                executedCmds.Add(cmd);
            }
            ctx.Commands.RemoveAll(executedCmds.Contains);
        }        
    }
})
```

Which now lets users perform multiple basic arithmetic operations with any AutoQuery request!

```csharp
var response = client.Get(new QueryRockstars {
    Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
});

response.Meta["ADD(6,2)"]      //= 8
response.Meta["SixTimesTwo"]   //= 12
response.Meta["Subtract(6,2)"] //= 4
response.Meta["TheDivide"]     //= 3
```

### Untyped SqlExpression

If you need to introspect or modify the executed `ISqlExpression`, it’s useful to access it as a `IUntypedSqlExpression` so its non-generic API's are still accessible without having to convert it back into its concrete generic `SqlExpression<T>` Type, e.g:

```csharp
IUntypedSqlExpression q = ctx.SqlExpression.GetUntypedSqlExpression()
    .Clone();
```

> Cloning the SqlExpression allows you to modify a copy that won't affect any other Response Filter.

### AutoQuery Property Mapping

AutoQuery can map `[DataMember]` property aliases on Request DTO's to the queried table, e.g:

```csharp
public class QueryPerson : QueryBase<Person>
{
    [DataMember("first_name")]
    public string FirstName { get; set; }
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

Which can be queried with:

    ?first_name=Jimi

or by setting the global `JsConfig.EmitLowercaseUnderscoreNames=true` convention:

```csharp
public class QueryPerson : QueryBase<Person>
{
    public string LastName { get; set; }
}
```

Where it's also queryable with:

    ?last_name=Hendrix

## OrmLite

### Dynamic Result Sets

There's new support for returning unstructured resultsets letting you Select `List<object>` instead of having results mapped to a concrete Poco class, e.g:

```csharp
db.Select<List<object>>(db.From<Poco>()
  .Select("COUNT(*), MIN(Id), MAX(Id)"))[0].PrintDump();
```

Output of objects in the returned `List<object>`:

    [
        10,
        1,
        10
    ]

You can also Select `Dictionary<string,object>` to return a dictionary of column names mapped with their values, e.g:

```csharp
db.Select<Dictionary<string,object>>(db.From<Poco>()
  .Select("COUNT(*) Total, MIN(Id) MinId, MAX(Id) MaxId"))[0].PrintDump();
```

Output of objects in the returned `Dictionary<string,object>`:

    {
        Total: 10,
        MinId: 1,
        MaxId: 10
    }

and can be used for API's returning a **Single** row result:

```csharp
db.Single<List<object>>(db.From<Poco>()
  .Select("COUNT(*) Total, MIN(Id) MinId, MAX(Id) MaxId")).PrintDump();
```

or use `object` to fetch an unknown **Scalar** value:

```csharp
object result = db.Scalar<object>(db.From<Poco>().Select(x => x.Id));
```

### New DB Parameters API's

To enable even finer-grained control of parameterized queries we've added new overloads that take a collection of IDbDataParameter's:

```csharp
List<T> Select<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T Single<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T Scalar<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> Column<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
IEnumerable<T> ColumnLazy<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
HashSet<T> ColumnDistinct<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
Dictionary<K, List<V>> Lookup<K, V>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> SqlList<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> SqlColumn<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T SqlScalar<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
```

> Including Async equivalents for each of the above Sync API's.

The new API's let you execute parameterized SQL with finer-grained control over the `IDbDataParameter` used, e.g:

```csharp
IDbDataParameter pAge = db.CreateParam("age", 40, dbType:DbType.Int16);
db.Select<Person>("SELECT * FROM Person WHERE Age > @pAge", new[] { pAge });
```

The new `CreateParam()` extension method above is a useful helper for creating custom IDbDataParameter's.

### Customize null values

The new `OrmLiteConfig.OnDbNullFilter` lets you to replace DBNull values with a custom value, so you could convert all `null` strings to be populated with `"NULL"` using:

```csharp
OrmLiteConfig.OnDbNullFilter = fieldDef => 
    fieldDef.FieldType == typeof(string)
        ? "NULL"
        : null;
```

### Case Insensitive References

If you're using a case-insensitive database you can tell OrmLite to match on case-insensitive POCO references with:

```csharp
OrmLiteConfig.IsCaseInsensitive = true;
```

### Enhanced CaptureSqlFilter

CaptureSqlFilter now tracks DB Parameters used in each query which can be used to quickly found out what SQL your DB calls generate by surrounding DB access in a using scope like:

```csharp
using (var captured = new CaptureSqlFilter())
using (var db = OpenDbConnection())
{
    db.Where<Person>(new { Age = 27 });

    captured.SqlCommandHistory[0].PrintDump();
}
```

Emits the Executed SQL along with any DB Parameters: 

    {
        Sql: "SELECT ""Id"", ""FirstName"", ""LastName"", ""Age"" FROM ""Person"" WHERE ""Age"" = @Age",
        Parameters: 
        {
            Age: 27
        }
    }

### Other OrmLite Features

 - New `IncludeFunctions = true` T4 Template configuration for generating Table Valued Functions added by [@mikepugh](https://github.com/mikepugh)
 - New `OrmLiteConfig.SanitizeFieldNameForParamNameFn` can be used to support sanitizing field names with non-ascii values into legal DB Param names

## Authentication

### Enable Session Ids on QueryString

Setting `Config.AllowSessionIdsInHttpParams=true` will allow clients to specify the `ss-id`, `ss-pid` Session Cookies on the QueryString or FormData. This is useful for getting Authenticated SSE Sessions working in IE9 which needs to rely on SSE Polyfills that's unable to send Cookies or Custom HTTP Headers.

The [SSE-polyfills Chat Demo](http://chat.servicestack.net/default_ieshim) has an example of adding the Current Session Id on the [JavaScript SSE EventSource Url](https://github.com/ServiceStackApps/Chat/blob/master/src/Chat/default_ieshim.cshtml#L93):

```js
var source = new EventSource('/event-stream?channels=@channels&ss-id=@(base.GetSession().Id)');
```

### In Process Authenticated Requests

You can enable the `CredentialsAuthProvider` to allow **In Process** requests to Authenticate without a Password with:

```csharp
new CredentialsAuthProvider {
    SkipPasswordVerificationForInProcessRequests = true,
}
```

When enabled this lets **In Process** Service Requests to login as a specified user without needing to provide their password. 

For example this could be used to create an [Intranet Restricted](https://github.com/ServiceStack/ServiceStack/wiki/Restricting-Services) **Admin-Only** Service that lets you login as another user so you can debug their account without knowing their password with:

```csharp
[RequiredRole("Admin")]
[Restrict(InternalOnly=true)]
public class ImpersonateUser 
{
    public string UserName { get; set; }
}

public object Any(ImpersonateUser request)
{
    using (var service = base.ResolveService<AuthenticateService>()) //In Process
    {
        return service.Post(new Authenticate {
            provider = AuthenticateService.CredentialsProvider,
            UserName = request.UserName,
        });
    }
}
```

> Your Services can use the new `Request.IsInProcessRequest()` to identify Services that were executed in-process.

### New CustomValidationFilter Filter

The new `CustomValidationFilter` is available on each `AuthProvider` and can be used to add custom validation logic where returning any non-null response will short-circuit the Auth Process and return the response to the client. 

The Validation Filter receives the full [AuthContext](https://github.com/ServiceStack/ServiceStack/blob/dd938c284ea509c4cdfab0e416c489aae7877981/src/ServiceStack/Auth/AuthProvider.cs#L415-L424) captured about the Authentication Request. 

So if you're under attack you could use this filter to Rick Roll North Korean hackers :)

```csharp
Plugins.Add(new AuthFeature(..., 
    new IAuthProvider[] {
        new CredentialsAuthProvider {
            CustomValidationFilter = authCtx => 
                authCtx.Request.UserHostAddress.StartsWith("175.45.17")
                    ? HttpResult.Redirect("https://youtu.be/dQw4w9WgXcQ")
                    : null
        }   
    }));
```

### UserName Validation

The UserName validation for all Auth Repositories have been consolidated in a central location, configurable at:

```csharp
Plugins.Add(new AuthFeature(...){
    ValidUserNameRegEx = new Regex(@"^(?=.{3,20}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled),
})
```

> Note: the default UserName RegEx above was increased from 15 chars limit to 20 chars

Instead of RegEx you can choose to validate using a Custom Predicate. The example below ensures UserNames don't include specific chars:

```csharp
Plugins.Add(new AuthFeature(...){
    IsValidUsernameFn = userName => userName.IndexOfAny(new[] { '@', '.', ' ' }) == -1
})
```

### Overridable Hash Provider

The `IHashProvider` used to generate and verify password hashes and salts in each UserAuth Repository is now overridable from its default with:

```csharp
container.Register<IHashProvider>(c => 
    new SaltedHash(HashAlgorithm:new SHA256Managed(), theSaltLength:4));
```

### Configurable Session Expiry

Permanent and Temporary Sessions can now be configured separately with different Session Expiries, configurable on either  `AuthFeature` or `SessionFeature` plugins, e.g:

```csharp
new AuthFeature(...) {
    SessionExpiry = TimeSpan.FromDays(7 * 2),
    PermanentSessionExpiry = TimeSpan.FromDays(7 * 4),
}
```

The above defaults configures Temporary Sessions to last a maximum of 2 weeks and Permanent Sessions lasting 4 weeks.

## Redis

 - New `PopItemsFromSet(setId, count)` API added by [@scottmcarthur](https://github.com/scottmcarthur)
 - The `SetEntry()` API's for setting string values in `IRedisClient` have been deprecated in favor of new `SetValue()` API's. 
 - Large performance improvement for saving large values (>8MB)  
 - Internal Buffer pool now configurable with `RedisConfig.BufferLength` and `RedisConfig.BufferPoolMaxSize`

## ServiceStack.Text

 - Use `JsConfig.ExcludeDefaultValues=true` to reduce payloads by omitting properties with default values
 - Text serializers now serialize any property or field annotated with the `[DataMember]` attribute, inc. private fields.
 - Updated `RecyclableMemoryStream` to the latest version, no longer throws an exception for disposing streams twice
 - Added support for serializing collections with base types

#### T[].NewArray() Usage

The `NewArray()` extension method reduces boilerplate required in modifying and returning an Array, it's useful when modifying configuration using fixed-size arrays, e.g:

```csharp
JsConfig.IgnoreAttributesNamed = JsConfig.IgnoreAttributesNamed.NewArray(
     with: typeof(ScriptIgnoreAttribute).Name,
  without: typeof(JsonIgnoreAttribute).Name);
```

Which is equivalent to the imperative alternative:

```csharp
var attrNames = new List<string>(JsConfig.IgnoreAttributesNamed) { 
    typeof(ScriptIgnoreAttribute).Name 
};
attrNames.Remove(typeof(JsonIgnoreAttribute).Name));
JsConfig.IgnoreAttributesNamed = attrNames.ToArray();
```

#### byte[].Combine() Usage

The `Combine(byte[]...)` extension method combines multiple byte arrays into a single byte[], e.g:

```csharp
byte[] bytes = "FOO".ToUtf8Bytes().Combine(" BAR".ToUtf8Bytes(), " BAZ".ToUtf8Bytes());
bytes.FromUtf8Bytes() //= FOO BAR BAZ
```

## Swagger

Swagger support has received a number of fixes and enhancements which now generates default params for DTO properties that aren't attributed with `[ApiMember]` attribute. Specifying a single `[ApiMember]` attribute reverts back to the existing behavior of only showing `[ApiMember]` DTO properties.

You can now Exclude **properties** from being listed in Swagger when using:

```csharp
[IgnoreDataMember]
```

Exclude **properties** from being listed in Swagger Schema Body with:

```csharp
[ApiMember(ExcludeInSchema=true)]
```

Or exclude entire Services from showing up in Swagger or any other Metadata Services (i.e. Metadata Pages, Postman, NativeTypes, etc) by annotating **Request DTO's** with:

```csharp
[Exclude(Feature.Metadata)]
```

## SOAP

There's finer-grain control available over which **Operations** and **Types** are exported in SOAP WSDL's and XSD's by overriding the new `ExportSoapOperationTypes()` and `ExportSoapType()` methods in your AppHost.

You can also exclude specific Request DTO's from being emitted in WSDL's and XSD's with:

```csharp
[Exclude(Feature.Soap)]
public class HiddenFromSoap { .. } 
```

You can also override and customize how the SOAP Message Responses are written, here's a basic example:

```csharp
public override WriteSoapMessage(Message message, Stream outputStream)
{
    using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
    {
        message.WriteMessage(writer);
    }
}
```

> The default [WriteSoapMessage](https://github.com/ServiceStack/ServiceStack/blob/fb08f5cb408ece66f203f677a4ec14ee9aad78ae/src/ServiceStack/ServiceStackHost.Runtime.cs#L484) implementation also raises a ServiceException and writes any returned response to a buffered Response Stream (if configured).

## Minor Enhancements

 - The Validation Features `AbstractValidator<T>` base class now implements `IRequiresRequest` and gets injected with the current Request
 - Response DTO is available at `IResponse.Dto`
 - Use `IResponse.ClearCookies()` to clear any cookies added during the Request pipeline before their written to the response
 - Use `[Exclude(Feature.Metadata)]` to hide Operations from being exposed in Metadata Services, inc. Metadata pages, Swagger, Postman, NativeTypes, etc
 - Use new `container.TryResolve(Type)` to resolve dependencies by runtime `Type`
 - New `debugEnabled` option added to `InMemoryLog` and `EventLogger`
 
### [Simple Customer REST Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/CustomerRestExample.cs)

New stand-alone [Customer REST Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/CustomerRestExample.cs) added showing an complete example of creating a Typed Client / Server REST Service with ServiceStack. Overview of this example was [answered on StackOverflow](http://stackoverflow.com/a/30273466/85785).

## Community 

 - [Jacob Foshee](https://twitter.com/82unpluggd) released a [custom short URLs ServiceStack Service](https://github.com/Durwella/UrlShortening) hosted on Azure.

## Breaking changes 

 - The provider ids for `InstagramOAuth2Provider` was renamed to `/auth/instagram` and `MicrosoftLiveOAuth2Provider` was renamed to `/auth/microsoftlive`.
 - All API's that return `HttpWebResponse` have been removed from `IServiceClient` and replaced with source-compatible extension methods 
 - `CryptUtils` have been deprecated and replaced with the more specific `RsaUtils`
 - `System.IO.Compression` functionality was removed from the PCL clients to workaround an [issue on Xamarin platforms](https://forums.servicestack.net/t/xamarin-ios-and-servicestack-dependency-issue/803/11)
 
All external NuGet packages were upgraded to their most recent major version (as done before every release).

# 4.0.40 Release Notes

## Native support for Java and Android Studio!

In our goal to provide a highly productive and versatile Web Services Framework that's ideal for services-heavy Mobile platforms, Service Oriented Architectures and Single Page Apps we're excited to announce new Native Types support for Java providing a terse and productive strong-typed Java API for the worlds most popular mobile platform - [Android](https://www.android.com/)!

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/android-studio-splash.png)

The new native Java types support for Android significantly enhances [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) support for mobile platforms to provide a productive dev workflow for mobile developers on the primary .NET, iOS and Java IDE's:

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/add-ss-reference-ides.png" align="right" />

#### [VS.NET integration with ServiceStackVS](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

Providing [C#](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference), [F#](https://github.com/ServiceStack/ServiceStack/wiki/FSharp-Add-ServiceStack-Reference), [VB.NET](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference) and [TypeScript](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference) Native Types support in Visual Studio for the [most popular platforms](https://github.com/ServiceStackApps/HelloMobile) including iOS and Android using [Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and [Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) on Windows.

#### [Xamarin Studio integration with ServiceStackXS](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio)

Providing [C# Native Types](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference) support for developing iOS and Android mobile Apps using [Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and [Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) with [Xamarin Studio](http://xamarin.com/studio) on OSX. The **ServiceStackXS** plugin also provides a rich web service development experience developing Client applications with [Mono Develop on Linux](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio-for-linux)

#### [Xcode integration with ServiceStackXC Plugin](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference)

Providing [Swift Native Types](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference) support for developing native iOS and OSX Applications with Xcode on OSX.

#### [Android Studio integration with ServiceStackIDEA](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

Providing Java Native Types support for developing pure cross-platform Java Clients or mobile Apps on the Android platform using Android Studio on both Windows and OSX.

## [ServiceStack IDEA Android Studio Plugin](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio)

Like the existing IDE integrations before it, the ServiceStack IDEA plugin provides Add ServiceStack Reference functionality to [Android Studio - the official Android IDE](https://developer.android.com/sdk/index.html). 

### Download and Install Plugin

The [ServiceStack AndroidStudio Plugin](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio) can be downloaded from the JetBrains plugins website at:

### [ServiceStackIDEA.zip](https://plugins.jetbrains.com/plugin/download?pr=androidstudio&updateId=19465)

After downloading the plugin above, install it in Android Studio by:

1. Click on `File -> Settings` in the Main Menu to open the **Settings Dialog**
2. Select **Plugins** settings screen
3. Click on **Install plugin from disk...** to open the **File Picker Dialog**
4. Browse and select the downloaded **ServiceStackIDEA.zip**
5. Click **OK** then Restart Android Studio

[![](https://github.com/ServiceStack/Assets/raw/34925d1b1b1b1856c451b0373139c939801d96ec/img/servicestackidea/android-plugin-install.gif)](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio)

### Java Add ServiceStack Reference

If you've previously used [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) in any of the supported IDE's before, you'll be instantly familiar with Add ServiceStack Reference in Android Studio. The only additional field is **Package**, required in order to comply with Java's class definition rules. 

To add a ServiceStack Reference, right-click (or press `Ctrl+Alt+Shift+R`) on the **Package folder** in your Java sources where you want to add the POJO DTO's. This will bring up the **New >** Items Context Menu where you can click on the **ServiceStack Reference...** Menu Item to open the **Add ServiceStack Reference** Dialog: 

![Add ServiceStack Reference Java Context Menu](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-context-menu.png)

The **Add ServiceStack Reference** Dialog will be partially populated with the selected **Package** from where the Dialog was launched from and the **File Name** defaulting to `dto.java` where the Plain Old Java Object (POJO) DTO's will be added to. All that's missing is the url of the remote ServiceStack instance you wish to generate the DTO's for, e.g: `http://techstacks.io`:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackidea/android-dialog.png)

Clicking **OK** will add the `dto.java` file to your project and modifies the current Project's **build.gradle** file dependencies list with the new **net.servicestack:android** dependency containing the Java JSON ServiceClients which is used together with the remote Servers DTO's to enable its typed Web Services API:

![](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-dialog-example.gif)

> As the Module's **build.gradle** file was modified you'll need to click on the **Sync Now** link in the top yellow banner to sync the **build.gradle** changes which will install or remove any modified dependencies.

### Java Update ServiceStack Reference

Like other Native Type languages, the generated DTO's can be further customized by modifying any of the options available in the header comments:

```
/* Options:
Date: 2015-04-17 15:16:08
Version: 1
BaseUrl: http://techstacks.io

Package: org.layoric.myapplication
GlobalNamespace: techstackdtos
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*
*/
...
```

For example the package name can be changed by uncommenting the **Package:** option with the new package name, then either right-click on the file to bring up the file context menu or use Android Studio's **Alt+Enter** keyboard shortcut then click on **Update ServiceStack Reference** to update the DTO's with any modified options:

![](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-update-example.gif)

### Java JsonServiceClient API
The goal of Native Types is to provide a productive end-to-end typed API to fascilitate consuming remote services with minimal effort, friction and cognitive overhead. One way we achieve this is by promoting a consistent, forwards and backwards-compatible message-based API that's works conceptually similar on every platform where each language consumes remote services by sending  **Typed DTO's** using a reusable **Generic Service Client** and a consistent client library API.

To maximize knowledge sharing between different platforms, the Java ServiceClient API is modelled after the [.NET Service Clients API](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) as closely as allowed within Java's language and idiomatic-style constraints. 

Thanks to C#/.NET being heavily inspired by Java, the resulting Java `JsonServiceClient` ends up bearing a close resemblance with .NET's Service Clients. The primary differences being due to language limitations like Java's generic type erasure and lack of language features like property initializers making Java slightly more verbose to work with, however as **Add ServiceStack Reference** is able to take advantage of code-gen we're able to mitigate most of these limitations to retain a familiar developer UX.

The [ServiceClient.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/ServiceClient.java) interface provides a good overview on the API available on the concrete [JsonServiceClient](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/JsonServiceClient.java) class:

```java
public interface ServiceClient {
    public <TResponse> TResponse get(IReturn<TResponse> request);
    public <TResponse> TResponse get(IReturn<TResponse> request, Map<String,String> queryParams);
    public <TResponse> TResponse get(String path, Class responseType);
    public <TResponse> TResponse get(String path, Type responseType);
    public HttpURLConnection get(String path);

    public <TResponse> TResponse post(IReturn<TResponse> request);
    public <TResponse> TResponse post(String path, Object request, Class responseCls);
    public <TResponse> TResponse post(String path, Object request, Type responseType);
    public <TResponse> TResponse post(String path, byte[] requestBody, String contentType, Class responseCls);
    public <TResponse> TResponse post(String path, byte[] requestBody, String contentType, Type responseType);
    public HttpURLConnection post(String path, byte[] requestBody, String contentType);

    public <TResponse> TResponse put(IReturn<TResponse> request);
    public <TResponse> TResponse put(String path, Object request, Class responseType);
    public <TResponse> TResponse put(String path, Object request, Type responseType);
    public <TResponse> TResponse put(String path, byte[] requestBody, String contentType, Class responseType);
    public <TResponse> TResponse put(String path, byte[] requestBody, String contentType, Type responseType);
    public HttpURLConnection put(String path, byte[] requestBody, String contentType);

    public <TResponse> TResponse delete(IReturn<TResponse> request);
    public <TResponse> TResponse delete(IReturn<TResponse> request, Map<String,String> queryParams);
    public <TResponse> TResponse delete(String path, Class responseType);
    public <TResponse> TResponse delete(String path, Type responseType);
    public HttpURLConnection delete(String path);
}
```

The primary concession is due to Java's generic type erasure which forces the addition overloads that include a `Class` parameter for specifying the response type to deserialize into as well as a `Type` parameter overload which does the same for generic types. These overloads aren't required for API's that accept a Request DTO annotated with `IReturn<T>` interface marker as we're able to encode the Response Type in code-generated Request DTO classes.

### Java JsonServiceClient Usage
To get started you'll just need an instance of `JsonServiceClient` initialized with the **BaseUrl** of the remote ServiceStack instance you want to access, e.g:

```java
JsonServiceClient client = new JsonServiceClient("http://techstacks.io");
```

> The JsonServiceClient is made available after the [net.servicestack:android](https://bintray.com/servicestack/maven/ServiceStack.Android/view) package is automatically added to your **build.gradle** when adding a ServiceStack reference.

Typical usage of the Service Client is the same in .NET where you just need to send a populated Request DTO and the Service Client will return a populated Response DTO, e.g:

```java
AppOverviewResponse r = client.get(new AppOverview());

ArrayList<Option> allTiers = r.getAllTiers();
ArrayList<TechnologyInfo> topTech = r.getTopTechnologies();
```

As Java doesn't have type inference you'll need to specify the Type when declaring a variable. Whilst the public instance fields of the Request and Response DTO's are accessible directly, the convention in Java is to use the **property getters and setters** that are automatically generated for each DTO property as seen above.

### Custom Example Usage

We'll now go through some of the other API's to give you a flavour of what's available. When preferred you can also consume Services using a custom route by supplying a string containing the route and/or Query String. As no type info is available you'll need to specify the Response DTO class to deserialize the response into, e.g:

```java
OverviewResponse response = client.get("/overview", OverviewResponse.class);
```

The path can either be a relative or absolute url in which case the **BaseUrl** is ignored and the full absolute url is used instead, e.g:

```java
OverviewResponse response = client.get("http://techstacks.io/overview", OverviewResponse.class);
```

When initializing the Request DTO you can take advantage of the generated setters which by default return `this` allowing them to be created and chained in a single expression, e.g:

```java
GetTechnology request = new GetTechnology()
	.setSlug("servicestack");

GetTechnologyResponse response = client.get(request);
```

### AutoQuery Example Usage

You can also send requests composed of both a Typed DTO and untyped String Dictionary by providing a Java Map of additional args. This is typically used when querying [implicit conventions in AutoQuery services](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#implicit-conventions), e.g:

```java
QueryResponse<Technology> response = client.get(new FindTechnologies(),
	Utils.createMap("DescriptionContains","framework"));
```

The `Utils.createMap()` API is included in the [Utils.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Utils.java) static class which contains a number of helpers to simplify common usage patterns and reduce the amount of boiler plate required for common tasks, e.g they can be used to simplify reading raw bytes or raw String from a HTTP Response. Here's how you can download an image bytes using a custom `JsonServiceClient` HTTP Request and load it into an Android Image `Bitmap`:

```java
HttpURLConnection httpRes = client.get("https://servicestack.net/img/logo.png");
byte[] imgBytes = Utils.readBytesToEnd(httpRes);
Bitmap img = BitmapFactory.decodeByteArray(imgBytes, 0, imgBytes.length);
```

### AndroidServiceClient
Unlike .NET, Java doesn't have an established Async story or any language support that simplifies execution and composition of Async tasks, as a result the Async story on Android is fairly fragmented with multiple options built-in for executing non-blocking tasks on different threads including:

 - Thread
 - Executor
 - HandlerThread
 - AsyncTask
 - Service
 - IntentService
 - AsyncQueryHandler
 - Loader

JayWay's Oredev presentation on [Efficient Android Threading](http://www.slideshare.net/andersgoransson/efficient-android-threading) provides a good overview of the different threading strategies above with their use-cases, features and pitfalls. Unfortunately none of the above options enable a Promise/Future-like API which would've been ideal in maintaining a consistent Task-based Async API across all ServiceStack Clients. Of all the above options the new Android [AsyncTask](http://developer.android.com/reference/android/os/AsyncTask.html) ended up the most suitable option, requiring the least effort for the typical Service Client use-case of executing non-blocking WebService Requests and having their results called back on the Main UI thread.

### AsyncResult
To enable an even simpler Async API decoupled from Android, we've introduced a higher-level [AsyncResult](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/AsyncResult.java) class which allows capturing of Async callbacks using an idiomatic anonymous Java class. `AsyncResult` is modelled after [jQuery.ajax](http://api.jquery.com/jquery.ajax/) and allows specifying **success()**, **error()** and **complete()** callbacks as needed.

### AsyncServiceClient API

Using AsyncResult lets us define a pure Java [AsyncServiceClient](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/AsyncServiceClient.java) interface that's decoupled from any specific threading implementation, i.e:

```java
public interface AsyncServiceClient {
    public <T> void getAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void getAsync(IReturn<T> request, final Map<String, String> queryParams, final AsyncResult<T> asyncResult);
    public <T> void getAsync(String path, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void getAsync(String path, final Type responseType, final AsyncResult<T> asyncResult);
    public void getAsync(String path, final AsyncResult<byte[]> asyncResult);

    public <T> void postAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final Object request, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final Object request, final Type responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final byte[] requestBody, final String contentType, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final byte[] requestBody, final String contentType, final Type responseType, final AsyncResult<T> asyncResult);
    public void postAsync(String path, final byte[] requestBody, final String contentType, final AsyncResult<byte[]> asyncResult);

    public <T> void putAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final Object request, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final Object request, final Type responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final byte[] requestBody, final String contentType, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final byte[] requestBody, final String contentType, final Type responseType, final AsyncResult<T> asyncResult);
    public void putAsync(String path, final byte[] requestBody, final String contentType, final AsyncResult<byte[]> asyncResult);

    public <T> void deleteAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(IReturn<T> request, final Map<String, String> queryParams, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(String path, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(String path, final Type responseType, final AsyncResult<T> asyncResult);
    public void deleteAsync(String path, final AsyncResult<byte[]> asyncResult);
}
```

The `AsyncServiceClient` interface is implemented by the `AndroidServiceClient` concrete class which behind-the-scenes uses an Android [AsyncTask](http://developer.android.com/reference/android/os/AsyncTask.html) to implement its Async API's. 

Whilst the `AndroidServiceClient` is contained in the **net.servicestack:android** dependency and only works in Android, the `JsonServiceClient` instead is contained in a seperate pure Java **net.servicestack:client** dependency which can be used independently to provide a typed Java API for consuming ServiceStack Services from any Java application.

### Async API Usage
To make use of Async API's in an Android App (which you'll want to do to keep web service requests off the Main UI thread), you'll instead need to use an instance of `AndroidServiceClient` which as it inherits `JsonServiceClient` can be used to perform both Sync and Async requests:
```java
AndroidServiceClient client = new AndroidServiceClient("http://techstacks.io");
```

Like other Service Clients, there's an equivalent Async API matching their Sync counterparts which differs by ending with an **Async** suffix that instead of returning a typed response, fires a **success(TResponse)** or **error(Exception)** callback with the typed response, e.g: 
```java
client.getAsync(new AppOverview(), new AsyncResult<AppOverviewResponse>(){
    @Override
    public void success(AppOverviewResponse r) {
        ArrayList<Option> allTiers = r.getAllTiers();
        ArrayList<TechnologyInfo> topTech = r.getTopTechnologies();
    }
});
```

Which just like the `JsonServiceClient` examples above also provide a number of flexible options to execute Custom Async Web Service Requests, e.g: 
```java
client.getAsync("/overview", OverviewResponse.class, new AsyncResult<OverviewResponse>(){
    @Override
    public void success(OverviewResponse response) {
    }
});
```

Example calling a Web Service with an absolute url:
```java
client.getAsync("http://techstacks.io/overview", OverviewResponse.class, new AsyncResult<OverviewResponse>() {
    @Override
    public void success(OverviewResponse response) {
    }
});
```

#### Async AutoQuery Example
Example calling an untyped AutoQuery Service with additional Dictionary String arguments:
```java
client.getAsync(request, Utils.createMap("DescriptionContains", "framework"),
    new AsyncResult<QueryResponse<Technology>>() {
        @Override
        public void success(QueryResponse<Technology> response) {
        }
    });
```

#### Download Raw Image Async Example
Example downloading raw Image bytes and loading it into an Android Image `Bitmap`:
```java
client.getAsync("https://servicestack.net/img/logo.png", new AsyncResult<byte[]>() {
    @Override
    public void success(byte[] imgBytes) {
        Bitmap img = BitmapFactory.decodeByteArray(imgBytes, 0, imgBytes.length);
    }
});
```

### Typed Error Handling
Thanks to Java also using typed Exceptions for error control flow, error handling in Java will be intantly familiar to C# devs which also throws a typed `WebServiceException` containing the remote servers structured error data:

```java
ThrowType request = new ThrowType()
    .setType("NotFound")
    .setMessage("not here");

try {
	ThrowTypeResponse response = testClient.post(request);
}
catch (WebServiceException webEx) {
    ResponseStatus status = thrownError.getResponseStatus();
	status.getMessage();    //= not here
    status.getStackTrace(); //= (Server StackTrace)
}
```

Likewise structured Validation Field Errors are also accessible from the familar `ResponseStatus` DTO, e.g:
```java
ThrowValidation request = new ThrowValidation()
    .setEmail("invalidemail");

try {
    client.post(request);
} catch (WebServiceException webEx){
    ResponseStatus status = webEx.getResponseStatus();

    ResponseError firstError = status.getErrors().get(0);
    firstError.getErrorCode(); //= InclusiveBetween
    firstError.getMessage();   //= 'Age' must be between 1 and 120. You entered 0.
    firstError.getFieldName(); //= Age
}
```

#### Async Error Handling
Async Error handling differs where in order to access the `WebServiceException` you'll need to implement the **error(Exception)** callback, e.g:
```java
client.postAsync(request, new AsyncResult<ThrowTypeResponse>() {
    @Override
    public void error(Exception ex) {
        WebServiceException webEx = (WebServiceException)ex;
        
        ResponseStatus status = thrownError.getResponseStatus();
        status.getMessage();    //= not here
        status.getStackTrace(); //= (Server StackTrace)
    }
});
```

Async Validation Errors are also handled in the same way: 
```java
client.postAsync(request, new AsyncResult<ThrowValidationResponse>() {
    @Override
    public void error(Exception ex) {
        WebServiceException webEx = (WebServiceException)ex;
        ResponseStatus status = webEx.getResponseStatus();

        ResponseError firstError = status.getErrors().get(0);
        firstError.getErrorCode(); //= InclusiveBetween
        firstError.getMessage();   //= 'Age' must be between 1 and 120. You entered 0.
        firstError.getFieldName(); //= Age
    }
}
```

### JsonServiceClient Error Handlers
To make it easier to generically handle Web Service Exceptions, the Java Service Clients also support static Global Exception handlers by assigning `AndroidServiceClient.GlobalExceptionFilter`, e.g:
```java
AndroidServiceClient.GlobalExceptionFilter = new ExceptionFilter() {
    @Override
    public void exec(HttpURLConnection res, Exception ex) {
    	//...
    }
};
```

As well as local Exception Filters by specifying a handler for `client.ExceptionFilter`, e.g:
```java
client.ExceptionFilter = new ExceptionFilter() {
    @Override
    public void exec(HttpURLConnection res, Exception ex) {
    	//...
    }
};
```

## Introducing [TechStacks Android App](https://github.com/ServiceStackApps/TechStacksAndroidApp)
To demonstrate Java Native Types in action we've ported the Swift [TechStacks iOS App](https://github.com/ServiceStackApps/TechStacksApp) to a native Java Android App to showcase the responsiveness and easy-of-use of leveraging Java Add ServiceStack Reference in Android Projects. 

The Android TechStacks App can be [downloaded for free from the Google Play Store](https://play.google.com/store/apps/details?id=servicestack.net.techstacks):

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-android-app.jpg)](https://play.google.com/store/apps/details?id=servicestack.net.techstacks)

### Data Binding
As there's no formal data-binding solution in Android we've adopted a lightweight iOS-inspired [Key-Value-Observable-like data-binding solution](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference#observing-data-changes) in Android TechStacks in order to maximize knowledge-sharing and ease porting between native Swift iOS and Java Android Apps. 

Similar to the Swift TechStacks iOS App, all web service requests are encapsulated in a single [App.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/techstacks/src/main/java/servicestack/net/techstacks/App.java) class and utilizes Async Service Client API's in order to maintain a non-blocking and responsive UI. 

### Registering for Data Updates
In iOS, UI Controllers register for UI and data updates by implementing `*DataSource` and `*ViewDelegate` protocols, following a similar approach, Android Activities and Fragments register for Async Data callbacks by implementing the Custom interface `AppDataListener` below:

```java
public static interface AppDataListener
{
    public void onUpdate(AppData data, DataType dataType);
}
```

Where Activities or Fragments can then register itself as a listener when they're first created:
```java
@Override
public void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    App.getData().addListener(this);
}
```

### Data Binding Async Service Responses
Then in `onCreateView` MainActivity calls the `AppData` singleton to fire off all async requests required to populate it's UI:
```java
public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle state) {
    App.getData().loadAppOverview();
    ...
}
```

Where `loadAppOverview()` makes an async call to the `AppOverview` Service, storing the result in an AppData instance variable before notifying all registered listeners that `DataType.AppOverview` has been updated:
```java
public AppData loadAppOverview(){
    client.getAsync(new AppOverview(), new AsyncResult<AppOverviewResponse>() {
        @Override
        public void success(AppOverviewResponse response){
            appOverviewResponse = response;
            onUpdate(DataType.AppOverview);
        }
    });
    return this;
}
```
> Returning `this` allows expression chaining, reducing the boilerplate required to fire off multiple requests

Calling `onUpdate()` simply invokes the list of registered listeners with itself and the  enum DataType of what was changed, i.e:
```java
public void onUpdate(DataType dataType){
    for (AppDataListener listener : listeners){
        listener.onUpdate(this, dataType);
    }
}
```

The Activity can then update its UI within the `onUpdate()` callback by re-binding its UI Controls when relevant data has changed, in this case when `AppOverview` response has returned:
```java
@Override
public void onUpdate(App.AppData data, App.DataType dataType) {
    switch (dataType) {
        case AppOverview:
            Spinner spinner = (Spinner)getActivity().findViewById(R.id.spinnerCategory);
            ArrayList<String> categories = map(data.getAppOverviewResponse().getAllTiers(), 
                new Function<Option, String>() {
                    @Override public String apply(Option option) {
                        return option.getTitle();
                    }
                });
            spinner.setAdapter(new ArrayAdapter<>(getActivity(),
                android.R.layout.simple_spinner_item, categories));

            ListView list = (ListView)getActivity().findViewById(R.id.listTopRated);
            ArrayList<String> topTechnologyNames = map(getTopTechnologies(data),
                new Function<TechnologyInfo, String>() {
                    @Override public String apply(TechnologyInfo technologyInfo) {
                        return technologyInfo.getName() + " (" + technologyInfo.getStacksCount() + ")";
                    }
                });
            list.setAdapter(new ArrayAdapter<>(getActivity(),
                android.R.layout.simple_list_item_1, topTechnologyNames));
            break;
    }
}
```

In this case the `MainActivity` home screen re-populates the Technology Category **Spinner** (aka Picker) and the Top Technologies **ListView** controls by assigning a new Android `ArrayAdapter`. 

### Functional Java Utils
The above example also introduces the `map()` functional util we've also included in the **net.servicestack:client** dependency to allow usage of Functional Programming techniques to transform, query and filter data given Android's Java 7 lack of any language or library support for Functional Programming itself. Unfortunately lack of closures in Java forces more boilerplate than otherwise would be necessary as it needs to fallback to use anonymous Type classes to capture delegates. Android Studio also recognizes this pattern as unnecessary noise and will automatically collapse the code into a readable closure syntax, with what the code would've looked like had Java supported closures, e.g:

![Android Studio Collapsed Closure](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/androidstudio-collapse-closure.png)

### [Func.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Func.java) API

The [Func.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Func.java) static class contains a number of common functional API's providing a cleaner and more robust alternative to working with Data than equivalent imperative code. We can take advantage of **static imports** in Java to import the namespace of all utils with the single import statement below:
```java
import static net.servicestack.client.Func.*;
```

Which will let you reference all the Functional utils below without a Class prefix:
```java
ArrayList<R> map(Iterable<T> xs, Function<T,R> f)
ArrayList<T> filter(Iterable<T> xs, Predicate<T> predicate)
void each(Iterable<T> xs, Each<T> f)
T first(Iterable<T> xs)
T first(Iterable<T> xs, Predicate<T> predicate)
T last(Iterable<T> xs)
T last(Iterable<T> xs, Predicate<T> predicate)
boolean contains(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> skip(Iterable<T> xs, int skip)
ArrayList<T> skip(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> take(Iterable<T> xs, int take)
ArrayList<T> take(Iterable<T> xs, Predicate<T> predicate)
boolean any(Iterable<T> xs, Predicate<T> predicate)
boolean all(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> expand(Iterable<T>... xss)
T elementAt(Iterable<T> xs, int index)
ArrayList<T> reverse(Iterable<T> xs)
reduce(Iterable<T> xs, E initialValue, Reducer<T,E> reducer)
E reduceRight(Iterable<T> xs, E initialValue, Reducer<T,E> reducer)
String join(Iterable<T> xs, String separator)
ArrayList<T> toList(Iterable<T> xs)
```

### Images and Custom Binary Requests
The TechStacks Android App also takes advantage of the Custom Service Client API's to download images asynchronously. As images can be fairly resource and bandwidth intensive they're stored in a simple Dictionary Cache to minimize any unnecessary CPU and network resources, i.e:
```java
HashMap<String,Bitmap> imgCache = new HashMap<>();
public void loadImage(final String imgUrl, final ImageResult callback) {
    Bitmap img = imgCache.get(imgUrl);
    if (img != null){
        callback.success(img);
        return;
    }

    client.getAsync(imgUrl, new AsyncResult<byte[]>() {
        @Override
        public void success(byte[] imgBytes) {
            Bitmap img = AndroidUtils.readBitmap(imgBytes);
            imgCache.put(imgUrl, img);
            callback.success(img);
        }
    });
}
```

The TechStacks App uses the above API to download screenshots and load their Bitmaps in `ImageView` UI Controls, e.g:

```java
String imgUrl = result.getScreenshotUrl();
final ImageView img = (ImageView)findViewById(R.id.imgTechStackScreenshotUrl);
data.loadImage(imgUrl, new App.ImageResult() {
    @Override public void success(Bitmap response) {
        img.setImageBitmap(response);
    }
});
```

## Java generated DTO Types

Our goal with **Java Add ServiceStack Reference** is to ensure a high-fidelity, idiomatic translation within the constraints of Java language and its built-in libraries, where .NET Server DTO's are translated into clean, conventional Java POJO's where .NET built-in Value Types mapped to their equivalent Java data Type.

To see what this ends up looking up we'll go through some of the [Generated Test Services](http://test.servicestack.net/types/java) to see how they're translated in Java.

### .NET Attributes translated into Java Annotations
By inspecting the `HelloAllTypes` Request DTO we can see that C# Metadata Attributes e.g. `[Route("/all-types")]` are also translated into the typed Java Annotations defined in the **net.servicestack:client** dependency. But as Java only supports defining a single Annotation of the same type, any subsequent .NET Attributes of the same type are emitted in comments.

### Terse, typed API's with IReturn interfaces
Java Request DTO's are also able to take advantage of the `IReturn<TResponse>` interface marker to provide its terse, typed generic API but due to Java's Type erasure the Response Type also needs to be encoded in the Request DTO as seen by the `responseType` field and `getResponseType()` getter:

```java
@Route("/all-types")
public static class HelloAllTypes implements IReturn<HelloAllTypesResponse>
{
    public String name = null;
    public AllTypes allTypes = null;
    
    public String getName() { return name; }
    public HelloAllTypes setName(String value) { this.name = value; return this; }
    public AllTypes getAllTypes() { return allTypes; }
    public HelloAllTypes setAllTypes(AllTypes value) { this.allTypes = value; return this; }

    private static Object responseType = HelloAllTypesResponse.class;
    public Object getResponseType() { return responseType; }
}
```

### Getters and Setters generated for each property
Another noticable feature is the Java getters and setters property convention are generated for each public field with setters returning itself allowing for multiple setters to be chained within a single expression. 

To comply with Gson JSON Serialization rules, the public DTO fields are emitted in the same JSON naming convention as the remote ServiceStack server which for the [test.servicestack.net](http://test.servicestack.net) Web Services, follows its **camelCase** naming convention that is configured in its AppHost with: 
```csharp
JsConfig.EmitCamelCaseNames = true;
```

Whilst the public fields match the remote server JSON naming convention, the getters and setters are always emitted in Java's **camelCase** convention to maintain a consistent API irrespective of the remote server configuration. To minimize API breakage they should be the preferred method to access DTO fields.

### Java Type Converions
By inspecting the `AllTypes` DTO fields we can see what Java Type each built-in .NET Type gets translated into. In each case it selects the most suitable concrete Java datatype available, inc. generic collections. We also see only reference types are used (i.e. instead of their primitive types equivalents) since DTO properties are optional and need to be nullable. 
```java
public static class AllTypes
{
    public Integer id = null;
    public Integer nullableId = null;
    @SerializedName("byte") public Short Byte = null;
    @SerializedName("short") public Short Short = null;
    @SerializedName("int") public Integer Int = null;
    @SerializedName("long") public Long Long = null;
    public Integer uShort = null;
    public Long uInt = null;
    public BigInteger uLong = null;
    @SerializedName("float") public Float Float = null;
    @SerializedName("double") public Double Double = null;
    public BigDecimal decimal = null;
    public String string = null;
    public Date dateTime = null;
    public TimeSpan timeSpan = null;
    public Date dateTimeOffset = null;
    public UUID guid = null;
    @SerializedName("char") public String Char = null;
    public Date nullableDateTime = null;
    public TimeSpan nullableTimeSpan = null;
    public ArrayList<String> stringList = null;
    public ArrayList<String> stringArray = null;
    public HashMap<String,String> stringMap = null;
    public HashMap<Integer,String> intStringMap = null;
    public SubType subType = null;
    ...
}
```

The only built-in Value Type that didn't have a suitable built-in Java equivalent was `TimeSpan`. In this case it uses our new [TimeSpan.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/TimeSpan.java) class which implements the same familiar API available in .NET's `TimeSpan`. 

Something else you'll notice is that some fields are annotated with the `@SerializedName()` Gson annotation. This is automatically added for Java keywords - required since Java doesn't provide anyway to escape keyword identifiers. The first time a Gson annotation is referenced it also automatically includes the required Gson namespace imports. If needed, this can also be explicitly added by with:
```java
JavaGenerator.AddGsonImport = true;
```

### Java Enums
.NET enums are also translated into typed Java enums where basic enums end up as a straight forward transaltion, e.g:
```java
public static enum BasicEnum
{
    Foo,
    Bar,
    Baz;
}
```

Whilst as Java doesn't support integer Enum flags directly the resulting translation ends up being a bit more convoluted:
```java
@Flags()
public static enum EnumFlags
{
    @SerializedName("1") Value1(1),
    @SerializedName("2") Value2(2),
    @SerializedName("4") Value3(4);

    private final int value;
    EnumFlags(final int intValue) { value = intValue; }
    public int getValue() { return value; }
}
```

## Java Native Types Customization
The header comments in the generated DTO's allows for further customization of how the DTO's are generated which can then be updated with any custom Options provided using the **Update ServiceStack Reference** Menu Item in Android Studio. Options that are preceded by a single line Java comment `//` are defaults from the server which can be overridden.

To override a value, remove the `//` and specify the value to the right of the `:`. Any value uncommented will be sent to the server to override any server defaults.
```java
/* Options:
Date: 2015-04-10 12:41:14
Version: 1
BaseUrl: http://techstacks.io

Package: net.servicestack.techstacks
//GlobalNamespace: dto
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*
*/
```
We'll go through and cover each of the above options to see how they affect the generated DTO's:

### Package
Specify the package name that the generated DTO's are in:
```
Package: net.servicestack.techstacks
```
Will generate the package name for the generated DTO's as:
```java
package net.servicestack.techstacks;
```

### GlobalNamespace
Change the name of the top-level Java class containar that all static POJO classes are generated in, e.g changing the `GlobalNamespace` to:
```
GlobalNamespace: techstacksdto
```
Will change the name of the top-level class to `techstacksdto`, e.g:
```java
public class techstacksdto
{
    ...
}
```
Where all static DTO classes can be imported using the wildcard import below:
```java
import net.servicestack.techstacksdto.*;
```

### AddPropertyAccessors
By default **getters** and **setters** are generated for each DTO property, you can prevent this default with:
```
AddPropertyAccessors: false
```
Which will no longer generate any property accessors, leaving just public fields, e.g:
```java
public static class AppOverviewResponse
{
    public Date Created = null;
    public ArrayList<Option> AllTiers = null;
    public ArrayList<TechnologyInfo> TopTechnologies = null;
    public ResponseStatus ResponseStatus = null;
}
```

### SettersReturnThis
To allow for chaining DTO field **setters** returns itself by default, this can be changed to return `void` with:
```
SettersReturnThis: false
```
Which will change the return type of each setter to `void`:
```java
public static class GetTechnology implements IReturn<GetTechnologyResponse>
{
    public String Slug = null;
    
    public String getSlug() { return Slug; }
    public void setSlug(String value) { this.Slug = value; }
}
```

### AddServiceStackTypes
Lets you exclude built-in ServiceStack Types and DTO's from being generated with:
```
AddServiceStackTypes: false
```
This will prevent Request DTO's for built-in ServiceStack Services like `Authenticate` from being emitted.

### AddImplicitVersion
Lets you specify the Version number to be automatically populated in all Request DTO's sent from the client:
```
AddImplicitVersion: 1
```
Which will embed the specified Version number in each Request DTO, e.g:
```java
public static class GetTechnology implements IReturn<GetTechnologyResponse>
{
    public Integer Version = 1;
    public Integer getVersion() { return Version; }
    public GetTechnology setVersion(Integer value) { this.Version = value; return this; }
}
```
This lets you know what Version of the Service Contract that existing clients are using making it easy to implement [ServiceStack's recommended versioning strategy](http://stackoverflow.com/a/12413091/85785).

### IncludeTypes
Is used as a Whitelist that can be used to specify only the types you would like to have code-generated:
```
/* Options:
IncludeTypes: GetTechnology,GetTechnologyResponse
```
Will only generate `GetTechnology` and `GetTechnologyResponse` DTO's, e.g:
```java
public class dto
{
    public static class GetTechnologyResponse { ... }
    public static class GetTechnology implements IReturn<GetTechnologyResponse> { ... }
}
```

### ExcludeTypes
Is used as a Blacklist where you can specify which types you would like to exclude from being generated:
```
/* Options:
ExcludeTypes: GetTechnology,GetTechnologyResponse
```
Will exclude `GetTechnology` and `GetTechnologyResponse` DTO's from being generated.

### DefaultImports
Lets you override the default import packages included in the generated DTO's:
```
java.math.*,java.util.*,net.servicestack.client.*,com.acme.custom.*
```
Will override the default imports with the ones specified, i.e: 
```java
import java.math.*;
import java.util.*;
import net.servicestack.client.*;
import com.acme.custom.*;
```

By default the generated DTO's do not require any Google's Gson-specific serialization hints, but when they're needed e.g. if your DTO's use Java keywords or are attributed with `[DataMember(Name=...)]` the required Gson imports are automatically added which can also be added explicitly with:
```csharp
JavaGenerator.AddGsonImport = true;
```
Which will add the following Gson imports:
```java
import com.google.gson.annotations.*;
import com.google.gson.reflect.*;
```

## ServiceStack Customer Forums moved to Discourse
The ServiceStack Customer Forums have been moved from **Google+** over to [Discourse](http://discourse.org/) which provides better readability, richer markup, support for code samples, better searching and discoverability, etc - basically an overall better option for providing support than Google+ was. The new Customer Forums is available at: 
### https://forums.servicestack.net

ServiceStack Customers will be able to register as a new user by using the same email that's registered in your ServiceStack account or added as a support contact at: http://servicestack.net/account/support

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestack-forums.jpg)](https://forums.servicestack.net/)

## Swift Native Types upgraded to Swift 1.2
The latest stable release of **Xcode 6.3** includes the new Swift 1.2 release that had a number of breaking language changes from the previous version. In this release both the Swift generated types in ServiceStack and the [JsonServiceClient.swift](https://github.com/ServiceStack/ServiceStack.Swift/blob/master/dist/JsonServiceClient.swift) client library have been upgraded to support Swift 1.2 language changes.

Whilst the latest version of Swift fixed a number of stability issues, it also introduced some regressions. Unfortunately one of these regressions affected extensions on generic types that also have `typealias` like what's used when generating generic type responses like the `QueryResponse<T>` that's used in AutoQuery Services. We've submitted a failing test case for this issue with Apple and hopefully it will get resolved in a future release of Swift.

## OrmLite

### Merge Disconnected POCO Data Sets
The new `Merge` extension method can stitch disconnected POCO collections together as per their relationships defined in [OrmLite's POCO References](https://github.com/ServiceStack/ServiceStack.OrmLite#reference-support-poco-style).

For example you can select a collection of Customers who've made an order with quantities of 10 or more and in a separate query select their filtered Orders and then merge the results of these 2 distinct queries together with:
```csharp
//Select Customers who've had orders with Quantities of 10 or more
List<Customer> customers = db.Select<Customer>(q =>
    q.Join<Order>()
     .Where<Order>(o => o.Qty >= 10)
     .SelectDistinct());

//Select Orders with Quantities of 10 or more
List<Order> orders = db.Select<Order>(o => o.Qty >= 10);

customers.Merge(orders); // Merge disconnected Orders with their related Customers

customers.PrintDump();   // Print merged customers and orders datasets
```

### New Multiple Select API's
Add new multi `Select<T1,T2>` and `Select<T1,T2,T3>` select overloads to allow selecting fields from multiple tables, e.g:
```csharp
var q = db.From<FooBar>()
    .Join<BarJoin>()
    .Select<FooBar, BarJoin>((f, b) => new { f.Id, b.Name });

Dictionary<int,string> results = db.Dictionary<int, string>(q);
```

### New OrmLite Naming Strategy
The new `LowercaseUnderscoreNamingStrategy` can be enabled with:
```csharp
OrmLiteConfig.DialectProvider.NamingStrategy = new LowercaseUnderscoreNamingStrategy();
```
### New Signed MySql NuGet Package
Add new **OrmLite.MySql.Signed** NuGet package containing signed MySql versions of .NET 4.0 and .NET 4.5 builds of MySql

## ServiceStack Changes
This release also saw a number of minor changes and enhancements added throughout the ServiceStack Framework libraries which are listed below, grouped under their related sections:

### [ServerEvents](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)
- ServerEvents Heartbeat is now disabled when underlying `$.ss.eventSource` EventSource is closed 
- `ServerEventFeature.OnCreated` callback can now be used to short-circuit ServerEvent connections with `httpReq.EndResponse()`
- Dropped connections are automatically restarted in C#/.NET `ServerEventsClient`
- Added new `IEventSubscription.UserAddress` field containing IP Address of ServerEvents Client
- ServerEvent requests are now verified that they're made from the original IP Address and returns `403 Forbidden` when invalid. IP Address Validation can be disabled with: `ServerEventsFeature.ValidateUserAddress = false`

#### New Session and Auth API's
- New `FourSquareOAuth2Provider` added by [@kevinhoward](https://github.com/kevinhoward)
- New `IResponse.DeleteSessionCookies()` extension method can be used delete existing Session Cookies
- New `ISession.Remove(key)` and `ISession.RemoveAll()` API's added on `ISession` Bag
- Implemented `IRemoveByPattern.RemoveByPattern(pattern)` on `OrmLiteCacheClient`
- Added new `IUserAuthRepository.DeleteUserAuth()` API and corresponding implementations in all **AuthRepository** providers
- A new .NET 4.5 release of `RavenDbUserAuthRepository` using the latest Raven DB Client libraries is available in the **ServiceStack.Authentication.RavenDb** NuGet package - added by [@kevinhoward](https://github.com/kevinhoward)

#### Session and Auth Changes
- Session Cookie Identifiers are now automatically deleted on Logout (i.e. `/auth/logout`). Can be disabled with `AuthFeature.DeleteSessionCookiesOnLogout = false`
- Auth now uses `SetParam` instead of `AddParam` to override existing QueryString variables in Redirect Urls (i.e. instead of appending to them to the end of the urls)
- Added new `AppHost.TestMode` to allow functionality during testing that's disabled in release mode. For example registering a `AuthUserSession` in the IOC is now disabled by default (as it's only hydrated from Cache not IOC). Can be enabled to simplify testing with `AppHost.TestMode = true`.

### New Generic Logger implementation 
- A new `GenericLogFactory` and `GenericLogger` implementations were added to simplify creation of new `ILog` providers. For example you can create and register a new custom logging implementation to redirect logging to an Xamarin.Android UI Label control with:

#### Android UI Logger Example
```csharp
LogManager.LogFactory = new GenericLogFactory(message => {
    RunOnUiThread(() => {
        lblResults.Text = "{0}  {1}\n".Fmt(DateTime.Now.ToLongTimeString(), message) + lblResults.Text;
    });
});
```

### New WebService Framework API's
- All magic keyword constants used within ServiceStack can now be overridden by reassinging them in the new static `Keywords` class
- Added new `IResponse.Request` property allowing access to `IRequest` from all `IResponse` instances
- Added new `HttpError.Forbidden(message)` convenience method
- Added new virtual `AppHost.ExecuteMessage(IMessage)` API's to be able to override default MQ ExecuteMessage impl
- Added explicit `IVirtual.Referesh()` API to force refresh of underlying `FileInfo` stats
- Added new `Xamarin.Mac20` NuGet profile to support **Xamarin.Mac Unified API** Projects 

### WebService Framework Changes
- Improve performance of processing HTTP Partial responses by using a larger and reusable `byte[]` buffer. The size of buffer used can be customized with: `HttpResultUtils.PartialBufferSize = 32 * 1024`
- Service `IDisposable` dependencies are now immediately released after execution
- Added support for case-insensitive Content-Type's 

### [Auto Batched Requests](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Batched-Requests)
- Added support for Async API's in Auto-Batched Requests
- A new `X-AutoBatch-Completed` HTTP Response Header is added to all Auto-Batched HTTP Responses containing number of individual requests completed

### [Metadata](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page)
- Added `[Restrict(VisibilityTo = RequestAttributes.None)]` to Postman and Swagger Request DTO's to hide their routes from appearing on metadata pages
- `PreRequestFilters` are now executed in Metadata Page Handlers 

### [Mini Profiler](https://github.com/ServiceStack/ServiceStack/wiki/Built-in-profiling)
- Added support of **Async OrmLite requests** in MiniProfiler
- The Values of Parameterized queries are now shown in MiniProfiler

### [AppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings)
- Added new `AppSettingsBase.Get<T>(name)` API to all [AppSettings providers](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings)

### [Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing)
- Weighting of Routes can now be customized with the new `RestPath.CalculateMatchScore` delegate

### [Swagger API](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)
- Updated to the latest [Swagger API](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)

## ServiceStack.Redis
New Redis Client API's added by [@andyberryman](https://github.com/andyberryman):
```csharp
public interface IRedisClient
{
    ...
    long StoreIntersectFromSortedSets(string intoSetId, string[] setIds, string[] args);
    long StoreUnionFromSortedSets(string intoSetId, string[] setIds, string[] args);
}

public interface IRedisTypedClient<T> 
{
    ...
    long StoreIntersectFromSortedSets(IRedisSortedSet<T> setId, IRedisSortedSet<T>[] setIds, string[] args);
    long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args);
}
```

Added support for `Dictionary<string,string>` API's in `IRedisQueueableOperation` which now allows execution of Dictionary API's in Redis Transactions, e.g"
```csharp
using (var trans = Redis.CreateTransaction()) 
{
    trans.QueueCommand(r => r.GetAllEntriesFromHash(HashKey), x => results = x);
    trans.Commit();
}
```

## ServiceStack.Text
 - JSON Support for `IEnumerable` with mixed types added by [@bcuff](https://github.com/bcuff)
 - Added new `string.SetQueryParam()` and `string.SetHashParam()` HTTP Utils API's 
 - Add range check for inferring valid JavaScript numbers with `JsonObject`

## [Stripe](https://github.com/ServiceStack/Stripe)
The Stripe Gateway also received updates thanks to [@jpasichnyk](https://github.com/jpasichnyk):

 - Added new `Send()` and `Post()` overloads that accepts Stripe's optional `Idempotency-Key` HTTP Header to prevent duplicate processing of resent requests
 - Added new `Type` property in `StripeError` error responses

# v4.0.38 Release Notes

## Native Support for Swift!

We're happy to announce an exciting new addition to [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) with support for Apple's new [Swift Programming Language](https://developer.apple.com/swift/) - providing the most productive way for consuming web services on the worlds most desirable platform!

![Swift iOS, XCode and OSX Banner](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/swift-logo-banner.jpg)

Native Swift support adds compelling value to your existing ServiceStack Services providing an idiomatic and end-to-end typed Swift API that can be effortlessly consumed from iOS and OSX Desktop Apps.

### ServiceStack XCode Plugin

To further maximize productivity we've integrated with XCode IDE to allow iOS and OSX developers to import your typed Services API directly into their XCode projects with the ServiceStack XCode plugin below:

[![ServiceStackXCode.dmg download](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-dmg.png)](https://github.com/ServiceStack/ServiceStack.Swift/raw/master/dist/ServiceStackXcode.dmg)

The ServiceStack XCode Plugin can be installed by dragging it to the XCode Plugins directory:

![ServiceStackXCode.dmg Installer](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-installer.png)

Once installed the plugin adds a couple new familiar Menu options to the XCode Menu:

### Swift Add ServiceStack Reference

![XCode Add Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-add-reference.png)

Use the **Add ServiceStack Reference** Menu option to bring up the Add Reference XCode UI Sheet, which just like the Popup Window in VS.NET just needs the Url for your remote ServiceStack instance and the name of the file the generated Swift DTO's should be saved to:

![XCode Add Reference Sheet](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-add-reference-sheet.png)

Clicking **Add Reference** adds 2 files to your XCode project:

 - `JsonServiceClient.swift` - A Swift JSON ServiceClient with API's based on that of [the .NET JsonServiceClient](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client)
 - `{FileName}.dtos.swift` - Your Services DTO Types converted in Swift

You can also customize how the Swift types are generated by uncommenting the desired option with the behavior you want, e.g. to enable [Key-Value Observing (KVO)](https://developer.apple.com/library/ios/documentation/Cocoa/Conceptual/KeyValueObserving/KeyValueObserving.html) in the generated DTO models, uncomment `BaseClass: NSObject` and then click the **Update ServiceStack Reference** Main Menu item to fetch the latest DTO's with all Types inheriting from `NSObject` as seen below:

![XCode Update Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-update-reference.png)

> We've temporarily disabled "Update on Save" functionality as it resulted in an unacceptable typing delay on the watched file. We hope to re-enable this in a future version of XCode which doesn't exhibit degraded performance.

## Swift Native Types

Like most ServiceStack's features our goal with **Add ServiceStack Reference** is to deliver the most value as simply as possible. One way we try to achieve this is by reducing the cognitive load required to use our libraries by promoting a simple but powerful conceptual model that works consistently across differring implementations, environments, langauges as well as UI integration with the various VS.NET, Xamarin Studio and now XCode IDE's - in a recent React conference this was nicely captured with the phrase [Learn once, Write Anywhere](http://agateau.com/2015/learn-once-write-anywhere/).

Whilst each language is subtly different, all implementations work conceptually similar with all using Clean, Typed DTO's sent using a generic Service Gateway to facilitate its end-to-end typed communications. The client gateways also support DTO's from any source whether shared in source or binary form or generated with [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference).

### [JsonServiceClient.swift](https://github.com/ServiceStack/ServiceStack.Swift/blob/master/dist/JsonServiceClient.swift)

With this in mind we were able to provide the same ideal, high-level API we've enjoyed in [.NET's ServiceClients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) into idiomatic Swift as seen with its `ServiceClient` protocol definition below:

```swift
public protocol ServiceClient
{
    func get<T>(request:T, error:NSErrorPointer) -> T.Return?
    func get<T>(request:T, query:[String:String], error:NSErrorPointer) -> T.Return?
    func get<T>(relativeUrl:String, error:NSErrorPointer) -> T?
    func getAsync<T>(request:T) -> Promise<T.Return>
    func getAsync<T>(request:T, query:[String:String]) -> Promise<T.Return>
    func getAsync<T>(relativeUrl:String) -> Promise<T>
    
    func post<T>(request:T, error:NSErrorPointer) -> T.Return?
    func post<Response, Request>(relativeUrl:String, request:Request?, error:NSErrorPointer) -> Response?
    func postAsync<T>(request:T) -> Promise<T.Return>
    func postAsync<Response, Request>(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func put<T>(request:T, error:NSErrorPointer) -> T.Return?
    func put<Response, Request>(relativeUrl:String, request:Request?, error:NSErrorPointer) -> Response?
    func putAsync<T>(request:T) -> Promise<T.Return>
    func putAsync<Response, Request>(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func delete<T>(request:T, error:NSErrorPointer) -> T.Return?
    func delete<T>(request:T, query:[String:String], error:NSErrorPointer) -> T.Return?
    func delete<T>(relativeUrl:String, error:NSErrorPointer) -> T?
    func deleteAsync<T>(request:T) -> Promise<T.Return>
    func deleteAsync<T>(request:T, query:[String:String]) -> Promise<T.Return>
    func deleteAsync<T>(relativeUrl:String) -> Promise<T>
    
    func send<T>(intoResponse:T, request:NSMutableURLRequest, error:NSErrorPointer) -> T?
    func sendAsync<T>(intoResponse:T, request:NSMutableURLRequest) -> Promise<T>
    
    func getData(url:String, error:NSErrorPointer) -> NSData?
    func getDataAsync(url:String) -> Promise<NSData>
}
```

> Generic type constraints omitted for readability

The minor differences are primarily due to differences in Swift which instead of throwing Exceptions uses error codes and `Optional` return types and its lack of any asynchrony language support led us to embed a lightweight and [well-documented Promises](http://promisekit.org/introduction/) implementation in [PromiseKit](https://github.com/mxcl/PromiseKit) which closely matches the `Task<T>` type used in .NET Async API's.

### JsonServiceClient.swift Usage

If you've ever had to make HTTP requests using Objective-C's `NSURLConnection` or `NSURLSession` static classes in iOS or OSX, the higher-level API's in `JsonServiceClient` will feel like a breath of fresh air - which enable the same ideal client API's we've enjoyed in ServiceStack's .NET Clients, in Swift Apps! 

> A nice benefit of using JsonServiceClient over static classes is that Service calls can be easily substituted and mocked with the above `ServiceClient` protocol, making it easy to test or stub out the external Gateway calls whilst the back-end is under development.

To illustrate its usage we'll go through some client code to consume [TechStacks](https://github.com/ServiceStackApps/TechStacks) Services after adding a **ServiceStack Reference** to `http://techstaks.io`:

```swift
var client = JsonServiceClient(baseUrl: "http://techstacks.io")
var response = client.get(AppOverview())
```

Essentially usage is the same as it is in .NET ServiceClients - where it just needs the `baseUrl` of the remote ServiceStack instance, which can then be used to consume remote Services by sending typed Request DTO's that respond in kind with the expected Response DTO.

### Async API Usage

Whilst the sync API's are easy to use their usage should be limited in background threads so they're not blocking the Apps UI whilst waiting for responses. Most of the time when calling services from the Main UI thread you'll want to use the non-blocking async API's, which for the same API looks like:

```swift
client.getAsync(AppOverview())
    .then(body:{(r:AppOverviewResponse) -> Void in 
        ... 
    })
```

Which is very similar to how we'd make async `Task<T>` calls in C# when not using its async/await language syntax sugar. 

> Async callbacks are called back on the main thread, ideal for use in iOS Apps. This behavior is also configurable in the Promise's callback API.

### Typed Error Handling

As Swift doesn't provide `try/catch` Exception Handling, Error handling is a little different in Swift which for most failable API's just returns a `nil` Optional to indicate when the operation didn't succeed. When more information about the error is required, API's will typically accept an additional `NSError` pointer argument to populate with more information about the error. Any additional metadata can be attached to NSError's `userInfo` Dictionary. We also follow this same approach to provide our structured error handling in `JsonServiceClient`.

To illustrate exception handling we'll connect to ServiceStack's Test Services and call the `ThrowType` Service to intentionally throw the error specified, e.g:

#### Sync Error Handling

```swift
var client = JsonServiceClient(baseUrl: "http://test.servicestack.net")

var request = ThrowType()
request.type = "NotFound"
request.message = "custom message"

var error:NSError?

let response = client.post(request, &error)
response //= nil

error!.code //= 404
var status:ResponseStatus = error!.convertUserInfo() //Convert into typed ResponseStatus
status.message //= not here
status.stackTrace //= Server Stack Trace
```

> Note the explicit type definition on the return type is required here as Swift uses it as part of the generic method invocation.

#### Async Error Handling

To handle errors in Async API's we just add a callback on `.catch()` API on the returned Promise, e.g:

```swift
client.postAsync(request)
    .catch({ (error:NSError) -> Void in
        var status:ResponseStatus = error.convertUserInfo()
        //...
    })
```

### JsonServiceClient Error Handlers

Just like in .NET, we can also attach Global or instance error handlers to be able to generically handle all Service Client errors with a custom handler, e.g:

```swift
client.onError = {(e:NSError) in ... }
JsonServiceClient.Global.onError = {(e:NSError) in ... }
```

### Custom Routes

As Swift doesn't support Attributes any exported .NET Attributes are emitted in comments on the Request DTO they apply to, e.g:

```swift
// @Route("/technology/{Slug}")
public class GetTechnology : IReturn { ... }
```

This also means that the Custom Routes aren't used when making Service Requests and instead just uses ServiceStack's built-in [pre-defined routes](https://github.com/ServiceStack/ServiceStack/wiki/Routing#pre-defined-routes). 

But when preferred `JsonServiceClient` can also be used to call Services using Custom Routes, e.g:

```swift
var response:GetTechnologyResponse? = client.get("/technology/servicestack")
```

### JsonServiceClient Options

Other options that can be configured on JsonServiceClient include:

```swift
client.onError = {(e:NSError) in ... }
client.timeout = ...
client.cachePolicy = NSURLRequestCachePolicy.ReloadIgnoringLocalCacheData
client.requestFilter = {(req:NSMutableURLRequest) in ... }
client.responseFilter = {(res:NSURLResponse) in ... }

//static Global configuration
JsonServiceClient.Global.onError = {(e:NSError) in ... }
JsonServiceClient.Global.requestFilter = {(req:NSMutableURLRequest) in ... }
JsonServiceClient.Global.responseFilter = {(res:NSURLResponse) in ... }
```

## Introducing TechStacks iPhone and iPad App!

To illustrate the ease-of-use and utility of ServiceStack's new Swift support we've developed a native iOS App for http://techstacks.io that has been recently published and is now available to download for free on the AppStore:

[![TechStacks on AppStore](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-appstore.png)](https://itunes.apple.com/us/app/techstacks/id965680615?ls=1&mt=8)

The complete source code for the [TechStacks App is available on GitHub](https://github.com/ServiceStackApps/TechStacks) - providing a good example on how easy it is to take advantage of ServiceStack's Swift support to quickly build a rich and responsive Services-heavy native iOS App. 

All remote Service Calls used by the App are encapsulated into a single [AppData.swift](https://github.com/ServiceStackApps/TechStacksApp/blob/master/src/TechStacks/AppData.swift) class and only uses JsonServiceClient's non-blocking Async API's to ensure a Responsive UI is maintained throughout the App.

### MVC and Key-Value Observables (KVO)

If you've ever had to implement `INotifyPropertyChanged` in .NET, you'll find the built-in model binding capabilities in iOS/OSX a refreshing alternative thanks to Objective-C's underlying `NSObject` which automatically generates automatic change notifications for its KV-compliant properties. UIKit and Cocoa frameworks both leverage this feature to enable its [Model-View-Controller Pattern](https://developer.apple.com/library/mac/documentation/General/Conceptual/DevPedia-CocoaCore/MVC.html). 

As keeping UI's updated with Async API callbacks can get unwieldy, we wanted to go through how we're taking advantage of NSObject's KVO support in Service Responses to simplify maintaining dynamic UI's.

### Enable Key-Value Observing in Swift DTO's

Firstly to enable KVO in your Swift DTO's we'll want to have each DTO inherit from `NSObject` which can be done by uncommenting `BaseObject` option in the header comments as seen below:

```
/* Options:
Date: 2015-02-19 22:43:04
Version: 1
BaseUrl: http://techstacks.io

BaseClass: NSObject
...
*/
```
and click the **Update ServiceStack Reference** Menu Option to fetch the updated DTO's.

Then to [enable Key-Value Observing](https://developer.apple.com/library/ios/documentation/Swift/Conceptual/BuildingCocoaApps/AdoptingCocoaDesignPatterns.html#//apple_ref/doc/uid/TP40014216-CH7-XID_8) just mark the response DTO variables with the `dynamic` modifier, e.g:

```swift
public dynamic var allTiers:[Option] = []
public dynamic var overview:AppOverviewResponse = AppOverviewResponse()
public dynamic var topTechnologies:[TechnologyInfo] = []
public dynamic var allTechnologies:[Technology] = []
public dynamic var allTechnologyStacks:[TechnologyStack] = []
```

Which is all that's needed to allow properties to be observed as they'll automatically issue change notifications when they're populated in the Service response async callbacks, e.g:

```swift
func loadOverview() -> Promise<AppOverviewResponse> {
    return client.getAsync(AppOverview())
        .then(body:{(r:AppOverviewResponse) -> AppOverviewResponse in
            self.overview = r
            self.allTiers = r.allTiers
            self.topTechnologies = r.topTechnologies
            return r
        })
}

func loadAllTechnologies() -> Promise<GetAllTechnologiesResponse> {
    return client.getAsync(GetAllTechnologies())
        .then(body:{(r:GetAllTechnologiesResponse) -> GetAllTechnologiesResponse in
            self.allTechnologies = r.results
            return r
        })
}

func loadAllTechStacks() -> Promise<GetAllTechnologyStacksResponse> {
    return client.getAsync(GetAllTechnologyStacks())
        .then(body:{(r:GetAllTechnologyStacksResponse) -> GetAllTechnologyStacksResponse in
            self.allTechnologyStacks = r.results
            return r
        })
}
```

### Observing Data Changes

In your [ViewController](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/HomeViewController.swift) have the datasources for your custom views binded to the desired data (which will initially be empty):

```swift
func pickerView(pickerView: UIPickerView, numberOfRowsInComponent component: Int) -> Int {
    return appData.allTiers.count
}
...
func tableView(tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
    return appData.topTechnologies.count
}
```

Then in `viewDidLoad()` [start observing the properties](https://github.com/ServiceStack/ServiceStack.Swift/blob/67c5c092b92927702f33b6a0669e3aa1de0e2cdc/apps/TechStacks/TechStacks/HomeViewController.swift#L31) your UI Controls are bound to, e.g:

```swift
override func viewDidLoad() {
    ...
    self.appData.observe(self, properties: ["topTechnologies", "allTiers"])
    self.appData.loadOverview()
}
deinit { self.appData.unobserve(self) }
```

In the example code above we're using some custom [KVO helpers](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/AppData.swift#L159-L183) to keep the code required to a minimum.

With the observable bindings in place, the change notifications of your observed properties can be handled by overriding `observeValueForKeyPath()` which passes the name of the property that's changed in the `keyPath` argument that can be used to determine the UI Controls to refresh, e.g:

```swift
override func observeValueForKeyPath(keyPath:String, ofObject object:AnyObject, change:[NSObject:AnyObject],
  context: UnsafeMutablePointer<Void>) {
    switch keyPath {
    case "allTiers":
        self.technologyPicker.reloadAllComponents()
    case "topTechnologies":
        self.tblView.reloadData()
    default: break
    }
}
```

Now that everything's configured, the observables provide an alternative to manually updating UI elements within async callbacks, instead you can now fire-and-forget your async API's and rely on the pre-configured bindings to automatically update the appropriate UI Controls when their bounded properties are updated, e.g:

```swift
self.appData.loadOverview() //Ignore response and use configured KVO Bindings
```

### Images and Custom Binary Requests

In addition to greatly simplifying Web Service Requests, `JsonServiceClient` also makes it easy to fetch any custom HTTP response like Images and other Binary data using the generic `getData()` and `getDataAsync()` NSData API's. This is used in TechStacks to [maintain a cache of all loaded images](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/AppData.swift#L144), reducing number of HTTP requests and load times when navigating between screens:

```swift
var imageCache:[String:UIImage] = [:]

public func loadImageAsync(url:String) -> Promise<UIImage?> {
    if let image = imageCache[url] {
        return Promise<UIImage?> { (complete, reject) in complete(image) }
    }
    
    return client.getDataAsync(url)
        .then(body: { (data:NSData) -> UIImage? in
            if let image = UIImage(data:data) {
                self.imageCache[url] = image
                return image
            }
            return nil
        })
}
```

## TechStacks OSX Desktop App!

As `JsonServiceClient.swift` has no external dependencies and only relies on core `Foundation` classes it can be used anywhere Swift can including OSX Cocoa Desktop and Command Line Apps and Frameworks.

Most of the API's used in TechStacks iOS App are standard typed Web Services calls. We've also developed a TechStacks OSX Desktop to showcase how easy it is to call ServiceStack's dynamic [AutoQuery Services](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) and how much auto-querying functionality they can provide for free.

E.g. The TechStacks Desktop app is essentially powered with these 2 AutoQuery Services:

```csharp
[Query(QueryTerm.Or)] //change from filtering (default) to combinatory semantics
public class FindTechStacks : QueryBase<TechnologyStack> {}

[Query(QueryTerm.Or)]
public class FindTechnologies : QueryBase<Technology> {}
```

Basically just a Request DTO telling AutoQuery what Table we want to Query and that we want to [change the default Search behavior](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#changing-querying-behavior) to have **OR** semantics. We don't need to specify which properties we can query as the [implicit conventions](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#implicit-conventions) automatically infer it from the table being queried.

The TechStacks Desktop UI is then built around these 2 AutoQuery Services allowing querying against each field and utilizing a subset of the implicit conventions supported:

### Querying Technology Stacks

![TechStack Desktop Search Fields](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-desktop-field.png)

### Querying Technologies

![TechStack Desktop Search Type](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-desktop-type.png)

Like the TechStacks iOS App all Service Calls are maintained in a single [AppData.swift](https://github.com/ServiceStackApps/TechStacksDesktopApp/blob/master/src/TechStacksDesktop/AppData.swift) class and uses KVO bindings to update its UI which is populated from these 2 services below:

```swift
func searchTechStacks(query:String, field:String? = nil, operand:String? = nil)
  -> Promise<QueryResponse<TechnologyStack>> {
    self.search = query
    
    let queryString = query.count > 0 && field != nil && operand != nil
        ? [createAutoQueryParam(field!, operand!): query]
        : ["NameContains":query, "DescriptionContains":query]
    
    let request = FindTechStacks<TechnologyStack>()
    return client.getAsync(request, query:queryString)
        .then(body:{(r:QueryResponse<TechnologyStack>) -> QueryResponse<TechnologyStack> in
            self.filteredTechStacks = r.results
            return r
        })
}

func searchTechnologies(query:String, field:String? = nil, operand:String? = nil)
  -> Promise<QueryResponse<Technology>> {
    self.search = query

    let queryString = query.count > 0 && field != nil && operand != nil
        ? [createAutoQueryParam(field!, operand!): query]
        : ["NameContains":query, "DescriptionContains":query]
    
    let request = FindTechnologies<Technology>()
    return client.getAsync(request, query:queryString)
        .then(body:{(r:QueryResponse<Technology>) -> QueryResponse<Technology> in
            self.filteredTechnologies = r.results
            return r
        })
}

func createAutoQueryParam(field:String, _ operand:String) -> String {
    let template = autoQueryOperandsMap[operand]!
    let mergedField = template.replace("%", withString:field)
    return mergedField
}
```

Essentially employing the same strategy for both AutoQuery Services where it builds a query String parameter to send with the request. For incomplete queries, the default search queries both `NameContains` and `DescriptionContains` field conventions returning results where the Search Text is either in `Name` **OR** `Description` fields.

## Swift Generated DTO Types

With Swift support our goal was to ensure a high-fidelity, idiomatic translation within the constraints of Swift language and built-in libraries, where the .NET Server DTO's are translated into clean Swift POSO's (Plain Old Swift Objects :) having their .NET built-in types mapped to their equivalent Swift data type. 

To see what this ended up looking like, we'll peel back behind the covers and look at a couple of the [Generated Swift Test Models](http://test.servicestack.net/types/swift) to see how they're translated in Swift:

```swift
public class AllTypes
{
    required public init(){}
    public var id:Int?
    public var nullableId:Int?
    public var byte:Int8?
    public var short:Int16?
    public var int:Int?
    public var long:Int64?
    public var uShort:UInt16?
    public var uInt:UInt32?
    public var uLong:UInt64?
    public var float:Float?
    public var double:Double?
    public var decimal:Double?
    public var string:String?
    public var dateTime:NSDate?
    public var timeSpan:NSTimeInterval?
    public var dateTimeOffset:NSDate?
    public var guid:String?
    public var char:Character?
    public var nullableDateTime:NSDate?
    public var nullableTimeSpan:NSTimeInterval?
    public var stringList:[String] = []
    public var stringArray:[String] = []
    public var stringMap:[String:String] = [:]
    public var intStringMap:[Int:String] = [:]
    public var subType:SubType?
}

public class AllCollectionTypes
{
    required public init(){}
    public var intArray:[Int] = []
    public var intList:[Int] = []
    public var stringArray:[String] = []
    public var stringList:[String] = []
    public var pocoArray:[Poco] = []
    public var pocoList:[Poco] = []
    public var pocoLookup:[String:[Poco]] = [:]
    public var pocoLookupMap:[String:[String:Poco]] = [:]
}

public enum EnumType : Int
{
    case Value1
    case Value2
}
```

As seen above, properties are essentially mapped to their optimal Swift equivalent. As DTO's can be partially complete all properties are `Optional` except for enumerables which default to an empty collection - making them easier to work with and despite their semantic differences, .NET enums are translated into typed Swift enums.

### Swift Challenges

The current stable version of Swift has several limitations that prevented using similar reflection and metaprogramming/code-gen techniques we're used to with .NET to implement them efficiently in Swift, e.g. Swift has an incomplete reflection API that can't set a property, is unable to cast `Any` (aka object) back to a concrete Swift type, unable to get the string literal for an enum value and we ran into many other Swift compiler limitations that would segfault whilst exploring this strategy.

Some of these limitations could be worked around by having every type inherit from `NSObject` and bridging to use the dynamism in Objective-C API's, but ultimately we decided against depending on `NSObject` or using Swift's built-in reflection API's which we also didn't expect to perform well in iOS's NoJIT environment which doesn't allow caching of reflection access to maintain
optimal runtime performance. 

### Swift Code Generation

As we were already using code-gen to generate the Swift types we could extend it without impacting the Developer UX which we expanded to also include what's essentially an **explicit Reflection API** for each type with API's to support serializing to and from JSON. Thanks to Swift's rich support for extending types we were able to leverage its Type extensions so the implementation details could remain disconnected from the clean Swift type definitions allowing improved readability when inspecting the remote DTO schema's.

We can look at `AllCollectionTypes` to see an example of the code-gen that's generated for each type, essentially emitting explicit readable/writable closures for each property: 

```swift
extension AllCollectionTypes : JsonSerializable
{
    public class var typeName:String { return "AllCollectionTypes" }
    public class func reflect() -> Type<AllCollectionTypes> {
        return TypeConfig.config() ?? TypeConfig.configure(Type<AllCollectionTypes>(
            properties: [
                Type<AllCollectionTypes>.arrayProperty("intArray", get: { $0.intArray }, set: { $0.intArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("intList", get: { $0.intList }, set: { $0.intList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringArray", get: { $0.stringArray }, set: { $0.stringArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringList", get: { $0.stringList }, set: { $0.stringList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoArray", get: { $0.pocoArray }, set: { $0.pocoArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoList", get: { $0.pocoList }, set: { $0.pocoList = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookup", get: { $0.pocoLookup }, set: { $0.pocoLookup = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookupMap", get: { $0.pocoLookupMap }, set: { $0.pocoLookupMap = $1 }),
            ]))
    }
    public func toJson() -> String {
        return AllCollectionTypes.reflect().toJson(self)
    }
    public class func fromJson(json:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromJson(AllCollectionTypes(), json: json)
    }
    public class func fromObject(any:AnyObject) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromObject(AllCollectionTypes(), any:any)
    }
    public func toString() -> String {
        return AllCollectionTypes.reflect().toString(self)
    }
    public class func fromString(string:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromString(AllCollectionTypes(), string: string)
    }
}
```

### Swift Native Types Limitations

Due to the semantic differences and limitations in Swift there are some limitations of what's not supported. Luckily these limitations are mostly [highly-discouraged bad practices](http://stackoverflow.com/a/10759250/85785) which is another reason not to use them. Specifically what's not supported:

#### No `object` or `Interface` properties
When emitting code we'll generate a comment when ignoring these properties, e.g:
```swift
//emptyInterface:IEmptyInterface ignored. Swift doesn't support interface properties
```

#### Base types must be marked abstract
As Swift doesn't support extension inheritance, when using inheritance in DTO's any Base types must be marked abstract.

#### All DTO Type Names must be unique
Required as there are no namespaces in Swift (Also required for F# and TypeScript). ServiceStack only requires Request DTO's to be unique, but our recommendation is for all DTO names to be unique.

#### IReturn not added for Array Responses
As Swift doesn't allow extending generic Arrays with public protocols, the `IReturn` marker that enables the typed ServiceClient API isn't available for Requests returning Array responses. You can workaround this limitation by wrapping the array in a Response DTO whilst we look at other solutions to support this in future.

## Swift Configuration

The header comments in the generated DTO's allows for further customization of how the DTO's are generated which can then be updated with any custom Options provided using the **Update ServiceStack Reference** Menu Item in XCode. Options that are preceded by a Swift single line comment `//` are defaults from the server that can be overridden, e.g:

```swift
/* Options:
Date: 2015-02-22 13:52:26
Version: 1
BaseUrl: http://techstacks.io

//BaseClass: 
//AddModelExtensions: True
//AddServiceStackTypes: True
//IncludeTypes: 
//ExcludeTypes: 
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//DefaultImports: Foundation
*/
```

To override a value, remove the `//` and specify the value to the right of the `:`. Any value uncommented will be sent to the server to override any server defaults.

We'll go through and cover each of the above options to see how they affect the generated DTO's:

### BaseClass
Specify a base class that's inherited by all Swift DTO's, e.g. to enable [Key-Value Observing (KVO)](https://developer.apple.com/library/ios/documentation/Cocoa/Conceptual/KeyValueObserving/KeyValueObserving.html) in the generated DTO models have all types inherit from `NSObject`:

```
/* Options:
BaseClass: NSObject
```

Will change all DTO types to inherit from `NSObject`:

```swift
public class UserInfo : NSObject { ... }
```

### AddModelExtensions
Remove the the code-generated type extensions required to support typed JSON serialization of the Swift types and leave only the clean Swift DTO Type definitions.
```
/* Options:
AddModelExtensions: False
```

### AddServiceStackTypes
Don't generate the types for built-in ServiceStack classes and Services like `ResponseStatus` and `Authenticate`, etc.
```
/* Options:
AddServiceStackTypes: False
```

### IncludeTypes
Is used as a Whitelist that can be used to specify only the types you would like to have code-generated:
```
/* Options:
IncludeTypes: GetTechnology,GetTechnologyResponse
```
Will only generate `GetTechnology` and `GetTechnologyResponse` DTO's:
```swift
public class GetTechnology { ... }
public class GetTechnologyResponse { ... }
```

### ExcludeTypes
Is used as a Blacklist where you can specify which types you would like to exclude from being generated:
```
/* Options:
ExcludeTypes: GetTechnology,GetTechnologyResponse
```
Will exclude `GetTechnology` and `GetTechnologyResponse` DTO's from being generated.

### AddResponseStatus
Automatically add a `ResponseStatus` property on all Response DTO's, regardless if it wasn't already defined:
```
/* Options:
AddResponseStatus: True
```
Will add a `ResponseStatus` property to all Response DTO's:
```swift
public class GetAllTechnologiesResponse
{
    ...
    public var responseStatus:ResponseStatus
}
```

### AddImplicitVersion
Lets you specify the Version number to be automatically populated in all Request DTO's sent from the client: 
```
/* Options:
AddImplicitVersion: 1
```
Will add an initialized `version` property to all Request DTO's:
```swift
public class GetAllTechnologies : IReturn
{
    ...
    public var version:Int = 1
}
```

This lets you know what Version of the Service Contract that existing clients are using making it easy to implement ServiceStack's [recommended versioning strategy](http://stackoverflow.com/a/12413091/85785). 

### InitializeCollections
Whether enumerables should be initialized with an empty collection (default) or changed to use an Optional type:
```
/* Options:
InitializeCollections: False
```
Changes Collection Definitions to be declared as Optional Types instead of being initialized with an empty collection:
```swift
public class ResponseStatus
{
    public var errors:[ResponseError]?
}
```

### DefaultImports
Add additional import statements to the generated DTO's:
```
/* Options:
DefaultImports: UIKit,Foundation
```
Will import the `UIKit` and `Foundation` frameworks:
```swift
import UIKit;
import Foundation;
```

## Improved Add ServiceStack Reference

Whilst extending Add ServiceStack Reference to add support for Swift above we've also made a number of refinements to the existing native type providers including:

 - Improved support for nested classes
 - Improved support from complex generic and inherited generic type definitions
 - Ignored DTO properties are no longer emitted
 - Uncommon Language-specific configuration moved into the native type providers 
 - New DefaultImports option available to TypeScript and Swift native types

### New Include and Exclude Types option added to all languages

You can now control what types are generated by using `ExcludeTypes` which acts as a blacklist excluding those specific types, e.g:

```
ExcludeTypes: ResponseStatus,ResponseError
```

In contrast to ExcludeTypes, if you're only making use of a couple of Services you can use `IncludeTypes` which acts like a White-List ensuring only those specific types are generated, e.g:

```
IncludeTypes: GetTechnologyStacks,GetTechnologyStacksResponse
```

### GlobalNamespace option added in C# and VB.NET projects

F#, TypeScript and Swift are limited to generating all DTO's under a single global namespace, however in most cases this is actually preferred as it strips away the unnecessary details of how the DTO's are organized on the Server (potentially across multiple dlls/namespaces) and presents them under a single configurable namespace to the client.

As it's a nice client feature, we've also added this option to C# and VB.NET native types as well which can be enabled by uncommenting the `GlobalNamespace` option, e.g:

```csharp
/* Options:
Version: 1
BaseUrl: http://techstacks.io

GlobalNamespace: ServiceModels
...
*/

namespace ServiceModels
{
...
}
```

## Integrated HTML, CSS and JavaScript Minification

As part of our quest to provide a complete and productive solution for developing highly responsive Web, Desktop and Mobile Apps, ServiceStack now includes minifiers for compressing HTML, CSS and JavaScript available from the new `Minifiers` class: 

```csharp
var minifiedJs = Minifiers.JavaScript.Compress(js);
var minifiedCss = Minifiers.Css.Compress(css);
var minifiedHtml = Minifiers.Html.Compress(html);

// Also minify in-line CSS and JavaScript
var advancedMinifiedHtml = Minifiers.HtmlAdvanced.Compress(html);
```

> Each minifier implements the lightweight [ICompressor](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/ICompressor.cs) interface making it trivial to mock or subtitute with a custom implementation.

#### JS Minifier
For the JavaScript minifier we're using [Ext.Net's C# port](https://github.com/extnet/Ext.NET.Utilities/blob/master/Ext.Net.Utilities/JavaScript/JSMin.cs) of Douglas Crockford's venerable [JSMin](http://crockford.com/javascript/jsmin). 

#### CSS Minifier
The CSS Minifer uses Mads Kristensen simple [CSS Minifer](http://madskristensen.net/post/efficient-stylesheet-minification-in-c). 

#### HTML Compressor
For compressing HTML we're using a [C# Port](http://blog.magerquark.de/c-port-of-googles-htmlcompressor-library/) of Google's excellent [HTML Compressor](https://code.google.com/p/htmlcompressor/) which we've further modified to remove the public API's ugly Java-esque idioms and replaced them with C# properties.

The `HtmlCompressor` also includes a number of well-documented options which can be customized by configuring the available properties on its concrete type, e.g:

```csharp
var htmlCompressor = (HtmlCompressor)Minifier.Html;
htmlCompressor.RemoveComments = false;
```

### Easy win for server generated websites

If your project is not based off one of our optimized Gulp/Grunt.js powered [React JS and AngularJS Single Page App templates](https://github.com/ServiceStack/ServiceStackVS) or configured to use our eariler [node.js-powered Bundler](https://github.com/ServiceStack/Bundler) Web Optimization solution, these built-in minifiers now offers the easiest solution to effortlessly optimize your existing website which is able to work transparently with your existing Razor Views and static `.js`, `.css` and `.html` files without requiring adding any additional external tooling or build steps to your existing development workflow.

### Minify dynamic Razor Views

Minification of Razor Views is easily enabled by specifying `MinifyHtml=true` when registering the `RazorFormat` plugin:

```csharp
Plugins.Add(new RazorFormat {
    MinifyHtml = true,
    UseAdvancedCompression = true,
});
```

Use the `UseAdvancedCompression=true` option if you also want to minify inline js/css, although as this requires a bit more processing you'll want to benchmark it to see if it's providing an overall performance benefit to end users. It's a recommended option if you're caching Razor Pages. Another solution is to minimize the use of in-line js/css and move them to static files to avoid needing in-line js/css compression.

### Minify static `.js`, `.css` and `.html` files

With nothing other than the new minifiers, we can leverage the flexibility in ServiceStack's [Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system) to provide an elegant solution for minifying static `.html`, `.css` and `.js` resources by simply pre-loading a new InMemory Virtual FileSystem with minified versions of existing files and giving the Memory FS a higher precedence so any matching requests serve up the minified version first. We only need to pre-load the minified versions once on StartUp by overriding `GetVirtualPathProviders()` in the AppHost:

```csharp
public override List<IVirtualPathProvider> GetVirtualPathProviders()
{
    var existingProviders = base.GetVirtualPathProviders();
    var memFs = new InMemoryVirtualPathProvider(this);

    //Get existing Local FileSystem Provider
    var fs = existingProviders.First(x => x is FileSystemVirtualPathProvider);

    //Process all .html files:
    foreach (var file in fs.GetAllMatchingFiles("*.html"))
    {
        var contents = Minifiers.HtmlAdvanced.Compress(file.ReadAllText());
        memFs.AddFile(file.VirtualPath, contents);
    }

    //Process all .css files:
    foreach (var file in fs.GetAllMatchingFiles("*.css")
      .Where(file => !file.VirtualPath.EndsWith(".min.css"))) //ignore pre-minified .css
    {
        var contents = Minifiers.Css.Compress(file.ReadAllText());
        memFs.AddFile(file.VirtualPath, contents);
    }

    //Process all .js files
    foreach (var file in fs.GetAllMatchingFiles("*.js")
      .Where(file => !file.VirtualPath.EndsWith(".min.js"))) //ignore pre-minified .js
    {
        try
        {
            var js = file.ReadAllText();
            var contents = Minifiers.JavaScript.Compress(js);
            memFs.AddFile(file.VirtualPath, contents);
        }
        catch (Exception ex)
        {
            //As JSMin is a strict subset of JavaScript, this can fail on valid JS.
            //We can report exceptions in StartUpErrors so they're visible in ?debug=requestinfo
            base.OnStartupException(new Exception("JSMin Error {0}: {1}".Fmt(file.VirtualPath, ex.Message)));
        }
    }

    //Give new Memory FS the highest priority
    existingProviders.Insert(0, memFs);
    return existingProviders;
}
```

A nice benefit of this approach is that it doesn't pollute your project with minified build artifacts, has excellent runtime performance with the minfied contents being served from Memory and as the file names remain the same, the links in HTML don't need to be rewritten to reference the minified versions. i.e. When a request is made it just looks through the registered virtual path providers and returns the first match, which given the Memory FS was inserted at the start of the list, returns the minified version.

### Enabled in [servicestack.net](https://servicestack.net)

As this was an quick and non-invasive feature to add, we've enabled it on all [servicestack.net](https://servicestack.net) Razor views and static files. You can `view-source:https://servicestack.net/` (as url in Chrome, Firefox or Opera) to see an example of the resulting minified output. 

## New [ServiceStack Cookbook](https://www.packtpub.com/application-development/servicestack-cookbook) Released!

<a href="https://www.packtpub.com/application-development/servicestack-cookbook"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestack-cookbook.jpg" align="left" vspace="10" width="320" height="380" /></a>

A new [ServiceStack Cookbook](https://www.packtpub.com/application-development/servicestack-cookbook) was just released by ThoughtWorker [@kylehodgson](https://twitter.com/kylehodgson) and our own [Darren Reid](https://twitter.com/layoric). 

The ServiceStack Cookbook includes over 70 recipes on creating message-based Web Services and Apps including leveraging OrmLite to build fast, testable and maintainable Web APIs - focusing on solving real-world problems that are a pleasure to create, maintain and consume with ServiceStack.

<img src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7" width="700" height="1">

## Support for RecyclableMemoryStream

The Bing team recently [opensourced their RecyclableMemoryStream](http://www.philosophicalgeek.com/2015/02/06/announcing-microsoft-io-recycablememorystream/) implementation which uses pooled, reusable byte buffers to help minimize GC pressure. To support switching to the new `RecyclableMemoryStream` we've changed most of ServiceStack's MemoryStream usages to use the new `MemoryStreamFactory.GetStream()` API's, allowing ServiceStack to be configured to use the new `RecyclableMemoryStream` implementation with:

```csharp
 MemoryStreamFactory.UseRecyclableMemoryStream = true;
```

Which now changes `MemoryStreamFactory.GetStream()` to return instances of `RecyclableMemoryStream`, e.g:

```csharp
using (var ms = (RecyclableMemoryStream)MemoryStreamFactory.GetStream()) { ... }
```

> To reduce dependencies and be able to support PCL clients we're using an [interned and PCL-compatible version of RecyclableMemoryStream](https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/RecyclableMemoryStream.cs) in ServiceStack.Text which has its auditing features disabled.

### RecyclableMemoryStream is strict

Whilst the announcement says `RecyclableMemoryStream` is a drop-in replacement for `MemoryStream`, this isn't strictly true as it enforces stricter rules on how you can use MemoryStreams. E.g. a common pattern when using a MemoryStream with a `StreamWriter` or `StreamReader` is, since it's also disposable, to enclose it in a nested using block as well:

```csharp
using (var ms = MemoryStreamFactory.GetStream())
using (var writer = new StreamWriter(ms)) {
...
}
```

But this has the effect of closing the stream twice which is fine in `MemoryStream` but throws an `InvalidOperationException` when using `RecyclableMemoryStream`. If you find this happening in your Application you can use the new `MemoryStreamFactory.MuteDuplicateDisposeExceptions=true` option we've added as a stop-gap to mute these exceptions until you're able to update your code to prevent this.

### Doesn't allow reading from closed streams 

Another gotcha when switching over to `RecyclableMemoryStream` is that once you close the Stream you'll no longer be able to read from it. This makes it incompatible to use with .NET's built-in `GZipStream` and `DeflateStream` classes which can only be read after its closed, having the effect of closing the underlying MemoryStream - preventing being able to access the compressed bytes.

Where possible we've refactored ServiceStack to use `MemoryStreamFactory.GetStream()` and adhered to its stricter usage so ServiceStack can be switched over to use it with `MemoryStreamFactory.UseRecyclableMemoryStream=true`, although we still have some more optimization work to be able to fully take advantage of it by changing our usage of `ToArray()` to use the more optimal `GetBuffer()` and `Length` API's where possible.

## Authentication

 - New `MicrosoftLiveOAuth2Provider` Microsoft Live OAuth2 Provider added by [@ivanfioravanti](https://github.com/ivanfioravanti)
 - New `InstagramOAuth2Provider` Instagram OAuth2 Provider added by [@ricardobrandao](https://github.com/ricardobrandao)

### New Url Filters added to all AuthProviders

New Url Filters have been added to all AuthProvider redirects letting you inspect or customize and decorate any redirect urls that are forwarded to the remote OAuth Server or sent back to the authenticating client. The list different Url Filters available in all AuthProviders include:

```csharp
new AuthProvider {
    PreAuthUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    AccessTokenUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    SuccessRedirectUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    FailedRedirectUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    LogoutUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
}
```

## OrmLite

 - Custom Decimal precision with `[DecimalLength(precision,scale)]` added to all OrmLite RDBMS Providers
 - Sqlite persists and queries DateTime's using LocalTime

## Messaging

 - Added new docs showing how to populate Session Ids to [make authenticated MQ requests](https://github.com/ServiceStack/ServiceStack/wiki/Messaging#authenticated-requests-via-mq).
 - Request/Reply MQ Requests for Services with no response will send the Request DTO back to `ReplyTo` Queue instead of the [default .outq topic](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ#messages-with-no-responses-are-sent-to-outq-topic).

## Misc

 - Default 404 Handler for HTML now emits Error message in page body
 - New `Dictionary<string, object> Items` were added to all Response Contexts which can be used to transfer metadata through the response pipeline 
 - `BufferedStream` is now accessible on concrete Request/Response Contexts
 - Added new `GetKeysByPattern()` API to `MemoryCacheClient`
 - Allow `DTO.ToAbsoluteUrl()` extension method to support ASP.NET requests without needing to configure `Config.WebHostUrl`. Self Hosts can use the explicit `DTO.ToAbsoluteUrl(IRequest)` API.
 - New `HostContext.TryGetCurrentRequest()` Singleton returns Current Request for ASP.NET hosts, `null` for Self Hosts. 
    - `HostContext.GetCurrentRequest()` will throw for Self Hosts which don't provide singleton access to the current HTTP Request
 - Added new `string.CollapseWhitespace()` extension method to collapse multiple white-spaces into a single space.

### Using ServiceStack as a Proxy

The new `Config.SkipFormDataInCreatingRequest` option instructs ServiceStack to skip reading from the Request's **FormData** on initialization (to support `X-Http-Method-Override` Header) so it avoids forced loading of the Request InputStream allowing ServiceStack to be used as a HTTP proxy with:

```csharp
RawHttpHandlers.Add(_ => new CustomActionHandler((req, res) => {
    var bytes = req.InputStream.ReadFully();
    res.OutputStream.Write(bytes, 0, bytes.Length);
}));
```

## NuGet dependency updates

 - Npgsql updated to 2.2.4.3
 - NLog updated to v3.2.0.0

## Updated Versioning Strategy

To make it easier for developers using interim [pre-release packages on MyGet](https://github.com/ServiceStack/ServiceStack/wiki/MyGet) upgrade to the official NuGet packages once they're released, we've started using odd version numbers (e.g **v4.0.37**) for pre-release MyGet builds and even numbers (e.g. **v4.0.38**) for official released packages on NuGet.

## Breaking changes

 - `void` or `null` responses return `204 NoContent` by default, can be disabled with `Config.Return204NoContentForEmptyResponse = false`
 - Failed Auth Validations now clear the Users Session
 - `ServiceExtensions.RequestItemsSessionKey` moved to `SessionFeature.RequestItemsSessionKey`

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


