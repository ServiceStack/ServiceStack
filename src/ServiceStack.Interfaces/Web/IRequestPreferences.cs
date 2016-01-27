namespace ServiceStack.Web
{
    public interface IRequestPreferences
    {
        bool AcceptsGzip { get; }

        bool AcceptsDeflate { get; }
    }
}