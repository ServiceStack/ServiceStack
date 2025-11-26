using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.AspNetCore.OpenApi;

// ServiceStack Document Transformer for Microsoft.AspNetCore.OpenApi
public class ServiceStackDocumentTransformer(OpenApiMetadata metadata) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Check if ServiceStack has been initialized
        if (HostContext.AppHost == null)
        {
            // ServiceStack not yet initialized, skip for now
            return Task.CompletedTask;
        }

        // Use AddComponent to properly register security schemes with the workspace
        // This is required for OpenApiSecuritySchemeReference.Target to resolve correctly
        if (metadata.SecurityDefinition != null)
        {
            document.AddComponent(metadata.SecurityDefinition.Scheme, metadata.SecurityDefinition);
        }

        if (metadata.ApiKeySecurityDefinition != null)
        {
            document.AddComponent(metadata.ApiKeySecurityDefinition.Scheme, metadata.ApiKeySecurityDefinition);
        }

        // Ensure we have a Paths collection to populate
        document.Paths ??= new OpenApiPaths();

        var restPathMap = HostContext.ServiceController.RestPathMap;

        // Build schemas for all DTOs used by ServiceStack operations
        var operations = HostContext.Metadata.OperationsMap.Values.ToList();
        var dtos = new HashSet<Type>();
        foreach (var op in operations)
        {
            if (IsDtoTypeOrEnum(op.RequestType))
            {
                AddReferencedTypes(dtos, op.RequestType, IsDtoTypeOrEnum, includeBaseTypes: HttpUtils.HasRequestBody(op.Method));
            }

            if (op.ResponseType != null && IsDtoTypeOrEnum(op.ResponseType))
            {
                // Ensure response DTOs like HelloResponse are also added to components/schemas
                AddReferencedTypes(dtos, op.ResponseType, IsDtoTypeOrEnum, includeBaseTypes: true);
            }
        }

        var orderedDtos = dtos.OrderBy(x => x.Name);
        foreach (var type in orderedDtos)
        {
            var schema = metadata.CreateSchema(type, allTypes: dtos);
            if (schema != null)
            {
                document.Components ??= new OpenApiComponents();
                document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                document.Components.Schemas[OpenApiMetadata.GetSchemaTypeName(type)] = schema;
            }
        }

        // Populate operations/paths from ServiceStack metadata
        foreach (var restPathList in restPathMap.Values)
        {
            foreach (var restPath in restPathList)
            {
                var routePath = restPath.Path.Replace("*", string.Empty);
                var requestType = restPath.RequestType;

                // Only register the primary HTTP method for each endpoint
                var primaryMethod = ServiceClientUtils.GetHttpMethod(requestType);
                var verbs = new List<string>();
                if (primaryMethod != null)
                {
                    verbs.Add(primaryMethod);
                }
                else if (!restPath.AllowsAllVerbs)
                {
                    verbs.AddRange(restPath.Verbs);
                }
                else
                {
                    // Fallback to POST if no primary method can be determined
                    verbs.Add(HttpMethods.Post);
                }
                if (!HostContext.Metadata.OperationsMap.TryGetValue(requestType, out var opMeta))
                    continue;

                if (metadata.Ignore?.Invoke(opMeta) == true)
                    continue;

                // Skip operations that exclude metadata or API explorer features
                if (requestType.ExcludesFeature(Feature.Metadata) || requestType.ExcludesFeature(Feature.ApiExplorer))
                    continue;

                // Swashbuckle expects route templates without leading ~/ and with {id} style params
                var swaggerPath = routePath.StartsWith("~/")
                    ? routePath.Substring(1)
                    : routePath;

                if (!document.Paths.TryGetValue(swaggerPath, out var pathItem))
                {
                    pathItem = new OpenApiPathItem();
                    document.Paths[swaggerPath] = pathItem;
                }

                // Apply each verb for this RestPath
                foreach (var verb in verbs)
                {
                    var openApiOp = new OpenApiOperation
                    {
                        OperationId = $"{requestType.Name}{verb}{swaggerPath.Replace("/", "_").Replace("{", "_").Replace("}", string.Empty)}",
                    };

                    openApiOp = metadata.AddOperation(openApiOp, opMeta, verb, routePath, document);

                    // Responses
                    var responses = GetResponses(metadata, restPath, requestType);
                    foreach (var entry in responses)
                    {
                        openApiOp.Responses[entry.Key] = entry.Value;
                    }

                    // Tags from [Tag] attributes if any
                    var userTags = requestType.AllAttributes<TagAttribute>().Map(x => x.Name);
                    foreach (var tag in userTags)
                    {
                        openApiOp.Tags ??= new HashSet<OpenApiTagReference>();
                        openApiOp.Tags.Add(new OpenApiTagReference(tag));
                    }

                    // Map verb to HttpMethod and add operation to path
                    HttpMethod? httpMethod = verb switch
                    {
                        HttpMethods.Get => HttpMethod.Get,
                        HttpMethods.Post => HttpMethod.Post,
                        HttpMethods.Put => HttpMethod.Put,
                        HttpMethods.Delete => HttpMethod.Delete,
                        HttpMethods.Patch => HttpMethod.Patch,
                        HttpMethods.Head => HttpMethod.Head,
                        HttpMethods.Options => HttpMethod.Options,
                        _ => null
                    };

                    if (httpMethod != null && pathItem is OpenApiPathItem concretePathItem)
                    {
                        concretePathItem.AddOperation(httpMethod, openApiOp);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static OrderedDictionary<string, OpenApiResponse> GetResponses(OpenApiMetadata metadata, IRestPath restPath, Type requestType)
    {
        // OpenApiMetadata already knows how to build response schemas using the v3 models
        var schemas = metadata.Schemas.ToDictionary(x => x.Key, x => x.Value);
        return metadata.GetMethodResponseCodes(restPath, schemas, requestType);
    }

    public static void AddReferencedTypes(HashSet<Type> to, Type? type, Func<Type,bool> include, bool includeBaseTypes)
    {
        if (type == null || to.Contains(type) || !include(type))
            return;

        to.Add(type);

        var baseType = type.BaseType;
        if (includeBaseTypes && baseType != null && include(baseType) && !to.Contains(baseType))
        {
            AddReferencedTypes(to, baseType, include, includeBaseTypes);

            var genericArgs = type.IsGenericType
                ? type.GetGenericArguments()
                : Type.EmptyTypes;

            foreach (var arg in genericArgs)
            {
                AddReferencedTypes(to, arg, include, includeBaseTypes);
            }
        }

        foreach (var pi in type.GetSerializableProperties())
        {
            // Skip Obsolete properties
            if (OpenApiUtils.IgnoreProperty(pi))
                continue;
            
            if (to.Contains(pi.PropertyType))
                continue;
            
            if (include(pi.PropertyType))
                AddReferencedTypes(to, pi.PropertyType, include, includeBaseTypes);

            var genericArgs = pi.PropertyType.IsGenericType
                ? pi.PropertyType.GetGenericArguments()
                : Type.EmptyTypes;

            if (genericArgs.Length > 0)
            {
                foreach (var arg in genericArgs)
                {
                    AddReferencedTypes(to, arg, include, includeBaseTypes);
                }
            }
            else if (pi.PropertyType.IsArray)
            {
                var elType = pi.PropertyType.HasElementType ? pi.PropertyType.GetElementType() : null;
                AddReferencedTypes(to, elType, include, includeBaseTypes);
            }
        }
    }

    public static bool IsDtoTypeOrEnum(Type? type) => type != null
        && (ServiceMetadata.IsDtoType(type) || type.IsEnum)
        && !type.IsGenericTypeDefinition
        && !(type.ExcludesFeature(Feature.Metadata) || type.ExcludesFeature(Feature.ApiExplorer));
}
