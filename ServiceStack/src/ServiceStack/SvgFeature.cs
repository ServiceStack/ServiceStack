#if NETCORE        
using ServiceStack.Host;
#else
using System.Web;
#endif
#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class SvgFeature : IPlugin, IPostInitPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Svg;
    /// <summary>
    /// RequestLogs service Route, default is /metadata/svg
    /// </summary>
    public string RoutePath { get; set; } = "/metadata/svg";

    /// <summary>
    /// Custom Validation for SVG Metadata Page, return false to deny access, e.g. only allow in DebugMode with:
    /// ValidateFn = req => HostContext.DebugMode
    /// </summary>
    public Func<IRequest, bool> ValidateFn { get; set; }

    public void Register(IAppHost appHost)
    {
        appHost.RawHttpHandlers.Add(req => req.PathInfo == RoutePath
            ? (string.IsNullOrEmpty(req.QueryString["id"]) || string.IsNullOrEmpty(req.QueryString["format"]) 
                ? new SharpPageHandler(HtmlTemplates.GetSvgTemplatePath()) {
                    ValidateFn = ValidateFn,
                    Context = SharpPageHandler.NewContext(appHost),
                }
                : new SvgFormatHandler {
                    Id = req.QueryString["id"], 
                    Format = req.QueryString["format"], 
                    Fill = req.QueryString["fill"],
                })
            : req.PathInfo.StartsWith(RoutePath)
                ? new SvgFormatHandler(req.PathInfo.Substring(RoutePath.Length+1)) {
                    Fill = req.QueryString["fill"]
                }
                : null);
        
#if NET8_0_OR_GREATER
        (appHost as IAppHostNetCore).MapEndpoints(routeBuilder =>
        {
            var tag = GetType().Name;
            routeBuilder.MapGet(RoutePath + "/{**path}", httpContext => httpContext.ProcessRequestAsync(
                httpContext.Request.Path.ToString() == RoutePath 
                    ? string.IsNullOrEmpty(httpContext.Request.Query["id"]) || string.IsNullOrEmpty(httpContext.Request.Query["format"]) 
                        ? new SharpPageHandler(HtmlTemplates.GetSvgTemplatePath()) {
                            ValidateFn = ValidateFn,
                            Context = SharpPageHandler.NewContext(appHost),
                        }
                        : new SvgFormatHandler {
                            Id = httpContext.Request.Query["id"], 
                            Format = httpContext.Request.Query["format"], 
                            Fill = httpContext.Request.Query["fill"],
                        }
                    : new SvgFormatHandler(httpContext.Request.Path.ToString().Substring(RoutePath.Length+1)) {
                        Fill = httpContext.Request.Query["fill"]
                    })
                )
                .WithMetadata<string>(name:nameof(SvgFeature), tag:tag, contentType:MimeTypes.PlainText);
        });
#endif
        
        var btnSvgCssFile = appHost.VirtualFileSources.GetFile("/css/buttons-svg.css");
        if (btnSvgCssFile != null)
        {
            var btnSvgCss = btnSvgCssFile.ReadAllText();
            foreach (var name in new[]{ "svg-auth", "svg-icons" })
            {
                if (Svg.CssFiles.ContainsKey(name) && !Svg.AppendToCssFiles.ContainsKey(name))
                {
                    Svg.AppendToCssFiles[name] = btnSvgCss;
                }
            }
        }

        appHost.ConfigurePlugin<MetadataFeature>(
            feature => feature.AddDebugLink(RoutePath, "SVG Images"));
    }

    static void AppendEntry(StringBuilder sb, string name, string dataUri)
    {
        sb.Append(".svg-").Append(name).Append(", .fa-").Append(name).AppendLine(" {");
        sb.Append("background-image: url(\"").Append(dataUri.Replace("\"","'")).AppendLine("\");");
        sb.AppendLine("}");
    }

    public static void WriteDataUris(StringBuilder sb, List<string> dataUris)
    {
        foreach (var name in dataUris)
        {
            AppendEntry(sb, name, Svg.GetDataUri(name));
        }
    }

    public static void WriteAdjacentCss(StringBuilder sb, List<string> dataUris, Dictionary<string, string> adjacentCssRules)
    {
        foreach (var entry in adjacentCssRules)
        {
            var i = 0;
            foreach (var name in dataUris)
            {
                if (i++ > 0)
                    sb.Append(", ");
                sb.Append(entry.Key).Append(name);
            }
            sb.Append(" { ").Append(entry.Value).AppendLine(" }");
        }
    }

    public static void WriteSvgCssFile(IVirtualFiles vfs, string name, 
        List<string> dataUris, 
        Dictionary<string, string> adjacentCssRules = null, 
        Dictionary<string, string> appendToCssFiles = null)
    {
        var sb = StringBuilderCache.Allocate();
        WriteDataUris(sb, dataUris);

        if (adjacentCssRules != null)
        {
            WriteAdjacentCss(sb, dataUris, adjacentCssRules);
        }

        if (appendToCssFiles != null)
        {
            if (appendToCssFiles.TryGetValue(name, out var suffix))
            {
                sb.AppendLine(suffix);
            }
        }

        var css = StringBuilderCache.ReturnAndFree(sb);
            
        if (Svg.CssFillColor.TryGetValue(name, out var fillColor))
        {
            css = Svg.Fill(css, fillColor);
        }
            
        vfs.WriteFile($"/css/{name}.css", css);
    }

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
        if (memFs == null)
            return;

        appHost.AfterInitCallbacks.Add(host => 
        {
            foreach (var cssFile in Svg.CssFiles)
            {
                WriteSvgCssFile(memFs, cssFile.Key, cssFile.Value, Svg.AdjacentCssRules, Svg.AppendToCssFiles);
            }
        });
    }
}

