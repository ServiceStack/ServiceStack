using ServiceStack;

namespace MyApp.Client.Components;

public class ComponentConfig
{
    public static List<AutoQueryConvention> DefaultFilters = new List<AutoQueryConvention> {
        Definition("=","%"),
        Definition("!=","%!"),
        Definition("<","<%"),
        Definition("<=","%<"),
        Definition(">","%>"),
        Definition(">=",">%"),
        Definition("In","%In"),
        Definition("Starts With","%StartsWith", x => x.Types = "string"),
        Definition("Contains","%Contains", x => x.Types = "string"),
        Definition("Ends With","%EndsWith", x => x.Types = "string"),
        Definition("Exists","%IsNotNull", x => x.ValueType = "none"),
        Definition("Not Exists","%IsNull", x => x.ValueType = "none"),
    };

    static AutoQueryConvention Definition(string name, string value, Action<AutoQueryConvention>? fn = null) =>
        X.Apply(new() { Name = name, Value = value }, fn);
}
