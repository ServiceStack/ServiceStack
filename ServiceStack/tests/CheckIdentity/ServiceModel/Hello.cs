using ServiceStack;

namespace CheckIdentity.ServiceModel
{
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>, IGet
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    [ValidateIsAuthenticated]
    [Route("/helloauth/{Name}")]
    public class HelloAuth : IReturn<HelloResponse>, IGet
    {
        public string Name { get; set; }
    }

    [ValidateHasRole("TheRole")]
    [Route("/hellorole/{Name}")]
    public class HelloRole : IReturn<HelloResponse>, IGet
    {
        public string Name { get; set; }
    }
}
