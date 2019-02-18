using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Templates;
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
    /// Shared Utils shared between different Template Filters and Razor Views/Helpers
    /// </summary>
    public static class ViewUtils
    {
        private static readonly TemplateHtmlFilters HtmlFilters = new TemplateHtmlFilters();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(object test) => test == null || test == JsNull.Value;
        
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
            
            return HtmlFilters.htmlDiv(errorSummaryMsg, divAttrs).ToRawString();
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
        public static List<string> ToStringList(IEnumerable target) => target is string s 
            ? new List<string> { s } 
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
                ? HtmlFilters.htmlClassList(oCls)
                : "";
            
            className = HtmlFilters.htmlAddClass(className, inputClass);

            if (size != null)
                className = HtmlFilters.htmlAddClass(className, inputClass + "-" + size);

            var errorMsg = ErrorResponse(GetErrorStatus(req), name);
            if (errorMsg != null)
                className = HtmlFilters.htmlAddClass(className, "is-invalid");

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

                labelHtml = HtmlFilters.htmlLabel(labelArgs).AsString();
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
                    args["html"] = HtmlFilters.htmlOptions(values, 
                        new Dictionary<string, object> { {"selected",formValue ?? value} });
                }
                else if (!args.ContainsKey("html"))
                    throw new NotSupportedException($"<select> requires either 'values' inputOption containing a collection of Key/Value Pairs or 'html' argument containing innerHTML <option>'s");
            }

            if (inputHtml == null)
                inputHtml = HtmlFilters.htmlTag(args, tagName).AsString();

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
     }
}