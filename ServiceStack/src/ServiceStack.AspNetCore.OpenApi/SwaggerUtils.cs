using System.Reflection;
using System.Text.Json.Serialization;

namespace ServiceStack.AspNetCore.OpenApi;

public static class SwaggerUtils
{
    public static Func<PropertyInfo, bool> IgnoreProperty { get; set; } = DefaultIgnoreProperty;
    
    public static bool DefaultIgnoreProperty(PropertyInfo pi)
    {
        var propAttrs = pi.AllAttributes();
        return propAttrs.Any(x => x is ObsoleteAttribute or JsonIgnoreAttribute
            or Swashbuckle.AspNetCore.Annotations.SwaggerIgnoreAttribute);
    }
}
