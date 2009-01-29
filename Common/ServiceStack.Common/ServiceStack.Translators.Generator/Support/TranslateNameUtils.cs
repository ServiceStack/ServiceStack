using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Translators.Generator.Support
{
	public static class TranslateNameUtils
	{

		public static string GetConvertToTargetMethodName(this TranslateAttribute attr)
		{
			var prefix = string.IsNullOrEmpty(attr.TargetMethodPrefix) ? "To" : attr.TargetMethodPrefix;
			return prefix + attr.TargetType.Name;
		}

		public static string GetConvertToTargetsMethodName(this TranslateAttribute attr)
		{
			var prefix = string.IsNullOrEmpty(attr.TargetMethodPrefix) ? "To" : attr.TargetMethodPrefix;
			return prefix + attr.TargetType.Name + "s";
		}

		public static string GetUpdateTargetMethodName(this TranslateAttribute attr)
		{
			return "Update" + attr.SourceType.Name;
		}

		public static string GetConvertToSourceMethodName(this TranslateAttribute attr)
		{
			var prefix = string.IsNullOrEmpty(attr.SourceMethodPrefix) ? "To" : attr.SourceMethodPrefix;
			return prefix + attr.SourceType.Name;
		}

		public static string GetConvertToSourcesMethodName(this TranslateAttribute attr)
		{
			var prefix = string.IsNullOrEmpty(attr.SourceMethodPrefix) ? "To" : attr.SourceMethodPrefix;
			return prefix + attr.SourceType.Name + "s";
		}

		public static string ToFormatString(this TranslateAttribute attr)
		{
			return string.Format("{0} => {1}", attr.SourceType.Name, attr.TargetType.Name);
		}
	}
}
