using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Render a Primary Tailwind Link or Button
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/PrimaryButton.png)
/// </remarks>
public partial class PrimaryButton : UiComponentBase
{
    [Parameter] public string type { get; set; } = "button";
    [Parameter] public string? href { get; set; }
    [Parameter] public string? title { get; set; }
    [Parameter] public string? target { get; set; }
    [Parameter] public ButtonStyle Style { get; set; } = ButtonStyle.Indigo;
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> onclick { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    public virtual IReadOnlyDictionary<string, object>? IncludeAttributes => TextInputBase.SanitizeAttributes(AdditionalAttributes);
}
