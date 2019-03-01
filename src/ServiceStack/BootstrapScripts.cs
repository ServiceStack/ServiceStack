using System.Collections.Generic;
using ServiceStack.Script;

namespace ServiceStack
{
    // ReSharper disable InconsistentNaming
    
    public class BootstrapScripts : ScriptMethods
    {
        public IRawString validationSummary(ScriptScopeContext scope) =>
            validationSummary(scope, null, null);

        public IRawString validationSummary(ScriptScopeContext scope, IEnumerable<object> exceptFields) =>
            validationSummary(scope, exceptFields, null);
        
        public IRawString validationSummary(ScriptScopeContext scope, IEnumerable<object> exceptFields, object htmlAttrs)
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
            
            return Context.HtmlMethods.htmlDiv(errorSummaryMsg, divAttrs);
        }

        public IRawString formTextarea(ScriptScopeContext scope, object args) => formTextarea(scope, args, null);
        public IRawString formTextarea(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "textarea", inputOptions);
        
        public IRawString formSelect(ScriptScopeContext scope, object args) => formSelect(scope, args, null);
        public IRawString formSelect(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "select", inputOptions);
        
        public IRawString formInput(ScriptScopeContext scope, object args) => formInput(scope, args, null);

        public IRawString formInput(ScriptScopeContext scope, object inputAttrs, object inputOptions) =>
            formControl(scope, inputAttrs, "input", inputOptions);

        public IRawString formControl(ScriptScopeContext scope, object inputAttrs, string tagName, object inputOptions) => 
            ViewUtils.FormControl(Context.GetServiceStackFilters().req(scope), inputAttrs.AssertOptions(nameof(formControl)), tagName, 
                (inputOptions as IEnumerable<KeyValuePair<string, object>>).FromObjectDictionary<InputOptions>()).ToRawString();
    }
}