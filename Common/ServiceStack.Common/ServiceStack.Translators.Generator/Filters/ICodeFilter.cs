using System.Text;

namespace ServiceStack.Translators.Generator.Filters
{
	public interface ICodeFilter
	{
		StringBuilder ApplyExtensionFilter(StringBuilder sourceBuilder);
	}
}