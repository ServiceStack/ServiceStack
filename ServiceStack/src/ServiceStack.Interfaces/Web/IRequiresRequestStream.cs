using System.IO;

namespace ServiceStack.Web
{
    /// <summary>
    /// Implement on Request DTOs that need access to the raw Request Stream
    /// </summary>
    public interface IRequiresRequestStream
    {
        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        Stream RequestStream { get; set; }
    }
}