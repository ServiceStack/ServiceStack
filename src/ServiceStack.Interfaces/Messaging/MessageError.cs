namespace ServiceStack.Messaging
{
	/// <summary>
	/// An Error Message Type that can be easily serialized
	/// </summary>
	public class MessageError
	{
		public string ErrorCode { get; set; }

		public string Message { get; set; }

		public string StackTrace { get; set; }
	}
}