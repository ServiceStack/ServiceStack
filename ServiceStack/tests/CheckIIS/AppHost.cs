using Funq;
using ServiceStack;

namespace CheckIIS
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base(nameof(MyServices), typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig {
                DebugMode = true,
                // EnableAutoHtmlResponses = false,
            });

            Plugins.Add(new ServerEventsFeature());
            Plugins.Add(new SoapFormat());
        }
    }
    
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }
    public class HelloResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class AddInts : IReturn<AddIntsResponse>
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    public class AddIntsResponse
    {
        public int Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }


    public class MyServices : Service
    {
        public object Any(Hello request) => new HelloResponse
        {
            Result = $"Hi, {request.Name}!"
        };

        public object Any(AddInts request) => new AddIntsResponse {
            Result = request.A + request.B
        };
    }    
}