# OpenApiScalar Demo

This demo project showcases **ServiceStack** integration with **Microsoft.AspNetCore.OpenApi** and **Scalar UI**.

## What's Included

- ✅ ServiceStack services with OpenAPI documentation
- ✅ Microsoft.AspNetCore.OpenApi for native OpenAPI 3.1 generation
- ✅ Scalar UI for modern, interactive API documentation
- ✅ Example services demonstrating various ServiceStack features

## Running the Demo

```bash
cd ServiceStack/tests/OpenApiScalar
dotnet run
```

The application will start on:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

## Available Endpoints

### API Documentation

| Endpoint | Description |
|----------|-------------|
| https://localhost:5001/scalar/v1 | **Scalar UI** - Modern interactive API documentation |
| https://localhost:5001/openapi/v1.json | **OpenAPI Document** - Raw OpenAPI 3.1 JSON |

### ServiceStack Endpoints

| Endpoint | Description |
|----------|-------------|
| https://localhost:5001/metadata | ServiceStack metadata page |
| https://localhost:5001/hello | Example Hello service |
| https://localhost:5001/hello/{Name} | Hello service with route parameter |

## Project Structure

```
OpenApiScalar/
├── Program.cs              # Application entry point and configuration
├── Configure.AppHost.cs    # ServiceStack AppHost configuration
├── ServiceInterface/       # Service implementations
│   └── MyServices.cs
└── ServiceModel/          # DTOs and request/response models
    └── Hello.cs
```

## Configuration Breakdown

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Add Microsoft's native OpenAPI support
builder.Services.AddOpenApi();

// 2. Add ServiceStack OpenAPI integration
builder.Services.AddServiceStackOpenApi();

// 3. Add ServiceStack services
builder.Services.AddServiceStack(typeof(MyServices).Assembly);

var app = builder.Build();

// 4. Initialize ServiceStack
app.UseServiceStack(new AppHost());

// 5. Map OpenAPI endpoints (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();           // Maps /openapi/v1.json
    app.MapScalarApiReference(); // Maps /scalar/v1
}

app.Run();
```

### Key Points

1. **AddOpenApi()** - Registers Microsoft's OpenAPI services (default document name: "v1")
2. **AddServiceStackOpenApi()** - Integrates ServiceStack operations into the OpenAPI document
3. **AddServiceStack()** - Registers ServiceStack services
4. **UseServiceStack()** - Initializes ServiceStack middleware (must be before MapOpenApi)
5. **MapOpenApi()** - Exposes the OpenAPI JSON document
6. **MapScalarApiReference()** - Exposes the Scalar UI

## Customizing the Configuration

### Custom Document Name

```csharp
// Use a custom document name
builder.Services.AddOpenApi("api");
builder.Services.AddServiceStackOpenApi("api");

// Access at /openapi/api.json and /scalar/api
```

### Multiple API Versions

```csharp
// Configure multiple versions
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

builder.Services.AddServiceStackOpenApi("v1");
builder.Services.AddServiceStackOpenApi("v2");

// Access at:
// - /openapi/v1.json and /scalar/v1
// - /openapi/v2.json and /scalar/v2
```

### Custom Metadata

```csharp
builder.Services.AddServiceStackOpenApi(configure: metadata =>
{
    metadata.Title = "My Custom API";
    metadata.Version = "2.0.0";
    metadata.Description = "A comprehensive API for my application";
    
    // Add authentication schemes
    metadata.AddBasicAuth();
    metadata.AddApiKeyAuth();
    metadata.AddBearerAuth();
});
```

### Scalar UI Customization

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithPreferredScheme("https");
});
```

## Testing the API

### Using Scalar UI

1. Navigate to https://localhost:5001/scalar/v1
2. Browse the available endpoints
3. Try out the API directly from the UI
4. View request/response examples
5. Generate code snippets in various languages

### Using curl

```bash
# Get the OpenAPI document
curl -k https://localhost:5001/openapi/v1.json

# Call the Hello service
curl -k "https://localhost:5001/hello?Name=World"

# Call with route parameter
curl -k https://localhost:5001/hello/World
```

### Using HTTP files (VS Code REST Client)

```http
### Get OpenAPI document
GET https://localhost:5001/openapi/v1.json

### Call Hello service
GET https://localhost:5001/hello?Name=World

### Call with route parameter
GET https://localhost:5001/hello/World
```

## Learn More

- [ServiceStack Documentation](https://docs.servicestack.net/)
- [Microsoft.AspNetCore.OpenApi](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [Scalar Documentation](https://github.com/scalar/scalar)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)

## Troubleshooting

### Port already in use

If ports 5000/5001 are already in use, modify `Properties/launchSettings.json`:

```json
{
  "applicationUrl": "https://localhost:7001;http://localhost:7000"
}
```

### HTTPS certificate issues

Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

### OpenAPI document is empty

Ensure the order of middleware registration:
1. `UseServiceStack()` must be called before `MapOpenApi()`
2. Document names must match between `AddOpenApi()` and `AddServiceStackOpenApi()`

