namespace ServiceStack.ServiceInterface
{
	public interface IXmlServiceRequest
	{
		int? Version { get; }
		string OperationName { get; }
	}
}