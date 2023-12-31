using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Mvc;

namespace CheckRazorCore;

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

[Route("/testauth")]
public class TestAuth : IReturn<TestAuth> {}

[Route("/razor/view/{Path}")]
public class RazorView : IReturn<string>
{
    public string Path { get; set; }
    public string Layout { get; set; }
}

[Route("/razor/hello/{Path}")]
public class RazorViewHello : IReturn<string>
{
    public string Path { get; set; }
    public string Layout { get; set; }
}

[Route("/razor/bytes/{Path}")]
public class RazorViewBytes : IReturn<string>
{
    public string Path { get; set; }
    public string Layout { get; set; }
}

[Route("/razor/content")]
[Route("/razor/content/{Path}")]
public class RazorContent : IReturn<string>
{
    public string Path { get; set; }
    public string Layout { get; set; }
}

public class RazorViewResponse
{
    public string Html { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public class MyServices : Service
{
    public object Any(Hello request)
    {
        return new HelloResponse {Result = $"Hello, {request.Name}!"};
    }

    [Authenticate]
    public object Any(TestAuth request) => request;

    public async Task<object> Any(RazorView request)
    {
        var razor = GetPlugin<RazorFormat>();
        var view = razor.GetViewPage(request.Path);
        if (view == null)
            throw HttpError.NotFound("Razor view not found: " + request.Path);

        var ret = await razor.RenderToHtmlAsync(view, new { Name = "World" },
            layout:request.Layout);
        return ret;
    }

    public async Task<object> Any(RazorViewHello request)
    {
        var razor = GetPlugin<RazorFormat>();
        var view = razor.GetViewPage(request.Path);
        if (view == null)
            throw HttpError.NotFound("Razor view not found: " + request.Path);

        var ret = await razor.RenderToHtmlAsync(view, new Hello { Name = "World" },
            layout:request.Layout);
        return ret;
    }

    public async Task Any(RazorViewBytes request)
    {
        var razor = GetPlugin<RazorFormat>();
        var view = razor.GetViewPage(request.Path);
        if (view == null)
            throw HttpError.NotFound("Razor view not found: " + request.Path);

        await razor.WriteHtmlAsync(Response.OutputStream, view, new Hello { Name = "World" }, 
            layout:request.Layout);
    }

    public async Task<object> Any(RazorContent request)
    {
        var razor = GetPlugin<RazorFormat>();
        var view = razor.GetContentPage(request.Path);
        if (view == null)
            throw HttpError.NotFound("Razor view not found: " + request.Path);

        var ret = await razor.RenderToHtmlAsync(view, new Hello { Name = "World" },
            layout:request.Layout);
        return ret;
    }
}
