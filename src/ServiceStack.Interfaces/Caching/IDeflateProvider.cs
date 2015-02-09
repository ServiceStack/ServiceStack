namespace ServiceStack.Caching
{
    public interface IDeflateProvider
    {
        byte[] Deflate(string text);

        string Inflate(byte[] gzBuffer);
    }
}
