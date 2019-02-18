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

        public IRawString formControl(TemplateScopeContext scope, object inputAttrs, string tagName, object inputOptions) => 
            ViewUtils.FormControl(Context.GetServiceStackFilters().req(scope), inputAttrs.AssertOptions(nameof(formControl)), tagName, 
                (inputOptions as IEnumerable<KeyValuePair<string, object>>).FromObjectDictionary<InputOptions>()).ToRawString();
    }
}