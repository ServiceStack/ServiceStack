namespace ServiceStack.Web
{
    public interface IRequestPreferences
    {
        bool AcceptsBrotli { get; }
        bool AcceptsDeflate { get; }
        bool AcceptsGzip { get; }
    }
}