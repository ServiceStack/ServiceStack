using System;
using ServiceStack;

namespace CheckTemplatesCore;

[Route("/hello")]
[Route("/hello/{Name}")]
public class Hello
{
    public string Name { get; set; }
}

public class HelloResponse
{
    public string Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/throw404")]
[Route("/throw404/{Message}")]
public class Throw404
{
    public string Message { get; set; }
}

[Route("/throw")]
[Route("/throw/{Message}")]
public class Throw
{
    public string Message { get; set; }
}

[Route("/validation/test")]
public class ValidationTest : IReturn<ValidationTest>
{
    [ValidateNotNull]
    public string Name { get; set; }
}

[FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
public class ViewIndex
{
    public string PathInfo { get; set; }
}

public class MyServices : Service
{
    public object Any(Hello request) => new HelloResponse {
        Result = $"Hi, {request.Name}!"
    };
        
    public object Any(Throw404 request) => throw HttpError.NotFound(request.Message ?? "Not Found");
        
    public object Any(Throw request) => throw new Exception(request.Message ?? "Exception in 'Throw' Service");
        
    public object Any(ViewIndex request)
    {
        return Request.GetPageResult("/index");
        //equivalent to: return new PageResult(Request.GetPage("/index")).BindRequest(Request);
    }

    public object Any(ValidationTest request) => request;
}