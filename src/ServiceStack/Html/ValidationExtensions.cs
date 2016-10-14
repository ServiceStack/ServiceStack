#if !NETSTANDARD1_6

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;

namespace ServiceStack.Html
{
    public static class ValidationExtensions
    {
        private const string HiddenListItem = @"<li style=""display:none""></li>";
        private static string _resourceClassKey;

        public static string ResourceClassKey
        {
            get { return _resourceClassKey ?? String.Empty; }
            set { _resourceClassKey = value; }
        }

        private static FieldValidationMetadata ApplyFieldValidationMetadata(HtmlHelper htmlHelper, ModelMetadata modelMetadata, string modelName)
        {
            FormContext formContext = htmlHelper.ViewContext.FormContext;
            FieldValidationMetadata fieldMetadata = formContext.GetValidationMetadataForField(modelName, true /* createIfNotFound */);

            // write rules to context object
            //IEnumerable<ModelValidator> validators = ModelValidatorProviders.Providers.GetValidators(modelMetadata, htmlHelper.ViewContext);
            //foreach (ModelClientValidationRule rule in validators.SelectMany(v => v.GetClientValidationRules()))
            //{
            //    fieldMetadata.ValidationRules.Add(rule);
            //}

            return fieldMetadata;
        }
        private static string GetInvalidPropertyValueResource(HttpContextBase httpContext)
        {
            string resourceValue = null;
            if (!String.IsNullOrEmpty(ResourceClassKey) && (httpContext != null))
            {
                // If the user specified a ResourceClassKey try to load the resource they specified.
                // If the class key is invalid, an exception will be thrown.
                // If the class key is valid but the resource is not found, it returns null, in which
                // case it will fall back to the MVC default error message.
                resourceValue = httpContext.GetGlobalResourceObject(ResourceClassKey, "InvalidPropertyValue", CultureInfo.CurrentUICulture) as string;
            }
            return resourceValue ?? MvcResources.Common_ValueNotValidForProperty;
        }

        private static string GetUserErrorMessageOrDefault(HttpContextBase httpContext, ModelError error, ModelState modelState)
        {
            if (!String.IsNullOrEmpty(error.ErrorMessage))
            {
                return error.ErrorMessage;
            }
            if (modelState == null)
            {
                return null;
            }

            string attemptedValue = (modelState.Value != null) ? modelState.Value.AttemptedValue : null;
            return String.Format(CultureInfo.CurrentCulture, GetInvalidPropertyValueResource(httpContext), attemptedValue);
        }

        // Validate
        public static void Validate(this HtmlHelper htmlHelper, string modelName)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException("modelName");
            }

