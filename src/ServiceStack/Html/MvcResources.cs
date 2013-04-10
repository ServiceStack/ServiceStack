
namespace ServiceStack.Html
{
	public static class MvcResources
	{
		public const string ViewMasterPage_RequiresViewPage = "View MasterPage Requires ViewPage";
		public const string MvcRazorCodeParser_CannotHaveModelAndInheritsKeyword = "The 'inherits' keyword is not allowed when a '{0}' keyword is used.";
		public const string MvcRazorCodeParser_OnlyOneModelStatementIsAllowed = "Only one '{0}' statement is allowed in a file.";
		public const string MvcRazorCodeParser_ModelKeywordMustBeFollowedByTypeName = "The '{0}' keyword must be followed by a type name on the same line.";

		public const string HtmlHelper_TextAreaParameterOutOfRange = "TextArea Parameter Out Of Range";
		public const string ValueProviderResult_ConversionThrew = "Conversion Threw";
		public const string ValueProviderResult_NoConverterExists = "No Converter Exists";
		public const string ViewDataDictionary_WrongTModelType = "Wrong Model Type";
		public const string ViewDataDictionary_ModelCannotBeNull = "Model Cannot Be Null";
		public const string Common_PropertyNotFound = "Property Not Found";
		public const string Common_NullOrEmpty = "Required field";
		public const string HtmlHelper_InvalidHttpVerb = "Invalid HTTP Verb";
		public const string HtmlHelper_InvalidHttpMethod = "Invalid HTTP Method";
		public const string TemplateHelpers_TemplateLimitations = "Unsupported Template Limitations";
		public const string ExpressionHelper_InvalidIndexerExpression = "Invalid Indexer Expression";
        
        public const string AntiForgeryToken_AdditionalDataCheckFailed = "The provided anti-forgery token failed a custom data check.";
        public const string AntiForgeryToken_ClaimUidMismatch = "The provided anti-forgery token was meant for a different claims-based user than the current user.";
        public const string AntiForgeryToken_CookieMissing = "The required anti-forgery cookie &quot;{0}&quot; is not present.";
        public const string AntiForgeryToken_DeserializationFailed = "The anti-forgery token could not be decrypted. If this application is hosted by a Web Farm or cluster, ensure that all machines are running the same version of ASP.NET Web Pages and that the &lt;machineKey&gt; configuration specifies explicit encryption and validation keys. AutoGenerate cannot be used in a cluster.";
        public const string AntiForgeryToken_FormFieldMissing = "The required anti-forgery form field &quot;{0}&quot; is not present.";
        public const string AntiForgeryToken_SecurityTokenMismatch = "The anti-forgery cookie token and form field token do not match.";
        public const string AntiForgeryToken_TokensSwapped = "Validation of the provided anti-forgery token failed. The cookie &quot;{0}&quot; and the form field &quot;{1}&quot; were swapped.";
        public const string AntiForgeryToken_UsernameMismatch = "The provided anti-forgery token was meant for user &quot;{0}&quot;, but the current user is &quot;{1}&quot;.";
        public static string AntiForgeryWorker_RequireSSL = "The anti-forgery system has the configuration value AntiForgeryConfig.RequireSsl = true, but the current request is not an SSL request.";
        public static string ClaimUidExtractor_DefaultClaimsNotPresent = "A claim of type 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier' or 'http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider' was not present on the provided ClaimsIdentity. To enable anti-forgery token support with claims-based authentication, please verify that the configured claims provider is providing both of these claims on the ClaimsIdentity instances it generates. If the configured claims provider instead uses a different claim type as a unique identifier, it can be configured by setting the static property AntiForgeryConfig.UniqueClaimTypeIdentifier.";
        public static string ClaimUidExtractor_ClaimNotPresent = "A claim of type '{0}' was not present on the provided ClaimsIdentity.";
        public static string HttpContextUnavailable = "An HttpContext is required to perform this operation. Check that this operation is being performed during a web request.";
        public static string TokenValidator_AuthenticatedUserWithoutUsername = "The provided identity of type '{0}' is marked IsAuthenticated = true but does not have a value for Name. By default, the anti-forgery system requires that all authenticated identities have a unique Name. If it is not possible to provide a unique Name for this identity, consider setting the static property AntiForgeryConfig.AdditionalDataProvider to an instance of a type that can provide some form of unique identifier for the current user.";

        public static string UnobtrusiveJavascript_ValidationTypeCannotBeEmpty = "Validation type names in unobtrusive client validation rules cannot be empty. Client rule type: {0}";
        public static string UnobtrusiveJavascript_ValidationTypeMustBeUnique = "Validation type names in unobtrusive client validation rules must be unique. The following validation type was seen more than once: {0}";
        public static string UnobtrusiveJavascript_ValidationTypeMustBeLegal = "Validation type names in unobtrusive client validation rules must consist of only lowercase letters. Invalid name: &quot;{0}&quot;, client rule type: {1}";
        public static string UnobtrusiveJavascript_ValidationParameterCannotBeEmpty = "Validation parameter names in unobtrusive client validation rules cannot be empty. Client rule type: {0}";
        public static string UnobtrusiveJavascript_ValidationParameterMustBeLegal = "Validation parameter names in unobtrusive client validation rules must start with a lowercase letter and consist of only lowercase letters or digits. Validation parameter name: {0}, client rule type: {1}"; 
    }
}
