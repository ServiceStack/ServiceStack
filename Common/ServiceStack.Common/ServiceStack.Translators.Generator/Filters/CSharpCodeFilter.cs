using System.Text;

namespace ServiceStack.Translators.Generator.Filters
{
	public class CSharpCodeFilter : ICodeFilter
	{
		public StringBuilder ApplyExtensionFilter(StringBuilder sourceBuilder)
		{
			sourceBuilder = sourceBuilder
				.Replace("sealed", "static")
				.Replace("[Extension()]", "this");
			
			return sourceBuilder;
		}
	}
}