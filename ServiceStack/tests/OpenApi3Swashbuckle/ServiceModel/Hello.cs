using ServiceStack;

namespace OpenApi3Swashbuckle.ServiceModel;

[Route("/hello", "GET")]
[Route("/hello/{Name}", "GET")]
public class Hello : IReturn<HelloResponse>
{
    public string? Name { get; set; }
}

public class HelloResponse
{
    public string? Result { get; set; }
}

