namespace ServiceStack.OpenApi.Tests.Services
{
    [Route("/secured-service")]
    public class SecuredRequest : IReturn<string> { }

    [Route("/secured-dto-service")]
    [Authenticate]
    public class SecuredDtoRequest : IReturn<string> { }

    [Route("/secured-ops-service")]
    public class SecuredOpsRequest : IReturn<string> { }

    [Authenticate]
    public class SecuredService : Service
    {
        public object Any(SecuredRequest request) => "Secured";
    }

    public class SecuredDtoService : Service
    {
        public string Any(SecuredDtoRequest request) => "Secured";
    }

    public class SecuredOpsService : Service
    {
        [Authenticate]
        public string Get(SecuredOpsRequest request) => "Secured";

        public string Post(SecuredOpsRequest request) => "Not Secured";
    }
}
