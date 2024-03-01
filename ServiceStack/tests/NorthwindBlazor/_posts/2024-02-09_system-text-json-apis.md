---
title: System.Text.Json ServiceStack APIs
summary: ServiceStack .NET 8+ APIs can now be configured to use high-performance async System.Text.Json serialization  
tags: [servicestack,.net8,json,apis]
image: https://images.unsplash.com/photo-1644325349124-d1756b79dd42?crop=entropy&fit=crop&h=1000&w=2000
author: Gayle Smith
---

In continuing our focus to enable ServiceStack to become a deeply integrated part of .NET 8 Application's, ServiceStack
latest .NET 8 templates now default to using standardized ASP.NET Core features wherever possible, including:

- [ASP.NET Core Identity Auth](/posts/net8-identity-auth)
- [ASP.NET Core IOC](/posts/servicestack-endpoint-routing#asp.net-core-ioc)
- [Endpoint Routing](/posts/servicestack-endpoint-routing#endpoint-routing)
- [Swashbuckle for Open API v3 and Swagger UI](/posts/openapi-v3-support)
- [System.Text.Json APIs](/posts/system-text-json-apis)

This reduces friction for integrating ServiceStack into existing .NET 8 Apps, encourages greater knowledge and reuse and
simplifies .NET development as developers have a reduced number of concepts to learn, fewer technology implementations to
configure and maintain that are now applied across their entire .NET App.

The last integration piece supported was utilizing **System.Text.Json** - the default high-performance async JSON serializer
used in .NET Applications, can now be used by ServiceStack APIs to serialize and deserialize its JSON API Responses
that's enabled by default when using **Endpoint Routing**.

This integrates ServiceStack APIs more than ever where just like Minimal APIs and Web API,
uses **ASP.NET Core's IOC** to resolve dependencies, uses **Endpoint Routing** to Execute APIs that's secured with
**ASP.NET Core Identity Auth** then uses **System.Text.Json** to deserialize and serialize its JSON payloads.


### Enabled by Default when using Endpoint Routing

```csharp
app.UseServiceStack(new AppHost(), options => {
    options.MapEndpoints();
});
```

### Enhanced Configuration

ServiceStack uses a custom `JsonSerializerOptions` to improve compatibility with existing ServiceStack DTOs and
ServiceStack's rich ecosystem of generic [Add ServiceStack Reference](https://docs.servicestack.net/add-servicestack-reference)
Service Clients, which is configured to:

- Not serialize `null` properties
- Supports Case Insensitive Properties
- Uses `CamelCaseNamingPolicy` for property names
- Serializes `TimeSpan` and `TimeOnly` Data Types with [XML Schema Time format](https://www.w3.org/TR/xmlschema-2/#isoformats)
- Supports `[DataContract]` annotations
- Supports Custom Enum Serialization

### Benefits all Add ServiceStack Reference Languages

This compatibility immediately benefits all of ServiceStack's [Add ServiceStack Reference](https://docs.servicestack.net/add-servicestack-reference)
native typed integrations for **11 programming languages** which all utilize ServiceStack's JSON API endpoints - now serialized with System.Text.Json

### Support for DataContract Annotations

Support for .NET's `DataContract` serialization attributes was added using a custom `TypeInfoResolver`, specifically it supports:

- `[DataContract]` - When annotated, only `[DataMember]` properties are serialized
- `[DataMember]` - Specify a custom **Name** or **Order** of properties
- `[IgnoreDataMember]` - Ignore properties from serialization
- `[EnumMember]` - Specify a custom value for Enum values

### Custom Enum Serialization

Below is a good demonstration of the custom Enum serialization support which matches ServiceStack.Text's behavior:

```csharp
public enum EnumType { Value1, Value2, Value3 }

[Flags]
public enum EnumTypeFlags { Value1, Value2, Value3 }

public enum EnumStyleMembers
{
    [EnumMember(Value = "lower")]
    Lower,
    [EnumMember(Value = "UPPER")]
    Upper,
}

return new EnumExamples {
    EnumProp = EnumType.Value2, // String value by default
    EnumFlags = EnumTypeFlags.Value2 | EnumTypeFlags.Value3, // [Flags] as int
    EnumStyleMembers = EnumStyleMembers.Upper, // Serializes [EnumMember] value
    NullableEnumProp = null, // Ignores nullable enums
};
```

Which serializes to:

```json
{
  "enumProp": "Value2",
  "enumFlags": 3,
  "enumStyleMembers": "UPPER"
}
```

### Custom Configuration

You can further customize the `JsonSerializerOptions` used by ServiceStack by using `ConfigureJsonOptions()` to add
any customizations that you can optionally apply to ASP.NET Core's JSON APIs and MVC with:

```csharp
builder.Services.ConfigureJsonOptions(options => {
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
})
.ApplyToApiJsonOptions()  // Apply to ASP.NET Core's JSON APIs
.ApplyToMvcJsonOptions(); // Apply to MVC
```

### Control over when and where System.Text.Json is used

Whilst `System.Text.Json` is highly efficient, it's also very strict in the inputs it accepts where you may want to
revert back to using ServiceStack's JSON Serializer for specific APIs, especially when you need to support external
clients that can't be updated.

This can done by annotating Request DTOs with `[SystemJson]` attribute, e.g: you can limit to only use `System.Text.Json`
for an **APIs Response** with:

```csharp
[SystemJson(UseSystemJson.Response)]
public class CreateUser : IReturn<IdResponse>
{
    //...
}
```

Or limit to only use `System.Text.Json` for an **APIs Request** with:

```csharp
[SystemJson(UseSystemJson.Request)]
public class CreateUser : IReturn<IdResponse>
{
    //...
}
```

Or not use `System.Text.Json` at all for an API with:

```csharp
[SystemJson(UseSystemJson.Never)]
public class CreateUser : IReturn<IdResponse>
{
    //...
}
```

### JsonApiClient Support

When Endpoints Routing is configured, the `JsonApiClient` will also be configured to utilize the same `System.Text.Json`
options to send and receive its JSON API Requests which also respects the `[SystemJson]` specified behavior.

Clients external to the .NET App can be configured to use `System.Text.Json` with:

```csharp
ClientConfig.UseSystemJson = UseSystemJson.Always;
```

Whilst any custom configuration can be applied to its `JsonSerializerOptions` with:

```csharp
TextConfig.ConfigureSystemJsonOptions(options => {
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
```

### Scoped JSON Configuration

We've also added partial support for [Customized JSON Responses](https://docs.servicestack.net/customize-json-responses)
for the following customization options:

:::{.table,w-full}
| Name                         | Alias |
|------------------------------|-------|
| EmitCamelCaseNames           | eccn  |
| EmitLowercaseUnderscoreNames | elun  |
| EmitPascalCaseNames          | epcn  |
| ExcludeDefaultValues         | edv   |
| IncludeNullValues            | inv   |
| Indent                       | pp    |
:::

These can be applied to the JSON Response by returning a decorated `HttpResult` with a custom `ResultScope`, e.g:

```csharp
return new HttpResult(responseDto) {
    ResultScope = () => 
        JsConfig.With(new() { IncludeNullValues = true, ExcludeDefaultValues = true })
};
```

They can also be requested by API consumers by adding a `?jsconfig` query string with the desired option or its alias, e.g:

```csharp
/api/MyRequest?jsconfig=EmitLowercaseUnderscoreNames,ExcludeDefaultValues
/api/MyRequest?jsconfig=eccn,edv
```

### SystemJsonCompatible

Another configuration automatically applied when `System.Text.Json` is enabled is:

```csharp
JsConfig.SystemJsonCompatible = true;
```

Which is being used to make ServiceStack's JSON Serializer more compatible with `System.Text.Json` output so it's easier
to switch between the two with minimal effort and incompatibility. Currently this is only used to override
`DateTime` and `DateTimeOffset` behavior which uses `System.Text.Json` for its Serialization/Deserialization.
