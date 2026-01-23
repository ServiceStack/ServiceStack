using ServiceStack;
using MyApp;

namespace MyApp.ServiceModel;

[Route("/hello")]
[Route("/hello/{Name}")]
public class Hello : IGet, IReturn<HelloResponse>
{
    public string? Name { get; set; }
}

public class HelloResponse
{
    public string Result { get; set; }
}
