using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using ServiceStack.Host;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack.AspNetCore.OpenApi;

// Last OpenApi Filter to run
public class ServiceStackDocumentFilter(OpenApiMetadata metadata) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        //Console.WriteLine(GetType().Name + "...");
        if (metadata.SecurityDefinition != null)
        {
            swaggerDoc.Components.SecuritySchemes[metadata.SecurityDefinition.Scheme] = metadata.SecurityDefinition;
        }

        if (metadata.ApiKeySecurityDefinition != null)
        {
            swaggerDoc.Components.SecuritySchemes[metadata.ApiKeySecurityDefinition.Scheme] = metadata.ApiKeySecurityDefinition;
        }

        var operations = HostContext.Metadata.OperationsMap.Values.ToList();
        var dtos = new HashSet<Type>();
        foreach (var op in operations)
        {
            if (!IsDtoTypeOrEnum(op.RequestType))
                continue;
            
            AddReferencedTypes(dtos, op.RequestType, IsDtoTypeOrEnum, includeBaseTypes:HttpUtils.HasRequestBody(op.Method));
        }
        
        var orderedDtos = dtos.OrderBy(x => x.Name);
        foreach (var type in orderedDtos)
        {
            //Console.WriteLine("Type: " + type.ToPrettyName() + " ...");
            var schema = metadata.CreateSchema(type, allTypes: dtos);
            if (schema != null)
            {
                swaggerDoc.Components.Schemas[OpenApiMetadata.GetSchemaTypeName(type)] = schema;
            }
        }
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
            if (pi.HasAttributeCached<ObsoleteAttribute>() || pi.HasAttributeCached<JsonIgnoreAttribute>())
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