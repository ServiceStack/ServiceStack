using System;
using System.IO;
using System.Threading;
#if !UNITY
using System.Threading.Tasks;
#endif

namespace ServiceStack.Web
{
#if !UNITY
    [Obsolete("Use IStreamWriterAsync")]
#endif
    public interface IStreamWriter
    {
        void WriteTo(Stream responseStream);
    }

#if !UNITY
    public interface IStreamWriterAsync
    {
        Task WriteToAsync(Stream responseStream, CancellationToken token = default(CancellationToken));
    }
#endif
}