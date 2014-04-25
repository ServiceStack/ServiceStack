namespace ServiceStack.Caching
{
    public interface IGZipProvider
    {
        byte[] GZip(string text);

        string GUnzip(byte[] gzBuffer);
    }
}