using ServiceStack;

namespace MyApp.ServiceModel;

[Tag("hello")]
[Route("/hello/{Name}")]
[ValidateHasRole("Employee")]
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
[Route("/hello-long/{Name}", "PATCH,PUT")]
[Route("/hello-very-long/{Name}", "GET,POST,PUT")]
[ValidateHasRole("Employee")]
[ValidateHasPermission("ThePermission")]
[ValidateIsAuthenticated]
public class HelloVeryLongOperationNameVersions : IReturn<HelloResponse>, IGet, IPost, IPut, IPatch
{
    public string Name { get; set; }
    public string[] Names { get; set; }
    public int[] Ids { get; set; }
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