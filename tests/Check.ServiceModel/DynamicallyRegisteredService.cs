using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/dynamically/registered/{Name}")]
    public class DynamicallyRegistered
    {
        public string Name { get; set; }
    }

    public class DynamicallyRegisteredService : Service
    {
        public object Any(DynamicallyRegistered request)
        {
            return request;
        }
    }

    public class DynamicallyRegisteredPlugin : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RegisterServicesInAssembly(GetType().Assembly);
        }
    }
}