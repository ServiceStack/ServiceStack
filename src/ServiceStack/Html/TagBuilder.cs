using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using ServiceStack.Markdown;

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
	}

	public enum TagRenderMode
	{
		Normal,
		StartTag,
		EndTag,
		SelfClosing
	}

	public class MvcHtmlString
	{
		private delegate MvcHtmlString MvcHtmlStringCreator(string value);
		private static readonly MvcHtmlStringCreator _creator = GetCreator();

		// imporant: this declaration must occur after the _creator declaration
		public static readonly MvcHtmlString Empty = Create(String.Empty);

		private readonly string _value;

		protected MvcHtmlString(string value)
		{
			_value = value ?? String.Empty;
		}

		public static MvcHtmlString Create(string value)
		{
			return _creator(value);
		}

		// in .NET 4, we dynamically create a type that subclasses MvcHtmlString and implements IHtmlString
		private static MvcHtmlStringCreator GetCreator()
		{
			var iHtmlStringType = typeof(HttpContext).Assembly.GetType("System.Web.IHtmlString");
			if (iHtmlStringType != null)
			{
				// first, create the dynamic type
				var dynamicType = DynamicTypeGenerator.GenerateType("DynamicMvcHtmlString", typeof(MvcHtmlString), new[] { iHtmlStringType });

				// then, create the delegate to instantiate the dynamic type
				var valueParamExpr = Expression.Parameter(typeof(string), "value");
				var newObjExpr = Expression.New(dynamicType.GetConstructor(new[] { typeof(string) }), valueParamExpr);
				var lambdaExpr = Expression.Lambda<MvcHtmlStringCreator>(newObjExpr, valueParamExpr);
				return lambdaExpr.Compile();
			}
			else
			{
				// disabling 0618 allows us to call the MvcHtmlString() constructor
#pragma warning disable 0618
				return value => new MvcHtmlString(value);
#pragma warning restore 0618
			}
		}

		public static bool IsNullOrEmpty(MvcHtmlString value)
		{
			return (value == null || value._value.Length == 0);
		}

		// IHtmlString.ToHtmlString()
		public string ToHtmlString()
		{
			return _value;
		}

		public override string ToString()
		{
			return _value;
		}
	}

	public class TagBuilder
	{
		private const string IdAttributeDotReplacement = "_";

		private const string AttributeFormat = @" {0}=""{1}""";
		private const string ElementFormatEndTag = "</{0}>";
		private const string ElementFormatNormal = "<{0}{1}>{2}</{0}>";
		private const string ElementFormatSelfClosing = "<{0}{1} />";
		private const string ElementFormatStartTag = "<{0}{1}>";

		private string innerHtml;

		public TagBuilder(string tagName)
		{
			if (String.IsNullOrEmpty(tagName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "tagName");
			}

			TagName = tagName;
			Attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
		}

		public IDictionary<string, string> Attributes { get; private set; }

		public string InnerHtml
		{
			get
			{
				return innerHtml ?? String.Empty;
			}
			set
			{
				innerHtml = value;
			}
		}

		public string TagName { get; private set; }

		public void AddCssClass(string value)
		{
			string currentValue;

			if (Attributes.TryGetValue("class", out currentValue))
			{
				Attributes["class"] = value + " " + currentValue;
			}
			else
			{
				Attributes["class"] = value;
			}
		}

		public static string CreateSanitizedId(string originalId)
		{
			return CreateSanitizedId(originalId, TagBuilder.IdAttributeDotReplacement);
		}

		internal static string CreateSanitizedId(string originalId, string invalidCharReplacement)
		{
			if (String.IsNullOrEmpty(originalId))
			{
				return null;
			}

			if (invalidCharReplacement == null) {
				throw new ArgumentNullException("invalidCharReplacement");
			}

			char firstChar = originalId[0];
			if (!Html401IdUtil.IsLetter(firstChar))
			{
				// the first character must be a letter
				return null;
			}

			var sb = new StringBuilder(originalId.Length);
			sb.Append(firstChar);

			for (int i = 1; i < originalId.Length; i++)
			{
				char thisChar = originalId[i];
				if (Html401IdUtil.IsValidIdCharacter(thisChar))
				{
					sb.Append(thisChar);
				}
				else
				{
					sb.Append(invalidCharReplacement);
				}
			}

			return sb.ToString();
		}

		public void GenerateId(string name)
		{
			if (!Attributes.ContainsKey("id")) {
				string sanitizedId = CreateSanitizedId(name, IdAttributeDotReplacement);
				if (!String.IsNullOrEmpty(sanitizedId)) {
					Attributes["id"] = sanitizedId;
				}
			}
		}
		
		private string GetAttributesString()
		{
			var sb = new StringBuilder();
			foreach (var attribute in Attributes)
			{
				string key = attribute.Key;
				if (String.Equals(key, "id", StringComparison.Ordinal /* case-sensitive */) && String.IsNullOrEmpty(attribute.Value))
				{
					continue; // DevDiv Bugs #227595: don't output empty IDs
				}
				string value = HttpUtility.HtmlAttributeEncode(attribute.Value);
				sb.AppendFormat(CultureInfo.InvariantCulture, AttributeFormat, key, value);
			}
			return sb.ToString();
		}

		public void MergeAttribute(string key, string value)
		{
			MergeAttribute(key, value, false /* replaceExisting */);
		}

		public void MergeAttribute(string key, string value, bool replaceExisting)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "key");
			}

			if (replaceExisting || !Attributes.ContainsKey(key))
			{
				Attributes[key] = value;
			}
		}

		public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes)
		{
			MergeAttributes(attributes, false /* replaceExisting */);
		}

		public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting)
		{
			if (attributes != null)
			{
				foreach (var entry in attributes)
				{
					string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
					string value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);
					MergeAttribute(key, value, replaceExisting);
				}
			}
		}

		public void SetInnerText(string innerText)
		{
			InnerHtml = HttpUtility.HtmlEncode(innerText);
		}

		internal MvcHtmlString ToMvcHtmlString(TagRenderMode renderMode)
		{
			return MvcHtmlString.Create(ToString(renderMode));
		}

		public override string ToString()
		{
			return ToString(TagRenderMode.Normal);
		}

		public string ToString(TagRenderMode renderMode)
		{
			switch (renderMode)
			{
				case TagRenderMode.StartTag:
					return String.Format(CultureInfo.InvariantCulture, ElementFormatStartTag, TagName, GetAttributesString());
				case TagRenderMode.EndTag:
					return String.Format(CultureInfo.InvariantCulture, ElementFormatEndTag, TagName);
				case TagRenderMode.SelfClosing:
					return String.Format(CultureInfo.InvariantCulture, ElementFormatSelfClosing, TagName, GetAttributesString());
				default:
					return String.Format(CultureInfo.InvariantCulture, ElementFormatNormal, TagName, GetAttributesString(), InnerHtml);
			}
		}

		// Valid IDs are defined in http://www.w3.org/TR/html401/types.html#type-id
		private static class Html401IdUtil
		{
			private static bool IsAllowableSpecialCharacter(char c)
			{
				switch (c)
				{
					case '-':
					case '_':
					case ':':
						// note that we're specifically excluding the '.' character
						return true;

					default:
						return false;
				}
			}

			private static bool IsDigit(char c)
			{
				return ('0' <= c && c <= '9');
			}

			public static bool IsLetter(char c)
			{
				return (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z'));
			}

			public static bool IsValidIdCharacter(char c)
			{
				return (IsLetter(c) || IsDigit(c) || IsAllowableSpecialCharacter(c));
			}
		}

	}
}