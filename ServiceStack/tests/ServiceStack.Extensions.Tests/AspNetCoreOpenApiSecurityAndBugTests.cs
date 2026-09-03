#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using ServiceStack.AspNetCore.OpenApi;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace ServiceStack.Extensions.Tests;

public class SampleGenericType<T>
{
    public T? Value { get; set; }
}

public class ModelWithIgnoredProperty
{
    public string PublicProp { get; set; } = "";
    
    [IgnoreDataMember]
    public SensitiveSecretData Secret { get; set; } = new();
}

public class SensitiveSecretData
{
    public string Password { get; set; } = "";
}

[Route("/files/{Path*}")]
[Route("/items/{Id:int}")]
public class RouteWildcardAndConstraintRequest : IReturnVoid
{
    public string? Path { get; set; }
    public int Id { get; set; }
}

[TestFixture]
[NonParallelizable]
public class AspNetCoreOpenApiSecurityAndBugTests
{
    [Test]
    public void SwaggerUtils_DefaultIgnoreProperty_Includes_IgnoreDataMemberAttribute()
    {
        var pi = typeof(ModelWithIgnoredProperty).GetProperty(nameof(ModelWithIgnoredProperty.Secret))!;
        Assert.That(SwaggerUtils.DefaultIgnoreProperty(pi), Is.True);

        var referencedTypes = new HashSet<Type>();
        ServiceStackDocumentFilter.AddReferencedTypes(
            referencedTypes, 
            typeof(ModelWithIgnoredProperty), 
            ServiceStackDocumentFilter.IsDtoTypeOrEnum, 
            includeBaseTypes: true);

        // SensitiveSecretData should NOT be pulled into referencedTypes because Secret is [IgnoreDataMember]
        Assert.That(referencedTypes.Contains(typeof(SensitiveSecretData)), Is.False);
    }

    [Test]
    public void GetSchemaDefinitionRef_Sanitizes_Generic_Type_Names()
    {
        var schemaRef = OpenApiMetadata.GetSchemaDefinitionRef(typeof(SampleGenericType<string>));
        Assert.That(schemaRef.Contains("<"), Is.False);
        Assert.That(schemaRef.Contains(">"), Is.False);
        Assert.That(schemaRef.StartsWith("SampleGenericType_"), Is.True);
    }

    [Test]
    public void CreateParameters_Identifies_Wildcards_And_Constraints_As_Path_Parameters()
    {
        var metadata = new OpenApiMetadata();
        
        // Test wildcard route
        var wildcardParams = (List<OpenApiParameter>)typeof(OpenApiMetadata)
            .GetMethod("CreateParameters", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(metadata, [typeof(RouteWildcardAndConstraintRequest), "/files/{Path*}", "GET"])!;

        var pathParam = wildcardParams.FirstOrDefault(p => p.Name == "Path");
        Assert.That(pathParam, Is.Not.Null);
        Assert.That(pathParam!.In, Is.EqualTo(ParameterLocation.Path));

        // Test constrained route
        var constrainedParams = (List<OpenApiParameter>)typeof(OpenApiMetadata)
            .GetMethod("CreateParameters", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(metadata, [typeof(RouteWildcardAndConstraintRequest), "/items/{Id:int}", "GET"])!;

        var idParam = constrainedParams.FirstOrDefault(p => p.Name == "Id");
        Assert.That(idParam, Is.Not.Null);
        Assert.That(idParam!.In, Is.EqualTo(ParameterLocation.Path));
    }

    [Test]
    public void Response_204_NoContent_Does_Not_Contain_Content_Body()
    {
        using var appHost = new BasicAppHost().Init();
        appHost.Config.Return204NoContentForEmptyResponse = true;

        var metadata = new OpenApiMetadata();
        var restPath = new RestPath(typeof(RouteWildcardAndConstraintRequest), "/files/{Path*}", "GET");

        var responses = (OrderedDictionary<string, OpenApiResponse>)typeof(OpenApiMetadata)
            .GetMethod("GetMethodResponseCodes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(metadata, [restPath, new Dictionary<string, OpenApiSchema>(), typeof(RouteWildcardAndConstraintRequest)])!;

        Assert.That(responses.ContainsKey("204"), Is.True);
        var response204 = responses["204"];
        Assert.That(response204.Content.Count, Is.EqualTo(0), "204 No Content response MUST NOT specify a response body Content map");
    }
}
