using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Display Customizable Icon
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/Icon.png)
/// </remarks>
public partial class Icon : UiComponentBase
{
    [Parameter] public string? Svg { get; set; }
    [Parameter] public string? Src { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public ImageInfo? Image { get; set; }

    public string? ToSvg()
    {
        var svg = Svg ?? Image?.Svg;
        if (svg == null || !svg.StartsWith("<svg ")) return null;

        var cls = ClassNames(Image?.Cls, Class ?? "w-5 h-5");
        var attrs = new List<string?>
        {
            $"class=\"{cls}\"",
            svg.IndexOf("role") == -1 ? "role=\"img\"" : null,
            svg.IndexOf("aria-hidden") == -1 ? "aria-hidden=\"true\"" : null,
        }.Where(x => x != null);
        var ret = $"<svg {string.Join(' ', attrs)} " + svg[4..];
        return ret;
    }

    public string? ToImg()
    {
        var src = Src ?? Image?.Uri;
        if (src == null) return null;

        var cls = ClassNames(Image?.Cls, Class ?? "w-5 h-5");
        var attrs = new List<string?>
        {
            $"class=\"{cls}\"",
            Alt != null ? $"alt=\"{Alt.HtmlEncode()}\"" : null
        }.Where(x => x != null);

        var ret = $"<img src=\"{BlazorConfig.Instance.AssetsPathResolver(src)}\" {string.Join(' ', attrs)} onerror=\"Files.iconOnError(this)\">";
        return ret;
    }

    public string ToHtml() => ToSvg() ?? ToImg() ?? "";
}
