using ServiceStack;

namespace PclTest.ServiceModel
{
    [Route("/hello")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/helloauth")]
    public class HelloAuth : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }
}
