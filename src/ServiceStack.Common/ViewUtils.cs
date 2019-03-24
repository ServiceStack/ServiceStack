using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// High-level Input options for rendering HTML Input controls
    /// </summary>
    public class InputOptions
    {
        public bool Inline { get; set; }
        
        public string Label { get; set; }
        
        public string LabelClass { get; set; }
        
        public string ErrorClass { get; set; }

        public string Help { get; set; }
        
        public string Size { get; set; }
        
        public object Values { get; set; }

        public IEnumerable<KeyValuePair<string, string>> InputValues
        {
            set => Values = value;
        }

        public bool PreserveValue { get; set; } = true;

        public bool ShowErrors { get; set; } = true;
    }

    /// <summary>
    /// Customize JS/CSS/HTML bundles
    /// </summary>
    public class BundleOptions
    {
        /// <summary>
        /// List of file and directory sources to include in this bundle, directory sources must end in `/`.
        /// Sources can include prefixes to specify which Virtual File System Source to use, options:
        /// 'content:' (ContentRoot HostContext.VirtualFiles), 'filesystem:' (WebRoot FileSystem), 'memory:' (WebRoot Memory)
        /// </summary>
        public List<string> Sources { get; set; } = new List<string>();
        
        /// <summary>
        /// Write bundled file to this Virtual Path
        /// </summary>
        public string OutputTo { get; set; }
        /// <summary>
        /// If needed, use alternative OutputTo Virtual Path in html tag
        /// </summary>
        public string OutputWebPath { get; set; }
        /// <summary>
        /// Whether to minify sources in bundle (default true)
        /// </summary>
        public bool Minify { get; set; } = true;
        /// <summary>
        /// Whether to save to disk or Memory File System (default Memory)
        /// </summary>
        public bool SaveToDisk { get; set; }
        /// <summary>
        /// Whether to return cached bundle if exists (default true)
        /// </summary>
        public bool Cache { get; set; } = true;
        /// <summary>
        /// Whether to bundle and emit single or not bundle and emit multiple html tags
        /// </summary>
        public bool Bundle { get; set; } = true;
        /// <summary>
        /// Whether to call AMD define for CommonJS modules
        /// </summary>
        public bool RegisterModuleInAmd { get; set; }
    }

    public class TextDumpOptions
    {
        public TextStyle HeaderStyle { get; set; }
        public string Caption { get; set; }
        public string CaptionIfEmpty { get; set; }
        public bool IncludeRowNumbers { get; set; } = true;

        public DefaultScripts Defaults { get; set; } = ViewUtils.DefaultScripts;
        
        internal int Depth { get; set; }
        internal bool HasCaption { get; set; }

        public static TextDumpOptions Parse(Dictionary<string, object> options, DefaultScripts defaults=null)
        {
            return new TextDumpOptions 
            {
                HeaderStyle = options.TryGetValue("headerStyle", out var oHeaderStyle)
                    ? oHeaderStyle.ConvertTo<TextStyle>()
                    : TextStyle.SplitCase,
                Caption = options.TryGetValue("caption", out var caption)
                    ? caption?.ToString()
                    : null,
                CaptionIfEmpty = options.TryGetValue("captionIfEmpty", out var captionIfEmpty)
                    ? captionIfEmpty?.ToString()
                    : null,
                IncludeRowNumbers = !options.TryGetValue("rowNumbers", out var rowNumbers) 
                    || (!(rowNumbers is bool b) || b),
                Defaults = defaults ?? ViewUtils.DefaultScripts,
            };
        }
    }

    public class HtmlDumpOptions
    {
        public string Id { get; set; }
        public string ClassName { get; set; }
        public string ChildClass { get; set; }

        public TextStyle HeaderStyle { get; set; }
        public string HeaderTag { get; set; }
        
        public string Caption { get; set; }
        public string CaptionIfEmpty { get; set; }

        public DefaultScripts Defaults { get; set; } = ViewUtils.DefaultScripts;
        
        internal int Depth { get; set; }
        internal int ChildDepth { get; set; } = 1;
        internal bool HasCaption { get; set; }
        
        public static HtmlDumpOptions Parse(Dictionary<string, object> options, DefaultScripts defaults=null)
        {
            return new HtmlDumpOptions 
            {
                Id = options.TryGetValue("id", out var oId)
                    ? (string)oId
                    : null,
                ClassName = options.TryGetValue("className", out var oClassName)
                    ? (string)oClassName
                    : null,
                ChildClass = options.TryGetValue("childClass", out var oChildClass)
                    ? (string)oChildClass
                    : null,

                HeaderStyle = options.TryGetValue("headerStyle", out var oHeaderStyle)
                    ? oHeaderStyle.ConvertTo<TextStyle>()
                    : TextStyle.SplitCase,
                HeaderTag = options.TryGetValue("headerTag", out var oHeaderTag)
                    ? (string)oHeaderTag
                    : null,
                
                Caption = options.TryGetValue("caption", out var caption)
                    ? caption?.ToString()
                    : null,
                CaptionIfEmpty = options.TryGetValue("captionIfEmpty", out var captionIfEmpty)
                    ? captionIfEmpty?.ToString()
                    : null,
                Defaults = defaults ?? ViewUtils.DefaultScripts,
            };
        }
    }

    public enum TextStyle
    {
        None,
        SplitCase,
        Humanize,
        TitleCase,
        PascalCase,
        CamelCase,
    }

    /// <summary>
    /// Shared Utils shared between different Template Filters and Razor Views/Helpers
    /// </summary>
    public static class ViewUtils
    {
        internal static readonly DefaultScripts DefaultScripts = new DefaultScripts();
        private static readonly HtmlScripts HtmlScripts = new HtmlScripts();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(object test) => test == null || test == JsNull.Value;
        
        public static CultureInfo GetDefaultCulture(this DefaultScripts defaultScripts) => 
            defaultScripts?.Context?.Args[ScriptConstants.DefaultCulture] as CultureInfo ?? ScriptConfig.DefaultCulture;
        
        public static string GetDefaultTableClassName(this DefaultScripts defaultScripts) => 
            defaultScripts?.Context?.Args[ScriptConstants.DefaultTableClassName] as string;

        public static string TextDump(this object target) => DefaultScripts.TextDump(target, null); 
        public static string TextDump(this object target, TextDumpOptions options) => DefaultScripts.TextDump(target, options); 
        
        public static string HtmlDump(object target) => HtmlScripts.HtmlDump(target, null); 
        public static string HtmlDump(object target, HtmlDumpOptions options) => HtmlScripts.HtmlDump(target, options); 
        
        public static string StyleText(string text, TextStyle textStyle)
        {
            if (text == null) return null;
            switch (textStyle)
            {
                case TextStyle.SplitCase:
                    return DefaultScripts.splitCase(text);
                case TextStyle.Humanize:
                    return DefaultScripts.humanize(text);
                case TextStyle.TitleCase:
                    return DefaultScripts.titleCase(text);
                case TextStyle.PascalCase:
                    return DefaultScripts.pascalCase(text);
                case TextStyle.CamelCase:
                    return DefaultScripts.camelCase(text);
            }
            return text;
        }
        
        public static string HtmlHiddenInputs(IEnumerable<KeyValuePair<string,object>> inputValues)
        {
            if (inputValues != null)
            {
                var sb = StringBuilderCache.Allocate();
                foreach (var entry in inputValues)
                {
                    sb.AppendLine($"<input type=\"hidden\" name=\"{entry.Key.HtmlEncode()}\" value=\"{entry.Value?.ToString().HtmlEncode()}\">");
                }
                return StringBuilderCache.ReturnAndFree(sb);
            }
            return null;
        }

        internal static object GetItem(this IRequest httpReq, string key)
        {
            if (httpReq == null) return null;

            httpReq.Items.TryGetValue(key, out var value);
            return value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ResponseStatus GetErrorStatus(IRequest req) => 
            req.GetItem("__errorStatus") as ResponseStatus; // Keywords.ErrorStatus

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasErrorStatus(IRequest req) => GetErrorStatus(req) != null; 
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormQuery(IRequest req, string name) => req.FormData[name] ?? req.QueryString[name];
        public static string[] FormQueryValues(IRequest req, string name)
        {
            var values = req.Verb == HttpMethods.Post 
                ? req.FormData.GetValues(name) 
                : req.QueryString.GetValues(name);

            return values?.Length == 1 // if it's only a single item can be returned in comma-delimited list
                ? values[0].Split(',') 
                : values ?? TypeConstants.EmptyStringArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormValue(IRequest req, string name) => FormValue(req, name, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormValue(IRequest req, string name, string defaultValue) => HasErrorStatus(req) 
            ? FormQuery(req, name) 
            : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] FormValues(IRequest req, string name) => HasErrorStatus(req) 
            ? FormQueryValues(req, name) 
            : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FormCheckValue(IRequest req, string name)
        {
            var value = FormValue(req, name);
            return value == "true" || value == "True" || value == "t" || value == "on" || value == "1";
        }

        public static string GetParam(IRequest req, string name) //sync with IRequest.GetParam()
        {
            string value;
            if ((value = req.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
            if ((value = req.QueryString[name]) != null) return value;
            if ((value = req.FormData[name]) != null) return value;

            //IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
            //TryGetValue is not happy with null dictionary keys, so we should bail out here
            if (string.IsNullOrEmpty(name)) return null;

            if (req.Cookies.TryGetValue(name, out var cookie)) return cookie.Value;

            if (req.Items.TryGetValue(name, out var oValue)) return oValue.ToString();

            return null;
        }

        /// <summary>
        /// Comma delimited field names
        /// </summary>
        public static List<string> ToVarNames(string fieldNames) =>
            fieldNames.Split(',').Map(x => x.Trim());

        public static IEnumerable<string> ToStrings(string filterName, object arg)
        {
            if (arg == null)
                return TypeConstants.EmptyStringArray;
            
            var strings = arg is IEnumerable<string> ls
                ? ls
                : arg is string s
                    ? (IEnumerable<string>)new [] { s }
                    : arg is IEnumerable<object> e
                        ? e.Map(x => x.AsString())
                        : throw new NotSupportedException($"{filterName} expected a collection of strings but was '{arg.GetType().Name}'");

            return strings;
        }

        public static string ValidationSummary(ResponseStatus errorStatus, string exceptFor) =>
            ValidationSummary(errorStatus, ToVarNames(exceptFor), null); 
        public static string ValidationSummary(ResponseStatus errorStatus, ICollection<string> exceptFields, Dictionary<string,object> divAttrs)
        {
            var errorSummaryMsg = exceptFields != null
                ? ErrorResponseExcept(errorStatus, exceptFields)
                : ErrorResponseSummary(errorStatus);

            if (string.IsNullOrEmpty(errorSummaryMsg))
                return null;

            if (divAttrs == null)
                divAttrs = new Dictionary<string, object>();
            
            if (!divAttrs.ContainsKey("class") && !divAttrs.ContainsKey("className"))
                divAttrs["class"] = "alert alert-danger";
            
            return HtmlScripts.htmlDiv(errorSummaryMsg, divAttrs).ToRawString();
        }

        public static string ErrorResponseExcept(ResponseStatus errorStatus, string fieldNames) =>
            ErrorResponseExcept(errorStatus, ToVarNames(fieldNames));
        public static string ErrorResponseExcept(ResponseStatus errorStatus, ICollection<string> fieldNames)
        {
            if (errorStatus == null)
                return null;
            
            var fieldNamesLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fieldName in fieldNames)
            {
                fieldNamesLookup.Add(fieldName);
            }

            if (!fieldNames.IsEmpty() && !errorStatus.Errors.IsEmpty())
            {
                foreach (var fieldError in errorStatus.Errors)
                {
                    if (fieldNamesLookup.Contains(fieldError.FieldName))
                        return null;
                }

                var firstFieldError = errorStatus.Errors[0];
                return firstFieldError.Message ?? firstFieldError.ErrorCode;
            }

            return errorStatus.Message ?? errorStatus.ErrorCode;
        }
        
        public static string ErrorResponseSummary(ResponseStatus errorStatus)
        {
            if (errorStatus == null)
                return null;

            return errorStatus.Errors.IsEmpty()
                ? errorStatus.Message ?? errorStatus.ErrorCode
                : null;
        }
        
        public static string ErrorResponse(ResponseStatus errorStatus, string fieldName)
        {
            if (fieldName == null)
                return ErrorResponseSummary(errorStatus);
            if (errorStatus == null || errorStatus.Errors.IsEmpty())
                return null;

            foreach (var fieldError in errorStatus.Errors)
            {
                if (fieldName.EqualsIgnoreCase(fieldError.FieldName))
                    return fieldError.Message ?? fieldError.ErrorCode;
            }

            return null;
        }

        public static List<KeyValuePair<string, string>> ToKeyValues(object values)
        {
            var to = new List<KeyValuePair<string, string>>();
            if (values != null)
            {
                if (values is IEnumerable<KeyValuePair<string, object>> kvps)
                    foreach (var kvp in kvps) to.Add(new KeyValuePair<string,string>(kvp.Key, kvp.Value?.ToString()));
                else if (values is IEnumerable<KeyValuePair<string, string>> kvpsStr)
                    foreach (var kvp in kvpsStr) to.Add(new KeyValuePair<string,string>(kvp.Key, kvp.Value));
                else if (values is IEnumerable<object> list)
                    to.AddRange(from string item in list select item.AsString() into s select new KeyValuePair<string, string>(s, s));
            }
            return to;
        }
        public static List<string> ToStringList(IEnumerable target) => target is List<string> l ? l
            : target is string s 
            ? new List<string> { s } 
            : target is IEnumerable<string> e
            ? new List<string>(e)
            : target.Map(x => x.AsString());

        public static string FormControl(IRequest req, Dictionary<string,object> args, string tagName, InputOptions inputOptions)
        {
            if (tagName == null)
                tagName = "input";
            
            var options = inputOptions ?? new InputOptions();

            string id = null;
            string type = null;
            string name = null;
            string label = null;
            string size = options.Size;
            bool inline = options.Inline;

            if (args.TryGetValue("type", out var oType))
                type = oType as string;
            else
                args["type"] = type = "text";

            var notInput = tagName != "input";
            if (notInput)
            {
                type = tagName;
                args.RemoveKey("type");
            }

            var inputClass = "form-control";
            var labelClass = "form-label";
            var helpClass = "text-muted";
            var isCheck = type == "checkbox" || type == "radio";
            if (isCheck)
            {
                inputClass = "form-check-input";
                labelClass = "form-check-label";
                if (!args.ContainsKey("value"))
                    args["value"] = "true";
            }
            else if (type == "range")
            {
                inputClass = "form-control-range";
            }
            if (options.LabelClass != null)
                labelClass = options.LabelClass;

            if (args.TryGetValue("id", out var oId))
            {
                if (!args.ContainsKey("name"))
                    args["name"] = id = oId as string;

                if (args.TryGetValue("name", out var oName))
                    name = oName as string;
            }

            string help = options.Help;
            string helpId = help != null ? (id ?? name) + "-help" : null;
            if (helpId != null)
                args["aria-describedby"] = helpId;

            if (options.Label != null)
            {
                label = options.Label;
                if (!args.ContainsKey("placeholder"))
                    args["placeholder"] = label;
            }

            var values = options.Values;
            var isSingleCheck = isCheck && values == null;

            string formValue = null;
            var isGet = req.Verb == HttpMethods.Get;
            var preserveValue = options.PreserveValue;
            if (preserveValue)
            {
                formValue = FormValue(req, name);
                if (!isGet || !string.IsNullOrEmpty(formValue)) //only override value if POST or GET queryString has value
                {
                    if (!isCheck)
                        args["value"] = formValue;
                    else if (isSingleCheck)
                        args["checked"] = formValue == "true";
                }
            }
            else if (!isGet)
            {
                if (!isCheck)
                {
                    args["value"] = null;
                }
            }

            var className = args.TryGetValue("class", out var oCls) || args.TryGetValue("className", out oCls)
                ? HtmlScripts.htmlClassList(oCls)
                : "";
            
            className = HtmlScripts.htmlAddClass(className, inputClass);

            if (size != null)
                className = HtmlScripts.htmlAddClass(className, inputClass + "-" + size);

            var errorMsg = ErrorResponse(GetErrorStatus(req), name);
            if (errorMsg != null)
                className = HtmlScripts.htmlAddClass(className, "is-invalid");

            args["class"] = className;

            string inputHtml = null, labelHtml = null;
            var sb = StringBuilderCache.Allocate();

            if (label != null)
            {
                var labelArgs = new Dictionary<string, object>
                {
                    ["html"] = label,
                    ["class"] = labelClass,
                };
                if (id != null)
                    labelArgs["for"] = id;

                labelHtml = HtmlScripts.htmlLabel(labelArgs).AsString();
            }

            var value = (args.TryGetValue("value", out var oValue)
                    ? oValue as string
                    : null)
                ?? (oValue?.GetType().IsValueType == true
                    ? oValue.ToString()
                    : null);

            if (type == "radio")
            {
                if (values != null)
                {
                    var sbInput = StringBuilderCacheAlt.Allocate();
                    var kvps = ToKeyValues(values);
                    foreach (var kvp in kvps)
                    {
                        var cls = inline ? " custom-control-inline" : "";
                        sbInput.AppendLine($"<div class=\"custom-control custom-radio{cls}\">");
                        var inputId = name + "-" + kvp.Key;
                        var selected = kvp.Key == formValue || kvp.Key == value ? " checked" : "";
                        sbInput.AppendLine($"  <input type=\"radio\" id=\"{inputId}\" name=\"{name}\" value=\"{kvp.Key}\" class=\"custom-control-input\"{selected}>");
                        sbInput.AppendLine($"  <label class=\"custom-control-label\" for=\"{inputId}\">{kvp.Value}</label>");
                        sbInput.AppendLine("</div>");
                    }
                    inputHtml = StringBuilderCacheAlt.ReturnAndFree(sbInput);
                }
                else throw new NotSupportedException($"input type=radio requires 'values' inputOption containing a collection of Key/Value Pairs");
            }
            else if (type == "checkbox")
            {
                if (values != null)
                {
                    var sbInput = StringBuilderCacheAlt.Allocate();
                    var kvps = ToKeyValues(values);

                    var selectedValues = value != null && value != "true"
                        ? new HashSet<string> {value}
                        : oValue == null
                            ? TypeConstants<string>.EmptyHashSet
                            : (FormValues(req, name) ?? ToStringList(oValue as IEnumerable).ToArray())
                                  .ToHashSet();
                                
                    foreach (var kvp in kvps)
                    {
                        var cls = inline ? " custom-control-inline" : "";
                        sbInput.AppendLine($"<div class=\"custom-control custom-checkbox{cls}\">");
                        var inputId = name + "-" + kvp.Key;
                        var selected = kvp.Key == formValue || selectedValues.Contains(kvp.Key) ? " checked" : "";
                        sbInput.AppendLine($"  <input type=\"checkbox\" id=\"{inputId}\" name=\"{name}\" value=\"{kvp.Key}\" class=\"form-check-input\"{selected}>");
                        sbInput.AppendLine($"  <label class=\"form-check-label\" for=\"{inputId}\">{kvp.Value}</label>");
                        sbInput.AppendLine("</div>");
                    }
                    inputHtml = StringBuilderCacheAlt.ReturnAndFree(sbInput);
                }
            }
            else if (type == "select")
            {
                if (values != null)
                {
                    args["html"] = HtmlScripts.htmlOptions(values, 
                        new Dictionary<string, object> { {"selected",formValue ?? value} });
                }
                else if (!args.ContainsKey("html"))
                    throw new NotSupportedException($"<select> requires either 'values' inputOption containing a collection of Key/Value Pairs or 'html' argument containing innerHTML <option>'s");
            }

            if (inputHtml == null)
                inputHtml = HtmlScripts.htmlTag(args, tagName).AsString();

            if (isCheck)
            {
                sb.AppendLine(inputHtml);
                if (isSingleCheck) 
                    sb.AppendLine(labelHtml);
            }
            else
            {
                sb.AppendLine(labelHtml).AppendLine(inputHtml);
            }

            if (help != null)
            {
                sb.AppendLine($"<small id='{helpId}' class='{helpClass}'>{help.HtmlEncode()}</small>");
            }

            string htmlError = null;
            if (options.ShowErrors && errorMsg != null)
            {
                var errorClass = "invalid-feedback";
                if (options.ErrorClass != null)
                    errorClass = options.ErrorClass ?? "";
                htmlError = $"<div class='{errorClass}'>{errorMsg.HtmlEncode()}</div>";
            }

            if (!isCheck)
            {
                sb.AppendLine(htmlError);
            }
            else
            {
                var cls = htmlError != null ? " is-invalid form-control" : "";
                sb.Insert(0, $"<div class=\"form-check{cls}\">");
                sb.AppendLine("</div>");
                if (htmlError != null)
                    sb.AppendLine(htmlError);
            }

            if (isCheck && !isSingleCheck) // multi-value checkbox/radio
                sb.Insert(0, labelHtml);

            var html = StringBuilderCache.ReturnAndFree(sb);
            return html;
        }
        
        private static IVirtualFiles ResolveWriteVfs(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, string outFile, bool toDisk, out string useOutFile)
        {
            if (outFile.IndexOf(':') >= 0)
            {
                ResolveVfsAndSource(filterName, webVfs, contentVfs, outFile, out var useVfs, out useOutFile);
                return (IVirtualFiles)useVfs;
            }

            useOutFile = outFile;
            var vfs = !toDisk
                ? (IVirtualFiles)webVfs.GetMemoryVirtualFiles() ??
                    throw new NotSupportedException($"{nameof(MemoryVirtualFiles)} is required in {filterName} when disk=false")
                : webVfs.GetFileSystemVirtualFiles() ??
                    throw new NotSupportedException($"{nameof(FileSystemVirtualFiles)} is required in {filterName} when disk=true");
            return vfs;
        }

        private static void ResolveVfsAndSource(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, string source, out IVirtualPathProvider useVfs, out string useSource)
        {
            useVfs = webVfs;
            useSource = source;

            var parts = source.SplitOnFirst(':');
            if (parts.Length != 2)
                return;

            useSource = parts[1];
            var name = parts[0];
            useVfs = name == "content"
                ? contentVfs
                : name == "web"
                    ? webVfs
                    : name == "filesystem"
                        ? (IVirtualPathProvider) webVfs.GetFileSystemVirtualFiles()
                        : name == "memory"
                            ? webVfs.GetMemoryVirtualFiles()
                            : throw new NotSupportedException($"Unknown Virtual File System provider '{name}' used in '{filterName}'. Valid providers: web,content,filesystem,memory");
        }

        public static IEnumerable<IVirtualFile> GetBundleFiles(string filterName, IVirtualPathProvider webVfs, IVirtualPathProvider contentVfs, IEnumerable<string> virtualPaths, string assetExt)
        {
            foreach (var source in virtualPaths)
            {
                ResolveVfsAndSource(filterName, webVfs, contentVfs, source, out var vfs, out var virtualPath);
                
                var file = vfs.GetFile(virtualPath);
                if (file != null)
                {
                    yield return file;
                    continue;
                }

                var dir = vfs.GetDirectory(virtualPath);
                if (dir != null)
                {
                    var files = dir.GetFiles();
                    foreach (var dirFile in files)
                    {
                        if (!assetExt.EqualsIgnoreCase(dirFile.Extension))
                            continue;
                        
                        yield return dirFile;
                    }
                }
                else throw new NotSupportedException($"Could not find resource at virtual path '{source}' in '{filterName}'");
            }
        }

        public static string BundleJs(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor jsCompressor,
            BundleOptions options)
        {
            var assetExt = "js";
            var outFile = options.OutputTo ?? (options.Minify 
                  ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var htmlTagFmt = "<script src=\"{0}\"></script>";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs,
                jsCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt);
        }

        public static string BundleCss(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor cssCompressor,
            BundleOptions options)
        {
            var assetExt = "css";
            var outFile = options.OutputTo ?? (options.Minify 
                ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var htmlTagFmt = "<link rel=\"stylesheet\" href=\"{0}\">";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs, 
                cssCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt);
        }

        public static string BundleHtml(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor htmlCompressor,
            BundleOptions options)
        {
            var assetExt = "html";
            var outFile = options.OutputTo ?? (options.Minify 
                  ? $"/{assetExt}/bundle.min.{assetExt}" : $"/{assetExt}/bundle.{assetExt}");
            var id = options.OutputTo != null
                ? $" id=\"{options.OutputTo.LastRightPart('/').LeftPart('.')}\"" : "";
            var htmlTagFmt = "<link rel=\"import\" href=\"{0}\"" + id + ">";

            return BundleAsset(filterName, 
                webVfs, 
                contentVfs,
                htmlCompressor, options, outFile, options.OutputWebPath, htmlTagFmt, assetExt);
        }

        private static string BundleAsset(string filterName, 
            IVirtualPathProvider webVfs, 
            IVirtualPathProvider contentVfs, 
            ICompressor jsCompressor,
            BundleOptions options, 
            string origOutFile, 
            string outWebPath, 
            string htmlTagFmt, 
            string assetExt)
        {
            try
            {
                var writeVfs = ResolveWriteVfs(filterName, webVfs, contentVfs, origOutFile, options.SaveToDisk, out var outFilePath);

                var outHtmlTag = htmlTagFmt.Replace("{0}", outFilePath);

                var maxDate = DateTime.MinValue;
                var hasHash = outFilePath.IndexOf("[hash]", StringComparison.Ordinal) >= 0;

                if (!options.Sources.IsEmpty() && options.Bundle && options.Cache)
                {
                    if (hasHash)
                    {
                        var memFs = webVfs.GetMemoryVirtualFiles();

                        var existingBundleTag = webVfs.GetFile(outFilePath); 
                        if (existingBundleTag == null)
                        {
                            // use existing bundle if file with matching hash pattern is found
                            var fileSearchPath = outFilePath.Replace("[hash]", ".*");
                            var fileMatch = webVfs.GetAllMatchingFiles(fileSearchPath).FirstOrDefault();
                            if (fileMatch != null)
                            {
                                outHtmlTag = htmlTagFmt.Replace("{0}", "/" + fileMatch.VirtualPath);
                                memFs.WriteFile(outFilePath, outHtmlTag); //cache lookup
                                return outHtmlTag;
                            }
                        }
                        else
                        {
                            return existingBundleTag.ReadAllText();
                        }
                    }
                    else if (webVfs.FileExists(outWebPath ?? outFilePath))
                    {
                        return outHtmlTag;
                    }
                }

                var sources = GetBundleFiles(filterName, webVfs, contentVfs, options.Sources, assetExt);

                var existing = new HashSet<string>();
                var sb = StringBuilderCache.Allocate();
                var sbLog = StringBuilderCacheAlt.Allocate();

                void LogWarning(string msg)
                {
                    sbLog.AppendLine()
                        .Append(assetExt == "html" ? "<!--" : "/*")
                        .Append(" WARNING: ")
                        .Append(msg)
                        .Append(assetExt == "html" ? "-->" : "*/");
                }

                
                var minExt = ".min." + assetExt;
                if (options.Bundle)
                {
                    foreach (var file in sources)
                    {
                        if (hasHash)
                        {
                            file.Refresh();
                            if (file.LastModified > maxDate)
                                maxDate = file.LastModified;
                        }
                        
                        string src;
                        try
                        {
                            src = file.ReadAllText();
                        }
                        catch (Exception e)
                        {
                            LogWarning($"Could not read '{file.VirtualPath}': {e.Message}");
                            continue;
                        }
                        
                        if (file.Name.EndsWith("bundle." + assetExt) ||
                            file.Name.EndsWith("bundle.min." + assetExt) ||
                            existing.Contains(file.VirtualPath))
                            continue;

                        if (options.Minify && !file.Name.EndsWith(minExt))
                        {
                            string minified;
                            try
                            {
                                minified = jsCompressor.Compress(src);
                            }
                            catch (Exception e)
                            {
                                LogWarning($"Could not Compress '{file.VirtualPath}': {e.Message}");
                                minified = src;
                            }
                            
                            sb.Append(minified).Append(assetExt == "js" ? ";" : "").AppendLine();
                        }
                        else
                        {
                            sb.AppendLine(src);
                        }
    
                        // Also define ES6 module in AMD's define(), required by /js/ss-require.js
                        if (options.RegisterModuleInAmd && assetExt == "js")
                        {
                            sb.AppendLine("if (typeof define === 'function' && define.amd && typeof module !== 'undefined') define('" +
                                          file.Name.WithoutExtension() + "', [], function(){ return module.exports; });");
                        }

                        existing.Add(file.VirtualPath);
                    }

                    var bundled = StringBuilderCache.ReturnAndFree(sb);
                    if (hasHash)
                    {
                        var hash = "." + maxDate.ToUnixTimeMs();
                        outHtmlTag = outHtmlTag.Replace("[hash]", hash);
                        webVfs.GetMemoryVirtualFiles().WriteFile(outFilePath, outHtmlTag); //have bundle[hash].ext return rendered html
                        
                        outFilePath = outFilePath.Replace("[hash]", hash);
                    }
                    
                    try
                    {
                        writeVfs.WriteFile(outFilePath, bundled);
                    }
                    catch (Exception e)
                    {
                        LogWarning($"Could not write to '{origOutFile}': {e.Message}");
                    }

                    if (sbLog.Length != 0) 
                        return outHtmlTag + StringBuilderCacheAlt.ReturnAndFree(sbLog);
                    
                    StringBuilderCacheAlt.Free(sbLog);
                    return outHtmlTag;
                }
                else
                {
                    var filePaths = new List<string>();
                    
                    foreach (var file in sources)
                    {
                        if (file.Name.EndsWith("bundle." + assetExt) ||
                            file.Name.EndsWith("bundle.min." + assetExt) ||
                            existing.Contains(file.VirtualPath))
                            continue;
                        
                        filePaths.Add("/".CombineWith(file.VirtualPath));
                        existing.Add(file.VirtualPath);
                    }

                    foreach (var filePath in filePaths)
                    {
                        if (filePath.EndsWith(minExt))
                        {
                            var withoutMin = filePath.Substring(0, filePath.Length - minExt.Length) + "." + assetExt;
                            if (filePaths.Contains(withoutMin))
                                continue;
                        }

                        sb.AppendLine(htmlTagFmt.Replace("{0}", filePath));
                    }
                    
                    return StringBuilderCache.ReturnAndFree(sb);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
    
}