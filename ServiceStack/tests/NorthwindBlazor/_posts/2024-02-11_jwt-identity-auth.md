---
title: ASP.NET Core JWT Identity Auth
summary: Learn about integration and value added features of ASP.NET Core JWT Identity Auth   
tags: [servicestack,.net8,auth]
image: https://images.unsplash.com/photo-1618482914248-29272d021005?crop=entropy&fit=crop&h=1000&w=2000
author: Brandon Foley
---

JWTs enable stateless authentication of clients without servers needing to maintain any Auth state in server infrastructure
or perform any I/O to validate a token. As such,
[JWTs are a popular choice for Microservices](https://docs.servicestack.net/auth/jwt-authprovider#stateless-auth-microservices)
as they only need to configured with confidential keys to validate access.

### ASP.NET Core JWT Authentication

ServiceStack's JWT Identity Auth reimplements many of the existing [ServiceStack JWT AuthProvider](https://docs.servicestack.net/auth/jwt-authprovider)
features but instead of its own implementation, integrates with and utilizes ASP.NET Core's built-in JWT Authentication that's
configurable in .NET Apps with the `.AddJwtBearer()` extension method, e.g:

#### Program.cs

```csharp
services.AddAuthentication()
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = config["JwtBearer:ValidIssuer"],
            ValidAudience = config["JwtBearer:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtBearer:IssuerSigningKey"]!)),
            ValidateIssuerSigningKey = true,
        };
    })
    .AddIdentityCookies(options => options.DisableRedirectsForApis());
```

Then use the `JwtAuth()` method to enable and configure ServiceStack's support for ASP.NET Core JWT Identity Auth: 

#### Configure.Auth.cs

```csharp
public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(
                options => {
                    options.SessionFactory = () => new CustomUserSession();
                    options.CredentialsAuth();
                    options.JwtAuth(x => {
                        // Enable JWT Auth Features...
                    });
                })));
        });
}
```

### Enable in Swagger UI

Once configured we can enable JWT Auth in Swagger UI by installing **Swashbuckle.AspNetCore**:

:::copy
`<PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />`
:::

Then enable Open API, Swagger UI, ServiceStack's support for Swagger UI and the JWT Bearer Auth option:

```csharp
public class ConfigureOpenApi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen();
                services.AddServiceStackSwagger();
                services.AddJwtAuth();
                //services.AddBasicAuth<Data.ApplicationUser>();
            
                services.AddTransient<IStartupFilter,StartupFilter>();
            }
        });

    public class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app => {
                // Provided by Swashbuckle library
                app.UseSwagger();
                app.UseSwaggerUI();
                next(app);
            };
    }
}
```

This will enable the **Authorize** button in Swagger UI where you can authenticate with a JWT Token:

![](https://servicestack.net/img/posts/jwt-identity-auth/jwt-swagger-ui.png)

### JWT Auth in Built-in UIs

This also enables the **JWT** Auth Option in ServiceStack's built-in 
[API Explorer](https://docs.servicestack.net/api-explorer), 
[Locode](https://docs.servicestack.net/locode/) and 
[Admin UIs](https://docs.servicestack.net/admin-ui):

<img class="shadow p-1" src="https://servicestack.net/img/posts/jwt-identity-auth/jwt-api-explorer.png">

### Authenticating with JWT

JWT Identity Auth is a drop-in replacement for ServiceStack's JWT AuthProvider where Authenticating via Credentials
will convert the Authenticated User into a JWT Bearer Token returned in the **HttpOnly**, **Secure** `ss-tok` Cookie
that will be used to Authenticate the client:

```csharp
var client = new JsonApiClient(BaseUrl);
await client.SendAsync(new Authenticate {
    provider = "credentials",
    UserName = Username,
    Password = Password,
});

var bearerToken = client.GetTokenCookie(); // ss-tok Cookie
```

## JWT Refresh Tokens

Refresh Tokens can be used to allow users to request a new JWT Access Token when the current one expires.

To enable support for JWT Refresh Tokens your `IdentityUser` model should implement the `IRequireRefreshToken` interface
which will be used to store the 64 byte Base64 URL-safe `RefreshToken` and its `RefreshTokenExpiry` in its persisted properties:

```csharp
public class ApplicationUser : IdentityUser, IRequireRefreshToken
{
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
```

Now after successful authentication, the `RefreshToken` will also be returned in the `ss-reftok` Cookie:

```csharp
var refreshToken = client.GetRefreshTokenCookie(); // ss-reftok Cookie
```

### Transparent Server Auto Refresh of JWT Tokens

To be able to terminate a users access, Users need to revalidate their eligibility to verify they're still allowed access 
(e.g. deny Locked out users). This JWT revalidation pattern is implemented using Refresh Tokens which are used to request 
revalidation of their access and reissuing a new JWT Access Token which can be used to make authenticated requests until it expires.

As Cookies are used to return Bearer and Refresh Tokens ServiceStack is able to implement the revalidation logic on the 
server where it transparently validates Refresh Tokens, and if a User is eligible will reissue a new JWT Token Cookie that
replaces the expired Access Token Cookie.

Thanks to this behavior HTTP Clients will be able to Authenticate with just the Refresh Token, which will transparently
reissue a new JWT Access Token Cookie and then continue to perform the Authenticated Request:

```csharp
var client = new JsonApiClient(BaseUrl);
client.SetRefreshTokenCookie(RefreshToken);

var response = await client.SendAsync(new Secured { ... });
```

There's also opt-in sliding support for extending a User's RefreshToken after usage which allows Users to treat 
their Refresh Token like an API Key where it will continue extending whilst they're continuously using it to make API requests, 
otherwise expires if they stop. How long to extend the expiry of Refresh Tokens after usage can be configured with:

```csharp
options.JwtAuth(x => {
    // How long to extend the expiry of Refresh Tokens after usage (default None)
    x.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(90);
});
```

## Convert Session to Token Service

Another useful Service that's available is being able to Convert your current Authenticated Session into a Token
with the `ConvertSessionToToken` Service which can be enabled with:

```csharp
options.JwtAuth(x => {
    x.IncludeConvertSessionToTokenService = true;
});
```

This can be useful for when you want to Authenticate via an external OAuth Provider that you then want to convert into a stateless
JWT Token by calling the `ConvertSessionToToken` on the client, e.g:

#### .NET Clients

```csharp
await client.SendAsync(new ConvertSessionToToken());
```

#### TypeScript/JavaScript

```ts
fetch('/session-to-token', { method:'POST', credentials:'include' })
```

The default behavior of `ConvertSessionToToken` is to remove the Current Session from the Auth Server which will prevent 
access to protected Services using our previously Authenticated Session. If you still want to preserve your existing Session 
you can indicate this with:

```csharp
await client.SendAsync(new ConvertSessionToToken { 
    PreserveSession = true 
});
```

### JWT Options

Other configuration options available for Identity JWT Auth include:

```csharp
options.JwtAuth(x => {
    // How long should JWT Tokens be valid for. (default 14 days)
    x.ExpireTokensIn = TimeSpan.FromDays(14);
    
    // How long should JWT Refresh Tokens be valid for. (default 90 days)
    x.ExpireRefreshTokensIn = TimeSpan.FromDays(90);
    
    x.OnTokenCreated = (req, user, claims) => {
        // Customize which claims are included in the JWT Token
    };
    
    // Whether to invalidate Refresh Tokens on Logout (default true)
    x.InvalidateRefreshTokenOnLogout = true;
    
    // How long to extend the expiry of Refresh Tokens after usage (default None)
    x.ExtendRefreshTokenExpiryAfterUsage = null;
});
```
