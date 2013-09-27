namespace ServiceStack.Web
{
	public interface IRequestAttributes
	{
		bool AcceptsGzip { get; }

		bool AcceptsDeflate { get; }
	}
}