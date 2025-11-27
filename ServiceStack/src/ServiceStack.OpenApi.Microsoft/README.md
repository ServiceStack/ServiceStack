# ServiceStack.AspNetCore.OpenApi.Microsoft

ServiceStack integration for **Microsoft.AspNetCore.OpenApi** - the native OpenAPI document generation library in ASP.NET Core.

This package enables ServiceStack services to be documented using Microsoft's built-in OpenAPI support, providing seamless integration with modern OpenAPI UI tools like Scalar, Swagger UI, and others.

## Features

- ✅ Native integration with `Microsoft.AspNetCore.OpenApi` (ASP.NET Core's built-in OpenAPI support)
- ✅ Automatic OpenAPI 3.1 document generation from ServiceStack services
- ✅ Support for multiple OpenAPI documents
- ✅ Compatible with Scalar UI, Swagger UI, and other OpenAPI viewers
- ✅ Full support for ServiceStack DTOs, routes, and metadata
- ✅ Security scheme support (Basic Auth, API Key, Bearer tokens)

## Installation

```bash
dotnet add package ServiceStack.AspNetCore.OpenApi.Microsoft
```

For Scalar UI support:
```bash
dotnet add package Scalar.AspNetCore
```

## Quick Start

### Basic Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Microsoft OpenAPI support
builder.Services.AddOpenApi();

// Add ServiceStack OpenAPI integration
builder.Services.AddServiceStackOpenApi();

// Add ServiceStack
builder.Services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

// Configure ServiceStack
app.UseServiceStack(new AppHost());

// Map OpenAPI endpoints (in Development)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
```

### With Scalar UI

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddServiceStackOpenApi();
builder.Services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

app.UseServiceStack(new AppHost());

if (app.Environment.IsDevelopment())
{
    // Map OpenAPI document endpoint
    app.MapOpenApi();
    
    // Map Scalar UI endpoint
    app.MapScalarApiReference();
}

app.Run();
```

## Available Endpoints

After configuration, the following endpoints are available:

### OpenAPI Document Endpoints

| Endpoint | Description |
|----------|-------------|
| `/openapi/v1.json` | OpenAPI 3.1 document in JSON format (default document) |
| `/openapi/{documentName}.json` | Named OpenAPI documents |

### UI Endpoints

#### Scalar UI (when using `Scalar.AspNetCore`)

| Endpoint | Description |
|----------|-------------|
| `/scalar/v1` | Scalar API reference UI for the default document |
| `/scalar/{documentName}` | Scalar UI for named documents |

#### Swagger UI (when using `Swashbuckle.AspNetCore`)

| Endpoint | Description |
|----------|-------------|
| `/swagger/v1/swagger.json` | Swagger document endpoint |
| `/swagger` | Swagger UI |

## Advanced Configuration

### Multiple OpenAPI Documents

You can configure multiple OpenAPI documents for different API versions or groups:

```csharp
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

// Configure ServiceStack for each document
builder.Services.AddServiceStackOpenApi("v1");
builder.Services.AddServiceStackOpenApi("v2");
```

### Custom Document Configuration

```csharp
builder.Services.AddServiceStackOpenApi(documentName: "v1", configure: metadata =>
{
    // Configure metadata
    metadata.Title = "My API";
    metadata.Version = "1.0.0";
    metadata.Description = "My ServiceStack API";
    
    // Add security definitions
    metadata.AddBasicAuth();
    metadata.AddApiKeyAuth();
    metadata.AddBearerAuth();
});
```

### Security Schemes

Add authentication schemes to your OpenAPI document:

```csharp
builder.Services.AddServiceStackOpenApi(configure: metadata =>
{
    // Basic Authentication
    metadata.AddBasicAuth();
    
    // API Key Authentication
    metadata.AddApiKeyAuth();
    
    // Bearer Token Authentication
    metadata.AddBearerAuth();
});
```

## Scalar UI Configuration

Scalar provides a modern, interactive API documentation interface.

### Basic Scalar Setup

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

### Custom Scalar Options

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
```

## Example Project

See the `ServiceStack/tests/OpenApiScalar` project for a complete working example.

## Differences from Swashbuckle

This package uses **Microsoft.AspNetCore.OpenApi** instead of **Swashbuckle.AspNetCore**:

| Feature | Swashbuckle | Microsoft.AspNetCore.OpenApi |
|---------|-------------|------------------------------|
| OpenAPI Version | 3.0 | 3.1 |
| Integration | Third-party | Native ASP.NET Core |
| Performance | Good | Better (native) |
| Document Filters | `IDocumentFilter` | `IOpenApiDocumentTransformer` |
| Schema Filters | `ISchemaFilter` | `IOpenApiSchemaTransformer` |

## Troubleshooting

### OpenAPI document shows empty paths

Make sure you call `AddServiceStackOpenApi()` with the correct document name that matches your `AddOpenApi()` call:

```csharp
// These must match
builder.Services.AddOpenApi("v1");
builder.Services.AddServiceStackOpenApi("v1");
```

### ServiceStack operations not appearing

Ensure `UseServiceStack()` is called before `MapOpenApi()`:

```csharp
app.UseServiceStack(new AppHost());  // Must be before MapOpenApi
app.MapOpenApi();
```

## License

See the main ServiceStack license.

