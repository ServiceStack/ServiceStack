# ServiceStack v5 Release Notes

The theme of this major v5 release is integration and unification. Triggered by the release of .NET Standard 2.0 which we believe is the .NET Contract offering the broadest compatibility and stabilized API surface we can standardize around. As a result we've upgraded all builds to .NET Standard 2.0 and all .NET Core Apps and Test projects to .NET Core 2.0 and merged our .NET Standard `.Core` NuGet packages into the main ServiceStack NuGet packages unifying them into a single suite of NuGet packages and release cadence. If you were waiting for .NET Core to stabilize before migrating or depending on the platform for new green field projects, we believe .NET Core 2.0 and .NET Standard 2.0 is ready for widespread adoption that is also the platform we'll be offering long term release support for.
 
This release also unifies the `.Signed` NuGet package variants where now all .NET 4.5 builds are strong named by default using the `servicestack.snk` signing key that's in the `/src` folder of each Project. The .NET Standard builds continue to remain unsigned so they can be built on each platform using .NET Core's `dotnet build` command. Consolidating all NuGet package variants into the main NuGet packages simplifies the development experience of Customers who need to use .Signed packages as they now get a first-class experience with the Project Templates and Integration in ServiceStackVS VS.NET Extension.

Before upgrading to v5 please read the [V5 Changes and Migration Notes](#v5-changes-and-migration-notes).

## One ServiceStack
 
An unfortunate side-effect of the multi-year development and multiple breaking versions being done in the open for what is now known as .NET Core is the trail of outdated blog posts and invalid documentation which has resulted in a lot of confusion and poor messaging about .NET Core 2.0's current polished and capable form and how it differentiates to the classic ASP .NET Framework. 
 
With the amount of accumulated misinformation one could be forgiven for thinking they're 2 completely different fragmented worlds shrouded in a myriad of incompatible dependencies. Whilst that may be true of some frameworks it's not the case of ServiceStack which maintains a single code base implementation, a single set of NuGet packages, single set of documentation relevant for all supported ASP.NET, HttpListener and .NET Core hosts.
 
This is made possible thanks to ServiceStack's high-level host agnostic API and our approach to decouple from concrete HTTP abstractions behind lightweight `IRequest` interfaces which is what allows the same ServiceStack Services to run on ASP.NET, Self Host HttpListener, SOAP Endpoints, multiple MQ Hosts and .NET Core Apps.
 
The primary advantage of this is simplicity, in both cognitive overhead for creating Services that target multiple hosts, reuse of existing knowledge and investments in using ServiceStack libraries and features and significantly reduced migration efforts to port existing .NET Framework code-bases to run on .NET Core where it enjoys near perfect source code compatibility. 
 
ServiceStack's exceptional source code compatibility is visible in our new .NET Core 2.0 and .NET Framework project templates where all templates utilize our recommended Physical Project Structure, share the same NuGet dependencies, same source code for its Server and Client App implementation as well as its Unit and Integration Tests. The primary difference between .NET Core and .NET Framework templates is how your AppHost is initialized, which for ASP.NET uses `Global.asax` whilst for .NET Core it's registered in .NET Core's pipeline like any other feature. The `.csproj` also differs where .NET Core uses MSBuild's new and minimal human-friendly format whereas ASP.NET Framework templates leverage VS.NET's classic project format for compatibility with older VS .NET versions.

## New .NET Core 2.0 and .NET Framework Project Templates
 
Now that ServiceStack v5 has merged into a single unified set of NuGet packages and consolidated around the long-term stable .NET Core 2.0 and .NET Standard 2.0 platform, we've developed **11 new .NET Core 2.0 project templates** for each of ServiceStack's most popular starting templates. Each .NET Core 2.0 template has an equivalent .NET Framework template except for [ServiceStack's Templates WebApp](http://templates.servicestack.net/docs/web-apps) which is itself a pre-built .NET Core 2.0 App that lets you develop Web Applications and HTTP APIs on-the-fly without any compilation.

All .NET Core 2.0 Templates can be developed using your preferred choice of either VS Code, VS.NET or JetBrains Project Rider on your preferred OS. Given the diverse ecosystem used to develop .NET Core Applications, the new Project Templates are being maintained on GitHub and made available via our new [dotnet-new](/dotnet-new) command-line tooling which can be installed from npm:
 
    $ npm install -g servicestack-cli
 
This will make the `dotnet-new` script available which you can use to view all available templates by running `dotnet-new` without any arguments:

![](http://docs.servicestack.net/images/ssvs/dotnet-new-list.png)

Then create new projects with:
 
    $ dotnet-new <template-name> <project-name>
 
E.g. to create a new Vue SPA template:
 
    $ dotnet-new vue-spa Acme
 
The resulting `Acme.sln` can be opened in VS 2017 which will automatically restore and install both the .NET and npm packages on first load and build. This can take a bit of time to install everything, once it's finished you'll see the `wwwroot` folder populated with your generated Webpack app which includes a `dist` folder and `index.html` page. After these are generated you can run your App with **F5** as normal. 

![](http://docs.servicestack.net/images/ssvs/dotnet-new-spa-files.png)

If you're using JetBrains Rider you can install npm packages by opening `package.json` and run the "npm install" tooltip on the **bottom right**. In VS Code you'll need to run `npm install` from the command-line.

### ServiceStackVS VS.NET Templates Updated

The VS.NET Templates inside [ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) have been updated to use the latest .NET Framework templates which you can continue to use to [create new projects within VS.NET](/create-your-first-webservice). For all other IDEs and non-Windows Operating Systems you can use the cross-platform `dotnet-new` tooling to create new .NET Core 2.0 Projects. 

In future we'll also be looking at making these templates available in .NET Core's `dotnet new` template system.

### .NET Core 2.0 TypeScript Webpack Templates
 
There's a template for each of the most popular Single Page Application JavaScript frameworks, including a new [Angular 5](https://angular.io) template built and managed using Angular's new [angular-cli](https://cli.angular.io) tooling. All other SPA Templates (inc. Angluar 4) utilize a modernized Webpack build system, pre-configured with npm scripts to perform debug/production and live watched builds and testing. The included [gulpfile.js](https://github.com/NetCoreTemplates/vue-spa/blob/master/MyApp/gulpfile.js) wrapper allows each npm script to be run without a command-line using VS.NET's built-in Task Runner Explorer GUI. 

### TypeScript

All Templates are configured to use TypeScript which offers both compile-time safety productivity and maintainability as well as access to JavaScript's latest ES6/7 features. They're also configured with Typed DTOs using ServiceStack's [TypeScript Add Reference](/typescript-add-servicestack-reference) and generic [servicestack-client](https://github.com/ServiceStack/servicestack-client) to provide an end-to-end Typed API to call your Services which can be updated with the npm (or Gulp) `dtos` script. All templates are configured to use the TypeScript `JsonServiceClient` with concrete Type Definitions whilst we've configured Angular 5 template to use ambient TypeScript declarations with Angular's built-in Rx-enabled HTTP Client as it's often preferred to utilize Angular's built-in dependencies. 

#### Angular 5 HTTP Client

The ambient TypeScript interfaces are still leveraged to enable a Typed API whilst the `createUrl(route,args)` helper lets you use your APIs Route definitions (emitted in comments above each DTO) to provide a nice UX for making API calls using Angular's HTTP Client:

```ts
import { createUrl } from 'servicestack-client';
...

this.http.get<HelloResponse>(createUrl('/hello/{Name}', { name })).subscribe(r => {
    this.result = r.result;
});
```

All Single Page App Templates are available for .NET Core 2.0 and ASP.NET Framework projects which you can preview and create using the template names below:

<table>
<tr>
    <th>.NET Core 2.0</th>
    <th>.NET Framework</th>
    <th>Single Page App Templates</th>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/angular-cli">angular-cli</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/angular-cli">angular-cli-netfx</a></td>
    <td align="center">
        <h3>Angular 5 CLI Bootstrap Template</h3>
        <a href="http://angular-cli.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/angular-cli.png" width="500" /></a>
        <p><a href="http://angular-cli.web-templates.io">angular-cli.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/angular-lite-spa">angular-lite-spa</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/angular-lite-spa-netfx">angular-lite-spa-netfx</a></td>
    <td align="center">
        <h3>Angular 4 Material Design Lite Template</h3>
        <a href="http://angular-lite-spa.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/angular-lite-spa.png" width="500" /></a>
        <p><a href="http://angular-lite-spa.web-templates.io">angular-lite-spa.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/react-spa">react-spa</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/react-spa-netfx">react-spa-netfx</a></td>
    <td align="center">
        <h3>React 16 Webpack Bootstrap Template</h3>
        <a href="http://react-spa.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/react-spa.png" width="500" /></a>
        <p><a href="http://react-spa.web-templates.io">react-spa.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/vue-spa">vue-spa</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/vue-spa-netfx">vue-spa-netfx</a></td>
    <td align="center">
        <h3>Vue 2.5 Webpack Bootstrap Template</h3>
        <a href="http://vue-spa.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/vue-spa.png" width="500" /></a>
        <p><a href="http://vue-spa.web-templates.io">vue-spa.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/aurelia-spa">aurelia-spa</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/aurelia-spa-netfx">aurelia-spa-netfx</a></td>
    <td align="center">
        <h3>Aurelia Webpack Bootstrap Template</h3>
        <a href="http://aurelia-spa.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/aurelia-spa.png" width="500" /></a>
        <p><a href="http://aurelia-spa.web-templates.io">aurelia-spa.web-templates.io</a></p>
    </td>
</tr>
</table>

### Optimal Dev Workflow with Hot Reloading

The Webpack templates have been updated to utilize [Webpack's DllPlugin](https://robertknight.github.io/posts/webpack-dll-plugins/) which split your App's TypeScript source code from its vendor dependencies for faster incremental build times. With the improved iteration times our recommendation for development is to run a normal Webpack watch using the `dev` npm (or Gulp) script:
 
    $ npm run dev

Which will watch and re-compile your App for any changes. These new templates also include a new hot-reload feature which works similar to ServiceStack Templates hot-reloading where in **DebugMode** it will long poll the server for any modified files in `/wwwroot` and automatically refresh the page. This provides a hot-reload alternative to `npm run dev-server` to run a [Webpack Dev Server proxy][16] on port http://localhost:3000 
 
### Configured with ServiceStack Templates
 
Hot Reloading works by leveraging [ServiceStack Templates](http://templates.servicestack.net) which works nicely with Webpack's generated `index.html` where we're able to evaluate Template Expressions whilst rendering the SPA home page. To enable Hot Reloading support the projects [include the expression](https://github.com/NetCoreTemplates/vue-spa/blob/0c13183b6a5ae20564f650e50d29b9d4e36cbd0c/MyApp/index.template.ejs#L8) below:

```html
{{ ifDebug | select: <script>{ '/js/hot-fileloader.js' | includeFile }</script> }}
```

Which renders the contents of [/js/hot-fileloader.js](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/js/hot-fileloader.js) when running the Web App during development.

Although optional, ServiceStack Templates is useful whenever you need to render server logic in the SPA home page, e.g:

```html 
<div>Copyright &copy; {{ now | dateFormat('yyyy') }}</div>
```

Will be evaluated on the server and emit the expected:
 
    Copyright © 2017

### Deployments

You can run the `publish` npm (or Gulp) script to package your App for deployment:

    npm run publish
 
Which generates a production Webpack client build and `dotnet publish` release Server build to package your App ready for an XCOPY, rsync or MSDeploy deployment. We used [rsync and supervisord to deploy](http://templates.servicestack.net/docs/deploying-web-apps) each packaged template to an Ubuntu Server

    http://<template-name>.web-templates.io

### /wwwroot WebRoot Path for .NET Framework Templates

To simplify migration efforts of ServiceStack projects between .NET Core and .NET Framework, all SPA and Website Templates are configured with .NET Core's convention of using `/wwwroot` for its public WebRoot Path. The 2 adjustments needed to support this was telling ServiceStack to use the `/wwwroot` path in AppHost:

```csharp
SetConfig(new HostConfig {
    WebHostPhysicalPath = MapProjectPath("~/wwwroot"),
});
```

Then instructing MSBuild to include all `wwwroot\**\*` files when publishing the project using MSWebDeploy which is contained in the [Properties/PublishProfiles/PublishToIIS.pubxml](https://github.com/NetFrameworkTemplates/vue-spa-netfx/blob/master/MyApp/Properties/PublishProfiles/PublishToIIS.pubxml) of each project:

```xml
<PropertyGroup>
    <CopyAllFilesToSingleFolderForMSDeployDependsOn>
        IncludeFiles;
        $(CopyAllFilesToSingleFolderForMSDeployDependsOn);
    </CopyAllFilesToSingleFolderForMSDeployDependsOn>
</PropertyGroup>
<Target Name="IncludeFiles">
    <ItemGroup>
        <PublishFiles Include="wwwroot\**\*" />
        <FilesForPackagingFromProject Include="@(PublishFiles)">
        <DestinationRelativePath>wwwroot\%(RecursiveDir)%(Filename)%(Extension)</DestinationRelativePath>
        </FilesForPackagingFromProject>
    </ItemGroup>
</Target>
```

### Website Templates

We also include starting templates for 3 different technologies you can use in ServiceStack to develop Server Generated Websites with. 

The `mvc` template is the most different between .NET Core and ASP.NET as the .NET Core MVC and ASP.NET MVC 5 are completely different implementations. With `mvc` ServiceStack is configured within the same .NET Core pipeline and shares the same request pipeline and "route namespace" but in ASP.NET MVC 5 ServiceStack is hosted at `/api` Custom Path. Use MVC if you prefer to create different Controllers and View Models for your Website disconnected from your HTTP APIs or if you prefer to generate server HTML validation errors within MVC Controllers.

The `razor` Template is configured to develop Websites using [ServiceStack.Razor](http://razor.servicestack.net) for developing server-generated Websites using Razor without MVC Controllers which lets you create Content Razor Pages that can be called directly or View Pages for generating the HTML View for existing Services. The source code for .NET Core and ASP.NET Framework projects are surprisingly nearly identical despite being completely different implementations with the .NET Core version being retrofitted on top of .NET Core MVC Views. Use `razor` templates if you like Razor and prefer the [API First Development model](/releases/v4.5.14#end-user-language-with-low-roi) or plan on developing Websites for both .NET Core and ASP.NET and would like to be easily able to migrate between them.

The `templates` Project Template is configured to develop Websites using [ServiceStack Templates](http://templates.servicestack.net), a simpler and cleaner alternative to Razor that lets you utilize simple Template Expressions for evaluating Server logic in `.html` pages. Templates doesn't require any precompilation, is easier to learn and more intuitive for non-programmers that's more suitable for a [number of use-cases](http://templates.servicestack.net/usecases/). Use templates if you want an [alternative to Razor](/releases/v4.5.14#why-templates) syntax and its heavy machinery required to support it.

#### Hot Reloading

Both `razor` and `templates` project enjoy Hot Reloading where a long poll is used during development to detect and reload changes in the current Template Page or static files in `/wwwroot`.

<table>
<tr>
    <th>.NET Core 2.0</th>
    <th>.NET Framework</th>
    <th>Single Page App Templates</th>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/mvc">mvc</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/mvc-netfx">mvc-netfx</a></td>
    <td align="center">
        <h3>MVC Bootstrap Template</h3>
        <a href="http://mvc.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/mvc.png" width="500" /></a>
        <p><a href="http://mvc.web-templates.io">mvc.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/razor">razor</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/razor-netfx">razor-netfx</a></td>
    <td align="center">
        <h3>ServiceStack.Razor Bootstrap Template</h3>
        <a href="http://razor.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/razor.png" width="500" /></a>
        <p><a href="http://razor.web-templates.io">razor.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/templates">templates</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/templates-netfx">templates-netfx</a></td>
    <td align="center">
        <h3>ServiceStack Templates Bootstrap Template</h3>
        <a href="http://templates.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/templates.png" width="500" /></a>
        <p><a href="http://templates.web-templates.io">templates.web-templates.io</a></p>
    </td>
</tr>
</table>

### Empty Web and SelfHost Templates

Those who prefer starting from an Empty slate can use the `web` template to create the minimal configuration for a Web Application whilst the `selfhost` template can be used to develop Self-Hosting Console Apps. Both templates still follow our recommended phyiscal project layout but are configured with the minimum dependencies, e.g. the `selfhost` Console App just has a dependency on [Microsoft.AspNetCore.Server.Kestrel and ServiceStack](https://github.com/NetCoreTemplates/selfhost/blob/f11b25e80752d1fee96ac904a8df07fb150ee746/MyApp/MyApp.csproj#L11-L12), in contrast most templates have a dependency on the uber `Microsoft.AspNetCore.All` meta package.

<table>
<tr>
    <th>.NET Core 2.0</th>
    <th>.NET Framework</th>
    <th>Empty Project Templates</th>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/web">web</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/web-netfx">web-netfx</a></td>
    <td align="center">
        <h3>Empty Web Template</h3>
        <a href="http://web.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/web.png" width="500" /></a>
        <p><a href="http://web.web-templates.io">web.web-templates.io</a></p>
    </td>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/selfhost">selfhost</a></td>
    <td><a href="https://github.com/NetFrameworkTemplates/selfhost-netfx">selfhost-netfx</a></td>
    <td align="center">
        <h3>Empty SelfHost Console App Template</h3>
        <a href="http://selfhost.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/selfhost.png" width="500" /></a>
        <p><a href="http://selfhost.web-templates.io">selfhost.web-templates.io</a></p>
    </td>
</tr>
</table>

### .NET Core 2.0 ServiceStack WebApp Template

The only .NET Core 2.0 project template not to have a .NET Framework equivalent is [templates-webapp](https://github.com/NetCoreTemplates/templates-webapp) as it's a pre-built .NET Core 2.0 App that dramatically simplifies .NET Wep App development by allowing you to develop Websites and APIs on-the-fly instantly without any compilation.

<table>
<tr>
    <th>.NET Core 2.0</th>
    <th>ServiceStack Templates WebApp</th>
</tr>
<tr>
    <td><a href="https://github.com/NetCoreTemplates/templates-webapp">templates-webapp</a></td>
    <td align="center">
        <p><a href="http://templates-webapp.web-templates.io">templates-webapp.web-templates.io</a></p>
        <a href="http://templates-webapp.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/templates-webapp.png" width="500" /></a>
    </td>
</tr>
</table>

See [templates.servicestack.net/docs/web-apps](http://templates.servicestack.net/docs/web-apps) to learn the different use-cases made possible with Web Apps.

### .NET Framework Templates

Likewise there are 2 .NET Framework Templates without .NET Core 2.0 equivalents as they contain Windows-only .NET Framework dependencies. This includes our React Desktop Template which supports packaging your Web App into 4 different ASP.NET, Winforms, OSX Cocoa and cross-platform Console App Hosts:

<table>
<tr>
    <th>.NET Framework</th>
    <th>React Desktop Apps Template</th>
</tr>
<tr>
    <td><a href="https://github.com/NetFrameworkTemplates/react-desktop-apps-netfx">react-desktop-apps-netfx</a></td>
    <td align="center">
        <p><a href="http://react-desktop-apps-netfx.web-templates.io">react-desktop-apps-netfx.web-templates.io</a></p>
        <a href="http://react-desktop-apps-netfx.web-templates.io"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/csharp-templates/react-desktop-apps-netfx.png" width="500" /></a>
    </td>
</tr>
</table>

In future we also intend on developing a Desktop Apps template for .NET Core 2.0 based on Electron.

### Windows Service Template

You can use [winservice-netfx](https://github.com/NetFrameworkTemplates/winservice-netfx) to create a Windows Service but as this requires Visual Studio it's faster to continie creating new Windows Service projects within VS.NET using ServiceStackVS VS.NET Project Template.

## .NET Core Apps Upgraded

As we've now stabilized on .NET Core 2.0 and .NET Standard 2.0 as our long-term supported .NET Core platform, we've upgraded all our existing .NET Core 1.x projects to .NET Core 2.0 and ServiceStack v5, including all [.NET Core Live Demos](https://github.com/NetCoreApps).

### Multi-stage Docker Builds

We've also updated the [.NET Core Apps deployed using Docker](/deploy-netcore-docker-aws-ecs) to use the ASP.NET Team's [recommended multi-stage Docker Builds](https://docs.microsoft.com/en-us/dotnet/core/docker/building-net-docker-images#your-first-aspnet-core-docker-app) where the App is built inside an `aspnetcore-build` Docker container with it's published output copied inside a new `aspnetcore` runtime Docker container:

```docker
FROM microsoft/aspnetcore-build:2.0 AS build-env
COPY src /app
WORKDIR /app

RUN dotnet restore --configfile ../NuGet.Config
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/aspnetcore:2.0
WORKDIR /app
COPY --from=build-env /app/Chat/out .
ENV ASPNETCORE_URLS http://*:5000
ENTRYPOINT ["dotnet", "Chat.dll"]
```

The smaller footprint required by the `aspnetcore` runtime reduced the footprint of [.NET Core Chat](https://github.com/NetCoreApps/Chat) from **567MB** to **126MB** whilst continuing to run flawlessly in AWS ECS at: http://chat.netcore.io

### ServiceStack WebApps Updated

Likewise all [.NET Core ServiceStack WebApps](https://github.com/NetCoreWebApps) including the pre-built [/web](https://github.com/NetCoreWebApps/Web) binaries have been updated to use ServiceStack v5.

Changes from the previous version is switched to using the default `WebHost.CreateDefaultBuilder()` builder to bootstrap WebApp's which will let you use `ASPNETCORE_URLS` to specify which URL and port to bind on which should simplify deployment configurations.

The `ASPNETCORE_ENVIRONMENT` Environment variable can also be used to specify to run WebApp's in `Production` mode. If preferred you can use the existing `bind`, `port` and `debug` options in your `web.settings` to override the default configuration. 

___TODO___

### .NET Core IAppSettings Adapter

Most .NET Core Templates are also configured to use the new `NetCoreAppSettings` adapter to utilize .NET Core's new `IConfiguration` config model in ServiceStack by initializing your `AppHost` with .NET Core's pre-configured `IConfiguration` that's injected into the [Startup.cs](https://github.com/NetCoreTemplates/vue-spa/blob/master/MyApp/Startup.cs) constructor, e.g:

```csharp
public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseServiceStack(new AppHost {
            AppSettings = new NetCoreAppSettings(Configuration)
        });
    }
}
```

This will let you use **appsettings.json** and .NET Core's other Configuration Sources in ServiceStack under the `IAppSettings` API which works as you'd expect where you can get both primitive values and complex Types with `Get<T>`, e.g:

```csharp
bool debug = AppSettings.Get<bool>("DebugMode", false);
MyConfig myConfig = AppSettings.Get<MyConfig>();
IList<string> fbScopes = AppSettings.GetList("oauth.facebook.Permissions");
List<string>  ghScopes = AppSettings.Get<List<string>>("oauth.github.Scopes");
```

But instead of a single string value, you'll need to use the appropriate JSON data type, e.g:

```json
{
    "DebugMode": true,
    "MyConfig": {
        "Name": "Kurt",
        "Age": 27
    },
    "oauth.facebook.Permissions": ["email"],
    "oauth.github.Scopes": ["user"]
}
```

### New Password Hashing implementation

We're now using the same [PBKDF2](https://en.wikipedia.org/wiki/PBKDF2) password hashing algorithm ASP.NET Identity v3 uses to hash passwords by default for both new users and successful authentication logins where their password will be re-hashed with the new implementation automatically. 

This also means if you wanted to switch, you'll be able to import ASP.NET Identity v3 User Accounts and their Password Hashes into ServiceStack.Auth's `UserAuth` tables and vice-versa.

If preferred you can revert to using the existing `SaltedHash` implementation with:

```csharp
SetConfig(new HostConfig { 
    UseSaltedHash = true
});
```

This also supports "downgrading" passwords that were hashed with the new `IPasswordHasher` provider where it will revert to using the older/weaker `SaltedHash` implementation on successful authentication.

#### Override Password Hashing Strength

The new [PasswordHasher](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Auth/PasswordHasher.cs) implementation can be made stronger or weaker by adjusting the iteration count (default 10000), e.g:

```csharp
container.Register<IPasswordHasher>(new PasswordHasher(1000));
```

#### Versionable Password Hashing

The new [IPasswordHasher](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/Auth/IPasswordHasher.cs) interface supports versioning Password Hashing implementations and rehsashing:

```csharp
public interface IPasswordHasher
{
    // First byte marker used to specify the format used. The default implementation uses format:
    // { 0x01, prf (UInt32), iter count (UInt32), salt length (UInt32), salt, subkey }
    byte Version { get; }

    // Returns a boolean indicating whether the providedPassword matches the hashedPassword
    // The needsRehash out parameter indicates whether the password should be re-hashed.
    bool VerifyPassword(string hashedPassword, string providedPassword, out bool needsRehash);

    // Returns a hashed representation of the supplied password
    string HashPassword(string password);
}
```

This is implemented in all ServiceStack Auth Repositories which have switched to use the ServiceStack APIs for verifying passwords:

```csharp
if (userAuth.VerifyPassword(password, out var needsRehash))
{
    this.RecordSuccessfulLogin(userAuth, needsRehash, password);
    return true;
}
```

If you're using a Custom Auth Repository it will also need to updated to use the new APIs, please refer to [OrmLiteAuthRepository](https://github.com/ServiceStack/ServiceStack/blob/bed1d900de93f889cca05299df4c33a04b7ad7a7/src/ServiceStack.Server/Auth/OrmLiteAuthRepository.cs#L325-L359) for a complete example.

#### Fallback PasswordHashers

You can use the new `Config.FallbackPasswordHashers` when migrating to a new Password Hashing algorithm by registering older Password Hashing implementations that were previously used to hash Users passwords. Failed password verifications fallback to check to see if the password was hashed with 
any of the registered `FallbackPasswordHashers`, if any are valid the password attempt will succeed and password will get re-hashed with the current registered `IPasswordHasher`.

#### Digest Auth Hashes only created when needed

We're also only maintaining Digest Auth Hashes if the DigestAuthProvider is registered. If you ever intend to support Digest access authentication1 in future but don't want to register the DigestAuthProvider just yet, you can force ServiceStack to maintain Digest Auth Hashes with:

```csharp
new AuthFeature {
    CreateDigestAuthHashes = true
}
```

Users that don't have Digest Auth Hashes will require logging in again in order to populate it. If you don't intend to use Digest Auth you can clear the `DigestHa1Hash` column in your `UserAuth` table which is otherwise unused.


## V5 Changes and Migration Notes

Most ServiceStack releases are in-place upgrades containing new features and enhancements with very minimal disruption to existing code-bases. Our Major versions are very few and far between which we use to implement any structural changes we've been holding off on which may require external project or server configuration changes, e.g. Our last v4.5.0 major version was our move from .NET v4.0 to .NET v4.5.

The theme for ServiceStack v5 is to restructure the code-base to lay the ground work for first-class support for .NET Core. Whilst some .NET Framework only implementations have moved to different projects there should still be minimal user-facing source code changes other than most deprecated APIs and dead-code that has been identified having been removed, as such the primary task before upgrading to V5 will be to move off deprecated APIs. Most `[Obsolete]` APIs specify which APIs to move to in their deprecated messages which you can find in your build warning messages.

Other major structural changes include:

 - Upgraded all NuGet packages to a **5.0.0** major version
 - Removed all PCL builds and upgraded all client libraries and .NET Standard builds to target **.NET Standard 2.0**
 - Merged `.Core` packages into main ServiceStack packages which now contain `net45` and `netstandard2.0` builds
 - Strong-named all .NET v4.5 builds which also only have Strong named dependencies
 - Upgrade **ServiceStack.RabbitMQ** to use the latest **RabbitMQ.Client** v5.0.1 NuGet dependency which requires a **.NET v4.5.1** minimum
 - Upgraded to the latest Fluent Validation 7.2

Migration notes:

 - Move off deprecated APIs before upgrading to v5
 - If you're using an outdated NuGet v2 client you'll need to upgrade to NuGet v3
 - If you were using `.Core` or `.Signed` NuGet packages you'll need to strip the suffixes to use the main NuGet packages. 

If you're using the new MSBuild project format using a `5.*` wildcard version will allow you to upgrade to the latest release of ServiceStack with a NuGet restore.

### Strong Naming Notes

All .NET 4.5 builds are now strong named with the `servicestack.snk` key that's in the [/src](https://github.com/ServiceStack/ServiceStack/tree/master/src) of each repo. **ServiceStack.Logging.Elmah** is the only .NET 4.5 dll not signed because it's elmah.corelibrary dependency is unsigned. .NET Standard builds remain unsigned as it would've impeded building ServiceStack libraries on non Windows platforms which require msbuild. 

### PCL Client Library Notes

The PCL client builds have been rewritten to use **.NET Standard 2.0** APIs. We've added new test suites for **Xamarin.IOS** and **Xamarin.Android** to ensure the new clients run smoothly. We've also included UWP in our .NET Standard 2.0 client test suites but this requires upgrading to Windows 10 Fall Creators Update. 

There's no longer any Silverlight or Windows8 App builds which end at v4.5.14 on NuGet. v5 remains wire-compatible with existing versions so if you're still maintaining legacy Silverlight or Windows8 Apps your v4.5.14 clients can consume v5 Services as before.

The new .NET Standard 2.0 clients are much nicer to use then the previous PCL clients which needed to utilize the [PCL Bait and Switch trick](https://log.paulbetts.org/the-bait-and-switch-pcl-trick/) to overcome the limitations in PCL's limited API Surface by delegating to platform specific implementations - required for each supported platform. The PCL limitations also meant you couldn't reference `ServiceStack.Text` package on its own as it needed the platform specific implementations in `ServiceStack.Client` which you would need to manually wire-up in iOS by hard-coding `IosPclExportClient.Configure();`.

By contrast .NET Standard 2.0 builds can be treated like regular .dlls. The existing PCL hacks, implementation-specific builds, polyfills and wrappers have all been removed. API and Code-reuse is also much broader as previously only the PCL compatible surface area could be shared amongst multiple projects, now the entire API surface is available.

### .NET Standard Client packages for .NET Framework Projects

One difference from before was previously `ServiceStack.Interfaces.dll` was a single PCL .dll referenced by all .NET v4.5, .NET Core and PCL projects. It now contains .NET 4.5 and .NET Standard 2.0 build like the other packages. One consequence of this is that you wont be able to have binary coupling of your Server .NET Core `ServiceModel.dll` inside .NET Framework clients despite both referencing the same `ServiceStack.Client` NuGet package. For this reason we're continuing to publish `.Core` versions of the client packages:

 - ServiceStack.Text.Core
 - ServiceStack.Interfaces.Core
 - ServiceStack.Client.Core
 - ServiceStack.HttpClient.Core

These contain the **exact same** .NET Standard 2.0 builds included in the main **ServiceStack.Client** packages but by having them in separate packages, you can force **.NET v4.6.1+** Framework projects to use .NET Standard versions which will let you share your server .NET Standard 2.0 `ServiceModel.dll` in **.NET v4.6.1+** clients. Alternatively you can avoid any binary coupling by using [Add ServiceStack Reference](/add-servicestack-reference) to import the source code of your Typed DTOs.

This doesn't work the other way around where ServiceStack .NET Framework projects still cannot reference .NET Standard only projects. In order to create projects that can be shared from both .NET Core and ASP.NET Framework projects they'll need to target both frameworks, e.g:

```xml
<PropertyGroup>
    <TargetFrameworks>netstandard2.0;net46</TargetFrameworks>
</PropertyGroup>
```

### .NET Framework specific features

A goal for the v5 restructure was to restrict the primary `ServiceStack.dll` and NuGet Package to contain core functionality that's applicable to all ASP.NET, HttpListener and .NET Core Hosts. To this end we've moved ASP.NET and HttpListener specific features to different packages, but remain source-compatible as they continue using the same Types and namespaces.

In addition both .NET Framework and .NET Core ServiceStack AppHost's are initialized with the same minimal feature set. The .NET Framework SOAP Support, Mini Profiler and Markdown Razor format features are now opt-in, if needed they'll need to be explicitly added with:

```csharp
Plugins.Add(new SoapFormat());
Plugins.Add(new MarkdownFormat());       // In ServiceStack.Razor
Plugins.Add(new MiniProfilerFeature());  // In ServiceStack.NetFramework
```

### ServiceStack.Razor

The MVC HTML Helpers for ASP.NET ServiceStack.Razor has been moved to the **ServiceStack.Razor** project. This should be a transparent change it needed ServiceStack.Razor to make use of them.

[Markdown Razor](/markdown-razor) and its `MarkdownFormat` was also moved to ServiceStack.Razor as they were built on older Code DOM technology which isn't available on .NET Core which had its feature-set stripped down to bare Markdown functionality.

#### ServiceStack Templates

If you were using Markdown Razor we recommend using the superior, cleaner, faster and more flexible alternative in [ServiceStack Templates](http://templates.servicestack.net/) which easily supports Markdown Razor's most redeeming feature of being able to mix .NET in Markdown by utilizing its [Transformers](http://templates.servicestack.net/docs/transformers) feature.

#### Markdown Config

The new `MarkdownConfig` API lets you specify which Markdown implementation to use in Razor and ServiceStack Template Markdown partials. By default it uses the built-in fast and light-weight MarkdownDeep implementation:

```csharp
MarkdownConfig.Transformer = new MarkdownDeep.Markdown();
```

Alternative Markdown implementations can be used by providing an adapter for the `IMarkdownTransformer` interface:

```csharp
public interface IMarkdownTransformer
{
    string Transform(string markdown);
}
```

### ServiceStack.NetFramework

The remaining .NET Framework and HttpListener specific features that were previously in `ServiceStack.dll` have moved to `ServiceStack.NetFramework` - a new project and NuGet package for maintaining .NET Framework only features.

    PM> Install-Package ServiceStack.NetFramework
 
It currently contains `MiniProfiler` and `SmartThreadPool` which is an internal ThreadPool originally imported to provide a faster alternative to .NET's built-in ThreadPool. The fastest self-hosting option is now to use a .NET Core App instead of .NET's HttpListener which also benefits from running flawlessly cross-platform so it's our recommended option for new projects when you don't have requirements that need the .NET Framework. 

The primary result of this is that now `AppSelfHostBase` is implemented on top of `AppHostHttpListenerPoolBase`. To have your HttpListener AppHost go back to utilizing a SmartThreadPool implementation, change your `AppHost` to inherit either `SmartThreadPool.AppSelfHostBase` or `AppHostHttpListenerSmartPoolBase`, e.g:

```csharp
public class AppHost : SmartThreadPool.AppSelfHostBase {}
public class AppHost : AppHostHttpListenerSmartPoolBase {}
```

### Validation

Our internal implementation of [FluentValidation](https://github.com/JeremySkinner/FluentValidation) has been upgraded to the latest 7.2 version which will let you take advantage of new features like implementing [Custom Validators](https://github.com/JeremySkinner/FluentValidation/wiki/e.-Custom-Validators#using-a-custom-validator), e.g:

```csharp
public class CustomValidationValidator : AbstractValidator<CustomValidation>
{
    public CustomValidationValidator()
    {
        RuleFor(request => request.Code).NotEmpty();
        RuleFor(request => request)
            .Custom((request, contex) => {
                if (request.Code?.StartsWith("A") != true)
                {
                    var propertyName = contex.ParentContext.PropertyChain.BuildPropertyName("Code");
                    contex.AddFailure(new ValidationFailure(propertyName, error:"Incorrect prefix") {
                        ErrorCode = "NotFound"
                    });
                }
            });
    }
}
```

#### Validators in ServiceAssemblies auto-wired by default

The `ValidationFeature` plugin now scans and auto-wires all validators in the `AppHost.ServiceAssemblies` that's specified in your AppHost constructor so you'll no longer need to manually register validators maintained in your `ServiceInterface.dll` project:

```csharp
//container.RegisterValidators(typeof(UserValidator).Assembly);
```

Although unlikely, you can disale auto-wiring of validators in Service Assemblies with:

```csharp
Plugins.Add(new ValidationFeature {
    ScanAppHostAssemblies = false
});
```

### Expanded Async Support

To pre-emptively support .NET Core when they [disable Sync Response writes by default](https://github.com/aspnet/Announcements/issues/252) in future version, we've rewritten our internal implementations to write to Responses asynchronously. 

We've also introduced new Async filter equivalents to match the existing sync filters available. It's highly unlikely the classic ASP.NET Framework will ever disable sync writes, but if you're on .NET Core you may want to consider switching to use the newer async API equivalents on [IAppHost](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/IAppHost.cs) below: 

    GlobalRequestFiltersAsync
    GlobalResponseFiltersAsync
    GatewayRequestFiltersAsync
    GatewayResponseFiltersAsync
    GlobalMessageRequestFiltersAsync
    GlobalMessageResponseFiltersAsync

#### Async Attribute Filters

Whilst you can inherit the new `RequestFilterAsyncAttribute` or `ResponseFilterAsyncAttribute` base classes to implement Filter Attributes which call any Async APIs.

All async equivalents follow the same [Order of Operations](/order-of-operations) and are executed immediately after any registered sync filters with the same priority.

#### Async Request and Response Converters

As they're not commonly used in normal ServiceStack Apps, the `RequestConverters` and `ResponseConverters` were converted to an Async API. 

#### Async ContentTypes Formats

There's also new async registration APIs for Content-Type Formats which perform Async I/O, most serialization formats don't except for our HTML View Engines which can perform Async I/O when rendering views, so they were also changed to use the new `RegisterAsync` APIs:

```csharp
appHost.ContentTypes.RegisterAsync(MimeTypes.Html, SerializeToStreamAsync, null);
appHost.ContentTypes.RegisterAsync(MimeTypes.JsonReport, SerializeToStreamAsync, null);
appHost.ContentTypes.RegisterAsync(MimeTypes.MarkdownText, SerializeToStreamAsync, null);
```

#### Async HttpWebRequest Service Clients

The Async implementation of the `HttpWebRequest` based Service Clients was rewritten to use the newer .NET 4.5 Async APIs as the older APM APIs were found to have some async request hanging issues in the .NET Standard 2.0 version of Xamarin.iOS.

### .NET Reflection APIs

One of the primary areas where .NET Standard and .NET Framework source code differed was in Reflection APIs. To work around this we created common wrapper extension methods around the most popular Reflection APIs for both platforms. These wrappers have now all been deprecated as they're no longer needed now that .NET Standard 2.0 has added support .NET's standard Reflection APIs. 

### Routes with Custom Rules

The new `Matches` property on `[Route]` and `[FallbackRoute]` attributes lets you specify an additional custom Rule that requests need to match. We use this feature in all SPA projects to specify that the `[FallbackRoute]` should only return the SPA `index.html` for unmatched requests which explicitly accepts HTML, i.e:

```csharp
[FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
public class FallbackForClientRoutes
{
    public string PathInfo { get; set; }
}
```

This works by matching the `AcceptsHtml` built-in `RequestRules` below where the Route will only match the Request if it includes the explicit `text/html` MimeType in the HTTP Request `Accept` Header. The `AcceptsHtml` rule prevents the home page from being returned for missing resource requests like **favicon** which will return a `404` instead.

The implementation of all built-in Request Rules:

```csharp
SetConfig(new HostConfig {
  RequestRules = {
    {"AcceptsHtml", req => req.Accept?.IndexOf(MimeTypes.Html, StringComparison.Ordinal) >= 0 },
    {"AcceptsJson", req => req.Accept?.IndexOf(MimeTypes.Json, StringComparison.Ordinal) >= 0 },
    {"AcceptsXml", req => req.Accept?.IndexOf(MimeTypes.Xml, StringComparison.Ordinal) >= 0 },
    {"AcceptsJsv", req => req.Accept?.IndexOf(MimeTypes.Jsv, StringComparison.Ordinal) >= 0 },
    {"AcceptsCsv", req => req.Accept?.IndexOf(MimeTypes.Csv, StringComparison.Ordinal) >= 0 },
    {"IsAuthenticated", req => req.IsAuthenticated() },
    {"IsMobile", req => Instance.IsMobileRegex.IsMatch(req.UserAgent) },
    {"{int}/**", req => int.TryParse(req.PathInfo.Substring(1).LeftPart('/'), out _) },
    {"path/{int}/**", req => {
        var afterFirst = req.PathInfo.Substring(1).RightPart('/');
        return !string.IsNullOrEmpty(afterFirst) && int.TryParse(afterFirst.LeftPart('/'), out _);
    }},
    {"**/{int}", req => int.TryParse(req.PathInfo.LastRightPart('/'), out _) },
    {"**/{int}/path", req => {
        var beforeLast = req.PathInfo.LastLeftPart('/');
        return !string.IsNullOrEmpty(beforeLast) && int.TryParse(beforeLast.LastRightPart('/'), out _);
    }},
 }
})
```

Routes that contain a `Matches` rule have a higher preference then Routes without which lets us define multiple idential routes to call different Service depending on whether the Path Segment is an integer or not, e.g:

```csharp
// matches /users/1
[Route("/users/{Id}", Matches = "**/{int}")]
public class GetUser
{
    public int Id { get; set; }
}

// matches /users/username
[Route("/users/{Slug}")]
public class GetUserBySlug
{
    public string Slug { get; set; }
}
```

Other examples using the `{int}` Request Rules:

```csharp
// matches /1/profile
[Route("/{UserId}/profile", Matches = @"{int}/**")]
public class GetProfile { ... }

// matches /username/profile
[Route("/{Slug}/profile")]
public class GetProfileBySlug { ... }

// matches /users/1/profile/avatar
[Route("/users/{UserId}/profile/avatar", Matches = @"path/{int}/**")]
public class GetProfileAvatar { ... }

// matches /users/username/profile/avatar
[Route("/users/{Slug}/profile/avatar")]
public class GetProfileAvatarBySlug { ... }
```

Another popular use-case is to call different services depending on whether a Request is from an Authenticated User or not:

```csharp
[Route("/feed", Matches = "IsAuthenticated")]
public class ViewCustomizedUserFeed { ... }

[Route("/feed")]
public class ViewPublicFeed { ... }
```

This can also be used to call different Services depending if the Request is from a Mobile browser or not:

```csharp
[Route("/search", Matches = "IsMobile")]
public class MobileSearch { ... }

[Route("/search")]
public class DesktopSearch { ... }
```

Instead of matching on a pre-configured RequestRule you can specify a Regular Expression instead using the format:

    {Property} =~ {RegEx}

Where `{Property}` is an `IHttpRequest` propery, e.g:

```csharp
[Route("/users/{Id}", Matches = @"PathInfo =~ \/[0-9]+$")]
public class GetUser { ... }
```

An exact match takes the format:

    {Property} = {Value}

Which you could use to provide a tailored feed for specific clients:

```csharp
[Route("/feed", Matches = @"UserAgent = specific-client")]
public class CustomFeedView { ... }
```

### View Pages support for ServiceStack Templates

ServiceStack Templates gains support for the last missing feature from ServiceStack.Razor with its new View Pages support which lets you use `.html` Template Pages to render the HTML for Services Responses. 

It works similarly to Razor ViewPages where it uses first matching View Page where the Response DTO is injected as the Model property. The View Pages can be in any folder within the `/Views` folder using the format `{PageName}.html` where `PageName` can be either the **Request DTO** or **Response DTO** Name but all page names within the `/Views` folder need to be unique.

Just like ServiceStack.Razor you can also specify to use different Views or Layouts by returning a custom `HttpResult`, e.g:

```csharp
public object Any(MyRequest request)
{
    ...
    return new HttpResult(response)
    {
        View = "CustomPage",
        Template = "_custom-layout",
    };
}
```

Or add the `[ClientCanSwapTemplates]` Request Filter attribute to allow clients to specify which View and Template to use via the query string, e.g: `?View=CustomPage&Template=_custom-layout`.

Additional examples of dynamically specifying the View and Template are in [TemplateViewPagesTests](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/TemplateTests/TemplateViewPagesTests.cs).

#### Cascading Layouts

One difference from Razor is that it uses a cascading `_layout.html` instead of `/Views/Shared/_Layout.cshtml`. 

So if your view page was in:

    /Views/dir/MyRequest.html

It will use the closest `_layout.html` it can find starting from:

    /Views/dir/_layout.html
    /Views/_layout.html
    /_layout.html


Commits:
ServiceStack
Validation


JWT 
RuntimeConfig APIs… for multi tenancy
GetRuntimeConfig(nameof(AuthKey))
GetRuntimeConfig(nameof(FallbackAuthKeys))
GetRuntimeConfig(nameof(FallbackPublicKeys))
GetRuntimeConfig(nameof(PrivateKey))
GetRuntimeConfig(nameof(PublicKey))

IUserSessionSource
https://stackoverflow.com/a/47403514/85785
https://stackoverflow.com/a/47442140/85785


Registration

JWT RefreshToken/BearerToken added to AutoLogin=true Register responses.

Updating UserInfo using the Register Service has been disabled by default, it can be enabled with: RegisterService.AllowUpdates = true;

Other

ToOptimizedResult() now supports HttpResult responses
Config.DebugMode is initialized using env.IsDevelopment() in .NET Core
Remove jquip/ss-utils dependency from MetadataDebugTemplate
Remove dead/unused code + PCL wrappers/polyfills like INameValueCollection
IMeta, IHasSessionId, IHasVersion interfaces now exported in Add SS Ref
VirtualFiles and VirtualFileSources properties in base Razor View
Html.IncludeFile() to embed file in HTML view
MapProjectPath() uses env.ContentRoot in .NET Core Apps


Community

Rolf Kristensen added support for async PushProperty for available loggers (Log4net,NLog, Serilog) with the ILogWithException and ILogWithContext interfaces and implemented support in EventLogger, Log4Net, NLog and Serilog logging providers which lets you add additional params with each Log Entry as well as logging within a context, e.g:

using (log.PushProperty("Hello", "World"))
{
    log.InfoFormat("Message");
}


ServiceStack.Redis

Add Ping/Echo APIs to IRedisClient

ServiceStack.OrmLite

Add support of MySqlConnector 
https://github.com/mysql-net/MySqlConnector

This is a clean-room reimplementation of the MySQL Protocol and is not based on the official connector. It's fully async, supporting the async ADO.NET methods added in .NET 4.5 without blocking (or using Task.Run to run synchronous methods on a background thread). It's also 100% compatible with .NET Core.

Add OnOpenConnection callback + OpenDbConnectionAsync API
https://github.com/ServiceStack/ServiceStack.OrmLite/commit/4c54a4199aba4034ba991907281162b865bbb5ee

Add support for auto splitting of IEnumerable params into multi db params
https://github.com/ServiceStack/ServiceStack.OrmLite/commit/8cee8a24b3674b76e821954c56917c9e97e704e4
https://forums.servicestack.net/t/where-in-params-for-custom-raw-sql-fail/4942/9?u=mythz

Dale daleholborow
Added support for allowing byte[] RowVersion in addition to OrmLite's ulong RowVersion, Dapper only supports byte[] RowVersion, which lets you use OrmLite DataModels with RowVersion in Dapper queries.

ServiceStack.Aws

PocoDynamo uses AWS's recommended SleepBackOffMultiplier
https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/

This is also available in:

Thread.Sleep(ExecUtils.CalculateFullJitterBackOffDelay(retriesAttempted));
await Task.Delay(ExecUtils.CalculateFullJitterBackOffDelay(retriesAttempted));











# [v4.5.14 Release Notes](/releases/v4.5.14)
