namespace ServiceStack.Messaging
{
	public interface IMessageError
	{
		string ErrorCode { get; }
		string Message { get; }
		string StackTrace { get; }
	}
}