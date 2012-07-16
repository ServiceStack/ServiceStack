namespace ServiceStack.Html
{
	public interface IViewEngine
	{
		string RenderPartial(string pageName, object model, bool renderHtml);
	}
}