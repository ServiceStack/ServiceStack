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

            if (options.TryGetValue("label", out var oLabel) && !args.ContainsKey("placeholder"))
                args["placeholder"] = label = oLabel as string;

            var isGet = Context.DefaultFilters.isHttpGet(scope);
            var preserveValue = !options.TryGetValue("preserveValue", out var oPreserve) || oPreserve as bool? == true;
            if (preserveValue)
            {
                var value = Context.DefaultFilters.httpForm(scope, name);
                if (!isGet || !string.IsNullOrEmpty(value)) //only override value if POST or GET queryString has value
                {
                    if (!isCheck)
                        args["value"] = value;
                    else
                        args["checked"] = value == "true";
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

                var labelHtml = htmlFilters.htmlLabel(labelArgs).AsString();
                sb.AppendLine(labelHtml);
            }

            var inputHtml = htmlFilters.htmlTag(args, tagName).AsString();
            if (!isCheck)
                sb.AppendLine(inputHtml);
            else
                sb.Insert(0, inputHtml).AppendLine();

            if (help != null)
            {
                sb.AppendLine($"<small id='{helpId}' class='{helpClass}'>{help.HtmlEncode()}</small>");
            }
            
            var showErrors = !options.TryGetValue("showErrors", out var oShowErrors) || oShowErrors as bool? == true;
            if (showErrors && errorMsg != null)
            {
                var errorClass = "invalid-feedback";
                if (options.TryGetValue("errorClass", out var oErrorClass))
                    errorClass = oErrorClass as string ?? "";
                var htmlError = $"<div class='{errorClass}'>{errorMsg.HtmlEncode()}</div>";
                sb.AppendLine(htmlError);
            }

            var html = StringBuilderCache.ReturnAndFree(sb);

            if (isCheck)
            {
                html = "<div class=\"form-check\">" + html + "</div>";
            }

            return html.ToRawString();
        }
    }
}