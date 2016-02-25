using System.IO;

namespace ServiceStack.Web
{
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }
}