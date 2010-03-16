namespace ServiceStack.Messaging
{
	public class MessageError
	{
		public string ErrorCode { get; set; }

		public string Message { get; set; }

		public string StackTrace { get; set; }
	}
}