using System.IO;

namespace ServiceStack.Caching
{
    public interface IGZipProvider
    {
        byte[] GZip(string text);
        byte[] GZip(byte[] bytes);

        string GUnzip(byte[] gzBuffer);
        byte[] GUnzipBytes(byte[] gzBuffer);

        Stream GZipStream(Stream outputStream);
    }
}