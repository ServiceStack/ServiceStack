## App Writable Folder

This directory is designated for:

- **Embedded Databases**: Such as SQLite.
- **Writable Files**: Files that the application might need to modify during its operation.

For applications running in **Docker**, it's a common practice to mount this directory as an external volume. This ensures:

- **Data Persistence**: App data is preserved across deployments.
- **Easy Replication**: Facilitates seamless data replication for backup or migration purposes.

## Startup Order Logs 

```
Program.cs

AppHost.ConfigureServices()
ConfigureAuth.ConfigureServices()
ConfigureAutoQuery.ConfigureServices()
ConfigureDb.ConfigureServices()
ConfigureSsg.ConfigureServices()
ConfigureMq.ConfigureServices()   

WebApplication.CreateBuilder(args)
services.AddAuthentication()
services.AddDbContext()   
services.AddIdentityCore()
services.AddBlazorServerIdentityApiClient()

services.AddServiceStack()
AutoQueryFeature.Configure(IServiceCollection)
AutoQueryFeature.AfterConfigure(IServiceCollection)
ValidationFeature.AfterConfigure(IServiceCollection)
ValidationFeature.EnableDeclarativeValidation

var app = builder.Build();
app.UseServiceStack()

ConfigureDbMigrations.ConfigureAppHost(ServiceStackHost)
ValidationFeature.BeforePluginsLoaded(IAppHost)
AutoQueryFeature.BeforePluginsLoaded(IAppHost)
ValidationFeature.Register(IAppHost)
AutoQueryFeature.Register(IAppHost)
ValidationFeature: appHost.PostConfigurePlugin<MetadataFeature>(IAppHost)

ValidationFeature.AfterInit
BlazorConfig.Set()
```