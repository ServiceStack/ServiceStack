using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Web
{
    [Obsolete("Use IPartialWriterAsync")]
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
}