#if !NET10_0_OR_GREATER
namespace ServiceStack.AspNetCore.OpenApi;

public static class OpenApiTypeFormat
{
    public const string Array = "int32";
    public const string Byte = "byte";
    public const string Binary = "binary";
    public const string Date = "date";
    public const string DateTime = "date-time";
    public const string Double = "double";
    public const string Float = "float";
    public const string Int = "int32";
    public const string Long = "int64";
    public const string Password = "password";
}
#endif
