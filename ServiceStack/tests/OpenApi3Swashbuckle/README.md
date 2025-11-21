# OpenApi3Sample

A minimal **.NET 10** ServiceStack host showing how to integrate the `ServiceStack.AspNetCore.OpenApi3` package with **Swashbuckle** and **Microsoft.OpenApi 3.x**.

This sample lives at:

- `ServiceStack/tests/OpenApi3Sample` (repo root)
- `tests/OpenApi3Sample` (inside the `ServiceStack` solution folder)

## What it demonstrates

- A minimal ServiceStack AppHost running on ASP.NET Core 10
- Integration of **ServiceStack.AspNetCore.OpenApi3** with **Swashbuckle.AspNetCore**
- Automatic generation of an **OpenAPI 3** document for ServiceStack services
- A simple `Hello` service exposed via ServiceStack and surfaced in Swagger

Key files:

- `OpenApi3Sample.csproj` – web project targeting `net10.0`, references:
  - `ServiceStack.AspNetCore.OpenApi3`
  - core ServiceStack projects and `Swashbuckle.AspNetCore`
- `Program.cs` – minimal ASP.NET Core bootstrap:
  - Adds ServiceStack (`AddServiceStack`)
  - Registers Swagger + ServiceStack OpenAPI (`AddSwaggerGen`, `AddServiceStackSwagger`)
  - Enables Swagger middleware in Development
- `Configure.AppHost.cs` – standard ServiceStack `AppHost` using `HostingStartup`
- `ServiceModel/Hello.cs` – `Hello` request/response DTOs
- `ServiceInterface/MyServices.cs` – `Hello` service implementation

## Build and run

From the **ServiceStack solution folder** (where `ServiceStack.sln` lives):

```bash
cd ServiceStack
dotnet build tests/OpenApi3Sample/OpenApi3Sample.csproj -f net10.0
```

To run the sample:

```bash
cd ServiceStack
dotnet run --project tests/OpenApi3Sample/OpenApi3Sample.csproj
```

By default this sample assumes Kestrel is listening on:

- `https://localhost:5001` (HTTPS)

## OpenAPI & Swagger endpoints

Once the app is running (Development environment):

- **Swagger UI (Swashbuckle):**
  - `https://localhost:5001/swagger`
- **Raw OpenAPI 3 JSON document:**
  - `https://localhost:5001/swagger/v1/swagger.json`

These are produced by **Swashbuckle** using the **ServiceStack.AspNetCore.OpenApi3** metadata pipeline, backed by **Microsoft.OpenApi 3.x**.

## ServiceStack endpoints to try

ServiceStack metadata & UI:

- Metadata: `https://localhost:5001/metadata`

`Hello` service examples:

- Query string:
  - `https://localhost:5001/json/reply/Hello?Name=World`
- Route-based:
  - `https://localhost:5001/hello/World`

You should see the `Hello` operation documented in Swagger UI and present under the `/hello` path in the OpenAPI JSON.