public class SvgFormatHandler : HttpAsyncTaskHandler
{
    public string Id { get; set; }
    public string Format { get; set; }
        
    public string Fill { get; set; }

    public SvgFormatHandler() {}

    public SvgFormatHandler(string fileName) //name.svg, name.datauri, name.css
    {
        Id = fileName.LeftPart('.');
        Format = fileName.RightPart('.');
    }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        var svg = Svg.GetImage(Id, Fill);
        if (svg == null)
        {
            httpRes.StatusCode = 404;
            httpRes.StatusDescription = "SVG Image was not found";
        }
        else if (Format == "svg")
        {
            httpRes.ContentType = MimeTypes.ImageSvg;
            await httpRes.WriteAsync(svg);
        }
        else if (Format == "css")
        {
            httpRes.ContentType = "text/css";
            var css = $".svg-{Id} {{\n  {Svg.InBackgroundImageCss(Svg.GetImage(Id, Fill))}\n}}\n";
            await httpRes.WriteAsync(css);
        }
        else if (Format == "datauri")
        {
            var dataUri = Svg.GetDataUri(Id, Fill);
            httpRes.ContentType = MimeTypes.PlainText;
            await httpRes.WriteAsync(dataUri);
        }
        else
        {
            httpRes.StatusCode = 400;
            httpRes.StatusDescription = "Unknown format, valid formats: svg, css, datauri";
        }
        await httpRes.EndRequestAsync();
    }
}

public static class Svg
{
    public static string LightColor { get; set; } = "#dddddd";
    public static string[] FillColors { get; set; } = { "#ffffff", "#556080" };

    public static Dictionary<string, List<string>> CssFiles { get; set; } = new Dictionary<string, List<string>> {
        ["svg-auth"]  = new() { "servicestack", "apple", "twitter", "github", "google", "facebook", "microsoft", "linkedin", },
        ["svg-icons"] = new() { "male", "female", "male-business", "female-business", "male-color", "female-color", "users", },
    };

    public static Dictionary<string, string> CssFillColor { get; set; } = new();
        
    public static Dictionary<string, string> AppendToCssFiles { get; set; } = new();

