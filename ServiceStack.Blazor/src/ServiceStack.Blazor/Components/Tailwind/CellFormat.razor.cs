using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class CellFormat
{
    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    [Parameter, EditorRequired] public MetadataType Type { get; set; } = default!;
    [Parameter, EditorRequired] public MetadataPropertyType PropType { get; set; } = default!;
    [Parameter, EditorRequired] public object ModelValue { get; set; } = default!;
    [Parameter, EditorRequired] public object Value { get; set; } = default!;

    public ImageInfo? Icon { get; set; }
    public string? Label { get; set; }
    public string? Title { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Icon = null;
        Label = null;
        Title = null;
        
        var refInfo = PropType.Ref;
        if (refInfo?.Model == null)
            return;

        var modelProps = Type.Properties;

        var complexRefProp = modelProps.FirstOrDefault(p => p.Type == refInfo.Model);
        if (complexRefProp == null)
            return;

        var model = ModelValue.ToObjectDictionary();
        var complexRefValue = model.GetIgnoreCase(complexRefProp.Name)?.ToObjectDictionary();
        Label = complexRefValue != null && refInfo!.RefLabel != null
            ? complexRefValue.GetIgnoreCase(refInfo.RefLabel).ConvertTo<string>()
            : null;
        if (Label == null)
            return;

        Title = Value.GetType().IsComplexType() ? null : $"{refInfo.Model} {Value}";

        var refType = AppMetadata?.GetType(refInfo.Model);
        Icon = refType != null
            ? refType.Icon
            : refInfo?.ModelType?.GetIcon();
    }

}