            ValidateHelper(htmlHelper,
                           ModelMetadata.FromStringExpression(modelName, htmlHelper.ViewContext.ViewData),
                           modelName);
        }

        public static void ValidateFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            ValidateHelper(htmlHelper,
                           ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData),
                           ExpressionHelper.GetExpressionText(expression));
        }

        private static void ValidateHelper(HtmlHelper htmlHelper, ModelMetadata modelMetadata, string expression)
        {
            FormContext formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();
            if (formContext == null || htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled)
            {
                return; // nothing to do
            }

            string modelName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
            ApplyFieldValidationMetadata(htmlHelper, modelMetadata, modelName);
        }

        // ValidationMessage

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName)
        {
            return ValidationMessage(htmlHelper, modelName, null /* validationMessage */, new RouteValueDictionary());
        }

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName, object htmlAttributes)
        {
            return ValidationMessage(htmlHelper, modelName, null /* validationMessage */, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName, string validationMessage)
        {
            return ValidationMessage(htmlHelper, modelName, validationMessage, new RouteValueDictionary());
        }

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName, string validationMessage, object htmlAttributes)
        {
            return ValidationMessage(htmlHelper, modelName, validationMessage, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName, IDictionary<string, object> htmlAttributes)
        {
            return ValidationMessage(htmlHelper, modelName, null /* validationMessage */, htmlAttributes);
        }

        public static MvcHtmlString ValidationMessage(this HtmlHelper htmlHelper, string modelName, string validationMessage, IDictionary<string, object> htmlAttributes)
        {
            if (modelName == null)
            {
                throw new ArgumentNullException("modelName");
            }

            return ValidationMessageHelper(htmlHelper,
                                           ModelMetadata.FromStringExpression(modelName, htmlHelper.ViewContext.ViewData),
                                           modelName,
                                           validationMessage,
                                           htmlAttributes);
        }

        public static MvcHtmlString ValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            return ValidationMessageFor(htmlHelper, expression, null /* validationMessage */, new RouteValueDictionary());
        }

        public static MvcHtmlString ValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage)
        {
            return ValidationMessageFor(htmlHelper, expression, validationMessage, new RouteValueDictionary());
        }

        public static MvcHtmlString ValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage, object htmlAttributes)
        {
            return ValidationMessageFor(htmlHelper, expression, validationMessage, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage, IDictionary<string, object> htmlAttributes)
        {
            return ValidationMessageHelper(htmlHelper,
                                           ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData),
                                           ExpressionHelper.GetExpressionText(expression),
                                           validationMessage,
                                           htmlAttributes);
        }

        private static MvcHtmlString ValidationMessageHelper(this HtmlHelper htmlHelper, ModelMetadata modelMetadata, string expression, string validationMessage, IDictionary<string, object> htmlAttributes)
        {
            string modelName = expression;

            var fieldError = htmlHelper.GetFieldError(modelName);
            if (fieldError == null)
                return null;

            var spanTag = new TagBuilder("span");
            spanTag.MergeAttributes(htmlAttributes);
            HtmlHelper.ValidationMessageCssClassNames.Split(' ').Each(spanTag.AddCssClass);
            spanTag.AddCssClass(fieldError.ErrorCode);
            spanTag.InnerHtml = fieldError.Message;
            return spanTag.ToMvcHtmlString(TagRenderMode.Normal);
        }

        // ValidationSummary

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper)
        {
            return ValidationSummary(htmlHelper, false /* excludePropertyErrors */);
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, bool excludePropertyErrors)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, null /* message */);
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, string message)
        {
            return ValidationSummary(htmlHelper, false /* excludePropertyErrors */, message, (object)null /* htmlAttributes */);
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, bool excludePropertyErrors, string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message, (object)null /* htmlAttributes */);
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, string message, object htmlAttributes)
        {
            return ValidationSummary(htmlHelper, false /* excludePropertyErrors */, message, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, bool excludePropertyErrors, string message, object htmlAttributes)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, string message, IDictionary<string, object> htmlAttributes)
        {
            return ValidationSummary(htmlHelper, false /* excludePropertyErrors */, message, htmlAttributes);
        }

        public static MvcHtmlString ValidationSummary(this HtmlHelper htmlHelper, bool excludePropertyErrors, string message, IDictionary<string, object> htmlAttributes)
        {
            if (htmlHelper == null)
                throw new ArgumentNullException("htmlHelper");

            var errorStatus = htmlHelper.GetErrorStatus();
            if (errorStatus == null || errorStatus.Errors != null && errorStatus.Errors.Count > 0)
                return null; //just show individual field errors

            var divTag = new TagBuilder("div");
            HtmlHelper.ValidationSummaryCssClassNames.Split(' ').Each(divTag.AddCssClass);
            divTag.MergeAttributes(htmlAttributes);
            divTag.InnerHtml = errorStatus.Message;
            return divTag.ToMvcHtmlString(TagRenderMode.Normal);
        }

        public static MvcHtmlString ValidationSuccess(this HtmlHelper htmlHelper, string message, object htmlAttributes=null)
        {
            return ValidationSuccess(htmlHelper, message, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ValidationSuccess(this HtmlHelper htmlHelper, string message, IDictionary<string, object> htmlAttributes)
        {
            if (htmlHelper == null)
                throw new ArgumentNullException("htmlHelper");

            var errorStatus = htmlHelper.GetErrorStatus();
            if (message == null 
                || errorStatus != null
                || htmlHelper.HttpRequest.HttpMethod == HttpMethods.Get)
                return null; 

            var divTag = new TagBuilder("div");
            HtmlHelper.ValidationSuccessCssClassNames.Split(' ').Each(divTag.AddCssClass);
            divTag.MergeAttributes(htmlAttributes);
            divTag.InnerHtml = message;
            return divTag.ToMvcHtmlString(TagRenderMode.Normal);
        }
    }
}

#endif
