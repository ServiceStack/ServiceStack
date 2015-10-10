using System.IO;

namespace ServiceStack.Caching
{
    public interface IGZipProvider
    {
        byte[] GZip(string text);

        string GUnzip(byte[] gzBuffer);

        Stream GetGZipCompressionStream(Stream outputStream);
    }
}