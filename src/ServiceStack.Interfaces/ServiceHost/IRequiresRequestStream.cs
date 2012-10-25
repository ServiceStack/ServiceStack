using System.IO;

namespace ServiceStack.ServiceHost
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