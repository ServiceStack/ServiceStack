using System;

namespace ServiceStack.Translators.Generator.Filters
{
	public class CodeFilters
	{
		public static ICodeFilter GetCodeFilter(CodeLang codeLang)
		{
			switch (codeLang)
			{
				case CodeLang.CSharp:
					return new CSharpCodeFilter();
				default:
					throw new NotSupportedException("Do not support source filters for: " + codeLang);
			}
		}
	}
}