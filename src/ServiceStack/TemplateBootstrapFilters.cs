using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public class TemplateBootstrapFilters : TemplateFilter
    {
        public IRawString validationSummary(TemplateScopeContext scope) =>
            validationSummary(scope, null, null);

        public IRawString validationSummary(TemplateScopeContext scope, IEnumerable<object> exceptFields) =>
            validationSummary(scope, exceptFields, null);
        
        public IRawString validationSummary(TemplateScopeContext scope, IEnumerable<object> exceptFields, object htmlAttrs)
        {
            var ssFilters = Context.GetServiceStackFilters();
            if (ssFilters == null)
                return null;

            var errorSummaryMsg = exceptFields != null
                ? ssFilters.errorResponseExcept(scope, exceptFields)
                : ssFilters.errorResponseSummary(scope);

            if (string.IsNullOrEmpty(errorSummaryMsg))
                return null;

            var divAttrs = htmlAttrs.AssertOptions(nameof(validationSummary));
            if (!divAttrs.ContainsKey("class") && !divAttrs.ContainsKey("className"))
                divAttrs["class"] = "alert alert-danger";
            
            return Context.HtmlFilters.htmlDiv(errorSummaryMsg, divAttrs);
        }

        public IRawString formTextarea(TemplateScopeContext scope, object args) => formTextarea(scope, args, null);
        public IRawString formTextarea(TemplateScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "textarea", inputOptions);
        
        public IRawString formSelect(TemplateScopeContext scope, object args) => formSelect(scope, args, null);
        public IRawString formSelect(TemplateScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "select", inputOptions);
        
        public IRawString formInput(TemplateScopeContext scope, object args) => formInput(scope, args, null);

        public IRawString formInput(TemplateScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "input", inputOptions);

        public IRawString formControl(TemplateScopeContext scope, object inputAttrs, string tagName, object inputOptions)
        {
            if (tagName == null)
                tagName = "input";
            
            var args = inputAttrs.AssertOptions(nameof(formInput));
            var options = inputOptions as Dictionary<string, object> ?? TypeConstants.EmptyObjectDictionary;

            string id = null;
            string type = null;
            string name = null;
            string label = null;
            string size = null;
            bool inline = options.TryGetValue("inline", out var oInline) && oInline is bool b;

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
            if (options.TryGetValue("labelClass", out var oLabelClass))
                labelClass = oLabelClass as string ?? "";

            if (args.TryGetValue("id", out var oId))
            {
                if (!args.ContainsKey("name"))
                    args["name"] = id = oId as string;

                if (args.TryGetValue("name", out var oName))
                    name = oName as string;
            }

            string help = options.TryGetValue("help", out var oHelp) ? oHelp as string : null;
            string helpId = help != null ? (id ?? name) + "-help" : null;
            if (helpId != null)
                args["aria-describedby"] = helpId;

            if (options.TryGetValue("label", out var oLabel))
            {
                label = oLabel as string;
                if (!args.ContainsKey("placeholder"))
                    args["placeholder"] = label;
            }

            var values = options.TryGetValue("values", out var oValues)
                ? oValues
                : null;
            var isSingleCheck = isCheck && values == null;

            string formValue = null;
            var defaultFilters = Context.DefaultFilters;
            var isGet = defaultFilters.isHttpGet(scope);
            var preserveValue = !options.TryGetValue("preserveValue", out var oPreserve) || oPreserve as bool? == true;
            if (preserveValue)
            {
                formValue = Context.GetServiceStackFilters().formValue(scope, name);
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

            var htmlFilters = scope.Context.HtmlFilters;
            var className = args.TryGetValue("class", out var oCls) || args.TryGetValue("className", out oCls)
                ? htmlFilters.htmlClassList(oCls)
                : "";
            
            className = htmlFilters.htmlAddClass(className, inputClass);

            if (options.TryGetValue("size", out var oSize))
                className = htmlFilters.htmlAddClass(className, inputClass + "-" + (size = oSize as string));

            var errorMsg = Context.GetServiceStackFilters()?.errorResponse(scope, name);
            if (errorMsg != null)
                className = htmlFilters.htmlAddClass(className, "is-invalid");

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

                labelHtml = htmlFilters.htmlLabel(labelArgs).AsString();
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
                    var kvps = defaultFilters.toKeyValues(values);
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
                    var kvps = defaultFilters.toKeyValues(values);

                    var selectedValues = value != null && value != "true"
                        ? new HashSet<string> {value}
                        : oValue == null
                            ? TypeConstants<string>.EmptyHashSet
                            : (Context.GetServiceStackFilters().formValues(scope, name) ?? defaultFilters.toStringList(oValue as IEnumerable).ToArray())
                                  .ToHashSet();
                                
                    foreach (var kvp in kvps)
                    {
                        var cls = inline ? " custom-control-inline" : "";
                        sbInput.AppendLine($"<div class=\"custom-control custom-checkbox{cls}\">");
                        var inputId = name + "-" + kvp.Key;
                        var selected = selectedValues.Contains(formValue) || selectedValues.Contains(kvp.Key) ? " checked" : "";
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
                    args["html"] = Context.HtmlFilters.htmlOptions(values, 
                        new Dictionary<string, object> { {"selected",formValue ?? value} });
                }
                else if (!args.ContainsKey("html"))
                    throw new NotSupportedException($"<select> requires either 'values' inputOption containing a collection of Key/Value Pairs or 'html' argument containing innerHTML <option>'s");
            }

            if (inputHtml == null)
                inputHtml = htmlFilters.htmlTag(args, tagName).AsString();

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
            var showErrors = !options.TryGetValue("showErrors", out var oShowErrors) || oShowErrors as bool? == true;
            if (showErrors && errorMsg != null)
            {
                var errorClass = "invalid-feedback";
                if (options.TryGetValue("errorClass", out var oErrorClass))
                    errorClass = oErrorClass as string ?? "";
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
            return html.ToRawString();
        }
    }
}