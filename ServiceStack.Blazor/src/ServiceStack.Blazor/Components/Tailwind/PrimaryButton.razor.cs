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
    public virtual IReadOnlyDictionary<string, object>? IncludeAttributes => SanitizeAttributes(AdditionalAttributes);

    public static string BaseClass { get; set; } = "inline-flex justify-center rounded-md border border-transparent py-2 px-4 text-sm font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 dark:ring-offset-black";
    public static string GetStyleClass(ButtonStyle style) => style switch
    {
        ButtonStyle.Blue => "text-white bg-blue-600 hover:bg-blue-700 focus:ring-indigo-500 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800",
        ButtonStyle.Purple => "text-white bg-purple-600 hover:bg-purple-700 focus:ring-indigo-500 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800",
        ButtonStyle.Red => "focus:ring-red-500 text-white bg-red-600 hover:bg-red-700 focus:ring-red-500 dark:bg-red-600 dark:hover:bg-red-700 dark:focus:ring-red-500",
        ButtonStyle.Green => "focus:ring-green-300 text-white bg-green-600 hover:bg-green-700 focus:ring-green-500 dark:bg-green-600 dark:hover:bg-green-700 dark:focus:ring-green-500",
        ButtonStyle.Sky => "focus:ring-sky-300 text-white bg-sky-600 hover:bg-sky-700 focus:ring-sky-500 dark:bg-sky-600 dark:hover:bg-sky-700 dark:focus:ring-sky-500",
        ButtonStyle.Cyan => "focus:ring-cyan-300 text-white bg-cyan-600 hover:bg-cyan-700 focus:ring-cyan-500 dark:bg-cyan-600 dark:hover:bg-cyan-700 dark:focus:ring-cyan-500",
        _ => "focus:ring-2 focus:ring-offset-2 text-white bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800"
    };
    public static string Classes(ButtonStyle style) => BaseClass + " " + GetStyleClass(style);
}