    public static Dictionary<string,string> AdjacentCssRules { get; set; } = new() {
        //[".btn-md .fa-"] = "css", // Generates: .btn-md .fa-svg1, .btn-md .fa-svg2 { css } 
    };

    public static Dictionary<string,string> Images { get; set; } = new() {
        [Logos.ServiceStack]   = SvgIcons.ServiceStack,
        [Logos.Apple]          = SvgIcons.Apple,
        [Logos.Twitter]        = SvgIcons.Twitter,
        [Logos.GitHub]         = SvgIcons.GitHub,
        [Logos.Google]         = SvgIcons.Google,
        [Logos.Facebook]       = SvgIcons.Facebook,
        [Logos.Microsoft]      = SvgIcons.Microsoft,
        [Logos.LinkedIn]       = SvgIcons.LinkedIn,

        [Icons.Male]           = SvgIcons.Male,
        [Icons.Female]         = SvgIcons.Female,
        [Icons.MaleBusiness]   = SvgIcons.MaleBusiness,
        [Icons.FemaleBusiness] = SvgIcons.FemaleBusiness,
        [Icons.MaleColor]      = SvgIcons.MaleColor,
        [Icons.FemaleColor]    = SvgIcons.FemaleColor,
        [Icons.Users]          = SvgIcons.Users,
        
        [Icons.Tasks]          = SvgIcons.Tasks,
        [Icons.Stats]          = SvgIcons.Stats,
        [Icons.Logs]           = SvgIcons.Logs,
        [Icons.Completed]      = SvgIcons.Completed,
        [Icons.Failed]         = SvgIcons.Failed,
    };
        
    public static Dictionary<string,string> DataUris { get; set; } = new();
        
    public static string GetImage(string name) => Images.TryGetValue(name, out var svg) ? svg : null;
    public static string GetImage(string name, string fillColor) => Fill(GetImage(name), fillColor);

    public static StaticContent GetStaticContent(string name) => new(GetImage(name).ToUtf8Bytes(), MimeTypes.ImageSvg);

    public static string GetDataUri(string name)
    {
        if (DataUris.TryGetValue(name, out var dataUri))
            return dataUri;
            
        if (!Images.TryGetValue(name, out var svg))
            return null;

        DataUris[name] = dataUri = ToDataUri(svg);
        return dataUri;
    }
    public static string GetDataUri(string name, string fillColor) => Fill(GetDataUri(name), fillColor);

    public static string Create(string body, string fill="none", string viewBox="0 0 24 24", string stroke="currentColor") =>
        $"<svg xmlns='http://www.w3.org/2000/svg' fill='{fill}' viewBox='{viewBox}' stroke='{stroke}' aria-hidden='true'>{body}</svg>";

    public static ImageInfo CreateImage(string body, string fill="none", string viewBox="0 0 24 24", string stroke="currentColor") =>
        ImageSvg(Create(body:body, fill:fill, viewBox:viewBox, stroke:stroke));

    public static ImageInfo ImageSvg(string svg) => new() { Svg = svg };
    public static ImageInfo ImageUri(string uri) => new() { Uri = uri };
        
