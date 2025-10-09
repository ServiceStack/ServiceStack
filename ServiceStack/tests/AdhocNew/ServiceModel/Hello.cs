using ServiceStack;

namespace MyApp.ServiceModel;

[Route("/hello/{Name}")]
public class Hello : IGet, IReturn<HelloResponse>
{
    public string? Name { get; set; }
}

public class HelloResponse
{
    public required string Result { get; set; }
}