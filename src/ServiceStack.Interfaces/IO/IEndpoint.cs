namespace ServiceStack.IO
{
    public interface IEndpoint
    {
        string Host { get; }
        int Port { get; }
    }
}