    public static class Body
    {
        public static string Home  = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' />";
        public static string Key = "<path fill='currentColor' d='M12.65 10A5.99 5.99 0 0 0 7 6c-3.31 0-6 2.69-6 6s2.69 6 6 6a5.99 5.99 0 0 0 5.65-4H17v4h4v-4h2v-4H12.65zM7 14c-1.1 0-2-.9-2-2s.9-2 2-2s2 .9 2 2s-.9 2-2 2z'/>";
        public static string User = "<path fill='currentColor' d='M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z'/>";
        public static string Role = "<path fill='currentColor' d='M21.953 9.108a3.75 3.75 0 0 1 4.094 0l14.414 9.393l-14.414 9.392a3.75 3.75 0 0 1-4.094 0L7.54 18.501zm5.46-2.094a6.25 6.25 0 0 0-6.825 0L4.605 17.429A1.25 1.25 0 0 0 4 18.5v13.25a1.25 1.25 0 0 0 2.5 0V20.807L11 23.74v12.01q0 .12.005.238c.011.318.145.62.372.844A17.95 17.95 0 0 0 24 42c4.917 0 9.376-1.973 12.623-5.168a1.25 1.25 0 0 0 .373-.844q.004-.119.004-.238V23.74l6.432-4.192a1.25 1.25 0 0 0 0-2.095zM34.5 25.369v10.033A15.44 15.44 0 0 1 24 39.5a15.44 15.44 0 0 1-10.5-4.098V25.369l7.088 4.619a6.25 6.25 0 0 0 6.824 0z'/>";
        public static string UserDetails = "<path fill='currentColor' d='M15 11h7v2h-7zm1 4h6v2h-6zm-2-8h8v2h-8zM4 19h10v-1c0-2.757-2.243-5-5-5H7c-2.757 0-5 2.243-5 5v1h2zm4-7c1.995 0 3.5-1.505 3.5-3.5S9.995 5 8 5S4.5 6.505 4.5 8.5S6.005 12 8 12z'/>";
        public static string UserShield = "<path fill='currentColor' d='M12 1L3 5v6c0 5.55 3.84 10.74 9 12c5.16-1.26 9-6.45 9-12V5l-9-4m0 4a3 3 0 0 1 3 3a3 3 0 0 1-3 3a3 3 0 0 1-3-3a3 3 0 0 1 3-3m5.13 12A9.69 9.69 0 0 1 12 20.92A9.69 9.69 0 0 1 6.87 17c-.34-.5-.63-1-.87-1.53c0-1.65 2.71-3 6-3s6 1.32 6 3c-.24.53-.53 1.03-.87 1.53Z'/>";
        public static string Users = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' />";
        public static string Table = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M3 8V6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2M3 8v6m0-6h6m12 0v6m0-6H9m12 6v4a2 2 0 0 1-2 2H9m12-6H9m-6 0v4a2 2 0 0 0 2 2h4m-6-6h6m0-6v6m0 0v6m6-12v12'/>";
        public static string History = "<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2'><path d='M3 3v5h5'/><path d='M3.05 13A9 9 0 1 0 6 5.3L3 8'/><path d='M12 7v5l4 2'/></g>";
        public static string Lock = "<path fill='currentColor' fill-opacity='.886' d='M16 8a3 3 0 0 1 3 3v8a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3v-8a3 3 0 0 1 3-3V6.5a4.5 4.5 0 1 1 9 0V8ZM7 9a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h9a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2H7Zm8-1V6.5a3.5 3.5 0 0 0-7 0V8h7Zm-3.5 6a1.5 1.5 0 1 0 0 3a1.5 1.5 0 0 0 0-3Zm0-1a2.5 2.5 0 1 1 0 5a2.5 2.5 0 0 1 0-5Z'/>";
        public static string Logs = "<path fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4'/>";
        public static string Profiling = "<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2'><path d='M10 2h4m-2 12l3-3'/><circle cx='12' cy='14' r='8'/></g>";
        public static string Database = "<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='1'><ellipse cx='12' cy='6' rx='8' ry='3'/><path d='M4 6v6a8 3 0 0 0 16 0V6'/><path d='M4 12v6a8 3 0 0 0 16 0v-6'/></g>";
        public static string Redis = "<path d='M10.5 2.661l.54.997-1.797.644 2.409.218.748 1.246.467-1.121 2.077-.208-1.61-.613.426-1.017-1.578.519zm6.905 2.077L13.76 6.182l3.292 1.298.353-.146 3.293-1.298zm-10.51.312a2.97 1.153 0 0 0-2.97 1.152 2.97 1.153 0 0 0 2.97 1.153 2.97 1.153 0 0 0 2.97-1.153 2.97 1.153 0 0 0-2.97-1.152zM24 6.805s-8.983 4.278-10.395 4.953c-1.226.561-1.901.561-3.261.094C8.318 11.022 0 7.241 0 7.241v1.038c0 .24.332.499.966.8 1.277.613 8.34 3.677 9.45 4.206 1.112.53 1.9.54 3.313-.197 1.412-.738 8.049-3.905 9.326-4.57.654-.342.945-.602.945-.84zm-10.042.602L8.39 8.26l3.884 1.61zM24 10.637s-8.983 4.279-10.395 4.954c-1.226.56-1.901.56-3.261.093C8.318 14.854 0 11.074 0 11.074v1.038c0 .238.332.498.966.8 1.277.612 8.34 3.676 9.45 4.205 1.112.53 1.9.54 3.313-.197 1.412-.737 8.049-3.905 9.326-4.57.654-.332.945-.602.945-.84zm0 3.842l-10.395 4.954c-1.226.56-1.901.56-3.261.094C8.318 18.696 0 14.916 0 14.916v1.038c0 .239.332.499.966.8 1.277.613 8.34 3.676 9.45 4.206 1.112.53 1.9.54 3.313-.198 1.412-.737 8.049-3.904 9.326-4.569.654-.343.945-.613.945-.841z'/>";
        public static string Command = "<path fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M9 15v3a3 3 0 1 1-3-3zm0 0h6m-6 0V9m6 6v3a3 3 0 1 0 3-3zm0 0V9m0 0H9m6 0V6a3 3 0 1 1 3 3zM9 9V6a3 3 0 1 0-3 3z'/>";
        public static string Keys = "<path fill='currentColor' d='M10.32 2.013A4 4 0 0 0 6.162 7.13l-3.987 3.986a.6.6 0 0 0-.176.424v2.86a.6.6 0 0 0 .6.6h2.8a.6.6 0 0 0 .6-.6V13h1.9a.6.6 0 0 0 .6-.6v-1.693l.735-.735a5.5 5.5 0 0 1-.569-.846l-.99.991a.6.6 0 0 0-.176.424V12H5.6a.6.6 0 0 0-.6.6V14H3v-2.293l4.32-4.32l-.118-.303a3 3 0 0 1 1.96-3.965c.33-.423.72-.796 1.157-1.106M13.5 6.25a.75.75 0 1 0 0-1.5a.75.75 0 0 0 0 1.5M9 6.5a4.5 4.5 0 1 1 7 3.742v2.05l.783.784a.6.6 0 0 1 0 .848L15.707 15l1.068 1.067a.6.6 0 0 1-.05.893l-2.35 1.88a.6.6 0 0 1-.75 0l-2.4-1.92a.6.6 0 0 1-.225-.468v-6.21A4.5 4.5 0 0 1 9 6.5M13.5 3a3.5 3.5 0 0 0-1.75 6.532a.5.5 0 0 1 .25.433v6.295l2 1.6l1.751-1.401l-1.034-1.035a.6.6 0 0 1 0-.848l1.076-1.076l-.617-.617a.6.6 0 0 1-.176-.424V9.965a.5.5 0 0 1 .25-.433A3.5 3.5 0 0 0 13.5 3'/>";
        public static string Analytics = "<path fill='currentColor' d='M13 5h2v14h-2zm-2 4H9v10h2zm-4 4H5v6h2zm12 0h-2v6h2z'/>";
    }

