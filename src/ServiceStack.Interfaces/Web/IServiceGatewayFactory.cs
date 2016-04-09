namespace ServiceStack.Web
{
    public interface IServiceGatewayFactory
    {
        IServiceGateway GetServiceGateway(IRequest request);
    }
}