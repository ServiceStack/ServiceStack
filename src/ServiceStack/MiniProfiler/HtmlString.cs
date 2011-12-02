namespace ServiceStack.MiniProfiler
{
	public interface IHtmlString
	{
		
	}

	public class HtmlString : IHtmlString
	{
		private string value;

		public HtmlString(string value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return value;
		}
	}
}