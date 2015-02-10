namespace ServiceStack.AuthWeb.Tests
{
    [RequiredRole("Manager")]
    public class RequiresManager
    {
        public string Name { get; set; }
    }

    [RequiredRole("Manager","Employee")]
    public class RequiresEmployee
    {
        public string Name { get; set; }
    }

    [RequiredRole("FooUser", "BarUser")]
    public class RequiresAnyUser
    {
        public string Name { get; set; }
    }

    [RequiredRole("Admin")]
    public class ProtectedServices : Service
    {
        public object Any(RequiresManager request)
        {
            return request;
        }

        public object Any(RequiresEmployee request)
        {
            return request;
        }

        public object Any(RequiresAnyUser request)
        {
            return request;
        }
    }

    [Route("/requiresauth")]
    public class RequiresAuth : IReturn<RequiresAuth>
    {
        public string Name { get; set; }        
    }

    [Authenticate]
    public class AuthService : Service
    {
        public object Any(RequiresAuth request)
        {
            return request;
        }
    }
}