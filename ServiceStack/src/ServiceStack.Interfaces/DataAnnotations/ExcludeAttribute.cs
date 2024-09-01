using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Mark types that are to be excluded from metadata & specified endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExcludeAttribute(Feature feature) : AttributeBase
{
    public Feature Feature { get; set; } = feature;
}

/// <summary>
/// Exclude API from all Metadata Services
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ExcludeMetadataAttribute() : ExcludeAttribute(Feature.Metadata | Feature.Soap);


/// <summary>
/// Exclude Description from being generated for API Endpoint, e.g. hiding it from OpenAPI
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ExcludeFromDescriptionAttribute() : AttributeBase;

/// <summary>
/// Exclude Auto Registering Explicit API in AutoQuery
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ExplicitAutoQuery() : AttributeBase;
