using System;
using System.Threading;
#if !UNITY
using System.Threading.Tasks;
#endif

namespace ServiceStack.Web
{
#if !UNITY
    [Obsolete("Use IPartialWriterAsync")]
#endif
    public interface IPartialWriter
	{
        /// <summary>
        /// Whether this HttpResult allows Partial Response
        /// </summary>
        bool IsPartialRequest { get; }

        /// <summary>
        /// Write a partial content result
        /// </summary>
        void WritePartialTo(IResponse response);
	}

#if !UNITY
    public interface IPartialWriterAsync
    {
        /// <summary>
        /// Whether this HttpResult allows Partial Response
        /// </summary>
        bool IsPartialRequest { get; }

        /// <summary>
        /// Write a partial content result
        /// </summary>
        Task WritePartialToAsync(IResponse response, CancellationToken token = default(CancellationToken));
    }
#endif
}