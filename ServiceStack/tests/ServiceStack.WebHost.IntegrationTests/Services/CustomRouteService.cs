namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/custom")]
    [Route("/custom/{Data}")]
    public class CustomRoute : IReturn<CustomRoute>
    {
        public string Data { get; set; }
    }

    [Route("/customdot/{Id}.{Data}")]
    public class CustomRouteDot : IReturn<CustomRouteDot>
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }

    public class CustomRouteService : IService
    {
        public object Any(CustomRoute request)
        {
            return request;
        }

        public object Any(CustomRouteDot request)
        {
            return request;
        }
    }
}