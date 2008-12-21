namespace ServiceStack.Common.Services.Service
{
    public interface IXmlServiceRequest
    {
        int? Version { get; }
        string OperationName { get; }
    }
}