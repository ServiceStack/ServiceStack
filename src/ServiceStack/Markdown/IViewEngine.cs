namespace ServiceStack.Markdown
{
	public interface IViewEngine
	{
		string RenderPartial(string pageName, object model, bool renderHtml);
	}
}