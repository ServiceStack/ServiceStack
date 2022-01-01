using ServiceStack;

namespace MyApp.ServiceModel;

[Tag("hello")]
[Route("/hello/{Name}")]
public class Hello : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[Tag("hello")]
[Route("/hellosecure/{Name}")]
[ValidateIsAuthenticated]
public class HelloSecure : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[Tag("hello")]
[Route("/hello-long/{Name}")]
[ValidateIsAuthenticated]
public class HelloVeryLongOperationNameVersions : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[Tag("hello")]
[Route("/hello-long/{Name}")]
[ValidateIsAuthenticated]
public class HelloVeryLongOperationNameVersionsAndThenSome : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

public class HelloResponse
{
    public string Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}