    public static class Logos
    {
        public const string ServiceStack = "servicestack";
        public const string Apple = "apple";
        public const string Twitter = "twitter";
        public const string GitHub = "github";
        public const string Google = "google";
        public const string Facebook = "facebook";
        public const string Microsoft = "microsoft";
        public const string LinkedIn = "linkedin";
    }

    public static class Icons
    {
        public const string DefaultProfile = Male;
        
        public const string User = "user";
        public const string Role = "role";
        public const string Male = "male";
        public const string Female = "female";
        public const string MaleBusiness = "male-business";
        public const string FemaleBusiness = "female-business";
        public const string MaleColor = "male-color";
        public const string FemaleColor = "female-color";
        public const string Users = "users";
        public const string Tasks = "tasks";
        public const string Stats = "stats";
        public const string Logs  = "logs";
        public const string Completed  = "completed";
        public const string Failed  = "failed";
    }

    public static string Fill(string svg, string fillColor)
    {
        if (string.IsNullOrEmpty(svg) || string.IsNullOrEmpty(fillColor)) 
            return svg;

        foreach (var color in FillColors)
        {
            svg = svg.Replace(color, fillColor);
            if (color[0] == '#' || fillColor[0] == '#')
            {
                svg = svg.Replace(
                    color[0] == '#' ? "%23" + color.Substring(1) : color, 
                    fillColor[0] == '#' ? "%23" + fillColor.Substring(1) : fillColor);
            }
        }
        return svg;
    }

