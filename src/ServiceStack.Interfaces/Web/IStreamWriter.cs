using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Web
{
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }

    public interface IStreamWriterAsync
    {
        Task WriteToAsync(Stream responseStream, CancellationToken token = default(CancellationToken));
    }
}