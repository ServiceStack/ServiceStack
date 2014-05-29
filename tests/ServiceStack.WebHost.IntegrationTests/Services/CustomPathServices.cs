namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/gettestapi")]
    public class GetTestapi
    {
    }

    public class CustomPathServices : Service
    {
        public object Any(GetTestapi request)
        {
            return new HelloResponse { Result = "GetTestapi" };
        }
    }
}