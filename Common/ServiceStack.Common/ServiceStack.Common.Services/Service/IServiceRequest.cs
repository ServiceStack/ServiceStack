namespace ServiceStack.Common.Services.Service
{
    public interface IServiceRequest
    {
        int? Version { get; }
        string OperationName { get; }
    }
}