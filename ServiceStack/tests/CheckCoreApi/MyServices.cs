using ServiceStack;

namespace CheckCoreApi;

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

public class MyServices : Service
{
    public object Any(Hello request) => new HelloResponse {
        Result = $"Hello, {request.Name}!"
    };
}
