using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;

namespace ServiceStack.Html
{
	public class TagBuilder
	{
		public const string IdAttributeDotReplacement = "_";

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

        private void AppendAttributes(StringBuilder sb)
        {
            foreach (var attribute in Attributes) {
                string key = attribute.Key;
                if (String.Equals(key, "id", StringComparison.Ordinal /* case-sensitive */) && String.IsNullOrEmpty(attribute.Value)) {
                    continue; // DevDiv Bugs #227595: don't output empty IDs
                }
                string value = HttpUtility.HtmlAttributeEncode(attribute.Value);
                sb.Append(' ')
                    .Append(key)
                    .Append("=\"")
                    .Append(value)
                    .Append('"');
            }
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

		internal MvcHtmlString ToHtmlString(TagRenderMode renderMode)
		{
			return MvcHtmlString.Create(ToString(renderMode));
		}

		public override string ToString()
		{
			return ToString(TagRenderMode.Normal);
		}

		public string ToString(TagRenderMode renderMode)
		{
            StringBuilder sb = new StringBuilder();
            switch (renderMode) {
                case TagRenderMode.StartTag:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>');
                    break;
                case TagRenderMode.EndTag:
                    sb.Append("</")
                        .Append(TagName)
                        .Append('>');
                    break;
                case TagRenderMode.SelfClosing:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append(" />");
                    break;
                default:
                    sb.Append('<')
                        .Append(TagName);
                    AppendAttributes(sb);
                    sb.Append('>')
                        .Append(InnerHtml)
                        .Append("</")
                        .Append(TagName)
                        .Append('>');
                    break;
            }
            return sb.ToString();
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