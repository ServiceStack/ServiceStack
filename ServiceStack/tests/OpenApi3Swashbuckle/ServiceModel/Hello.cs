using ServiceStack;

namespace MyApp.ServiceModel;

[Tag("hello")]
[Route("/hello", "GET")]
[Route("/hello/{Name}", "GET")]
public class Hello : IGet, IReturn<HelloResponse>
{
    public string? Name { get; set; }
}

public class HelloResponse
{
    public string? Result { get; set; }
}

[Tag("hello")]
[Route("/hellosecure", "GET")]
[ValidateIsAuthenticated]
public class HelloSecure : IGet, IReturn<HelloResponse>
{
    public string? Name { get; set; }
}

[Tag("hello")]
[Route("/helloapikey", "GET")]
[ValidateApiKey]
public class HelloApiKey : IGet, IReturn<HelloResponse>
{
    public string? Name { get; set; }
}