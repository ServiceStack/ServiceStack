#if NET10_0_OR_GREATER
using Microsoft.OpenApi;

namespace ServiceStack.AspNetCore.OpenApi;

public static class OpenApiType
{
    public const string Array = "array";
    public const string Boolean = "boolean";
    public const string Number = "number";
    public const string Integer = "integer";
    public const string String = "string";
    public const string Object = "object";

    public static JsonSchemaType ToJsonSchemaType(string type) => type switch
    {
        Array => JsonSchemaType.Array,
        Boolean => JsonSchemaType.Boolean,
        Number => JsonSchemaType.Number,
        Integer => JsonSchemaType.Integer,
        String => JsonSchemaType.String,
        Object => JsonSchemaType.Object,
        _ => JsonSchemaType.String
    };
}
#endif