    public static string Encode(string svg)
    {
        if (string.IsNullOrEmpty(svg))
            return null;

        //['%','#','<','>','?','[','\\',']','^','`','{','|','}'].map(x => `.Replace("${x}","` + encodeURIComponent(x) + `")`).join('')
        return svg.Replace("\r", " ").Replace("\n", "")                
            .Replace("\"","'")
            .Replace("%","%25")
            .Replace("#","%23")
            .Replace("<","%3C")
            .Replace(">","%3E")
            .Replace("?","%3F")
            .Replace("[","%5B")
            .Replace("\\","%5C")
            .Replace("]","%5D")
            .Replace("^","%5E")
            .Replace("`","%60")
            .Replace("{","%7B")
            .Replace("|","%7C")
            .Replace("}","%7D");
    }

    public static string ToDataUri(string svg) => "data:image/svg+xml," + Encode(svg);
    public static string GetBackgroundImageCss(string name) => InBackgroundImageCss(GetImage(name));
    public static string GetBackgroundImageCss(string name, string fillColor) => InBackgroundImageCss(GetImage(name, fillColor));

    public static string InBackgroundImageCss(string svg) => "background-image: url(\"" + ToDataUri(svg) + "\");";

    internal static long ImagesAdded = 0;

    public static void AddImage(string svg, string name, string cssFile=null)
    {
        Interlocked.Increment(ref ImagesAdded);
            
        svg = svg.Trim();
        if (svg.IndexOf("http://www.w3.org/2000/svg", StringComparison.OrdinalIgnoreCase) < 0)
        {
            svg = svg.LeftPart(' ') + " xmlns='http://www.w3.org/2000/svg' " + svg.RightPart(' ');
        }
            
        Images[name] = svg;

        if (cssFile != null)
        {
            if (CssFiles.TryGetValue(cssFile, out var cssFileSvgs))
            {
                cssFileSvgs.Add(name);
            }
            else
            {
                CssFiles[cssFile] = new List<string> { name };
            }
        }
    }

    public static void Load(IVirtualDirectory svgDir)
    {
        if (svgDir == null)
            throw new ArgumentNullException(nameof(svgDir));
            
        var context = new ScriptContext {
            ScriptBlocks = {
                new SvgScriptBlock()
            }
        }.Init();

        var log = LogManager.GetLogger(typeof(Svg));
        foreach (var svgGroupDir in svgDir.GetDirectories())
        {
            foreach (var file in svgGroupDir.GetFiles())
            {
                if (file.Extension == "svg")
                {
                    var svg = file.ReadAllText();
                    AddImage(svg, file.Name.WithoutExtension(), svgGroupDir.Name);
                }
                else if (file.Extension == "html")
                {
                    var script = file.ReadAllText();
                    var svg = context.EvaluateScript(script);
                    if (svg.StartsWith("<svg"))
                    {
                        AddImage(svg, file.Name.WithoutExtension(), svgGroupDir.Name);
                    }
                    else
                    {
                        log.Warn($"Unable to load #Script SVG '{file.Name}'");
                    }
                }
            }
        }

        // Also load any .html #Script files which can register svg using {{#svg name group}} #Script block
        foreach (var svgScript in svgDir.GetAllMatchingFiles("*.html"))
        {
            var script = svgScript.ReadAllText();
            var output = context.EvaluateScript(script);
        }
    }
}