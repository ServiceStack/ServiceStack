using ServiceStack;
using ServiceStack.Web;

namespace MyApp.ServiceInterface;

[Route("/sendjson")]
public class SendJson : IRequiresRequestStream, IReturn<string>
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public Stream RequestStream { get; set; }
}

[Route("/sendtext")]
public class SendText : IRequiresRequestStream, IReturn<string>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ContentType { get; set; }

    public Stream RequestStream { get; set; }
}

[Route("/sendraw")]
public class SendRaw : IRequiresRequestStream, IReturn<byte[]>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ContentType { get; set; }

    public Stream RequestStream { get; set; }
}

public class SendRawService : Service
{
    [JsonOnly]
    public async Task<object> Any(SendJson request)
    {
        base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

        return await request.RequestStream.ReadToEndAsync();
    }

    public async Task<object> Any(SendText request)
    {
        base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

        base.Request.ResponseContentType = request.ContentType ?? base.Request.AcceptTypes[0];
        return await request.RequestStream.ReadToEndAsync();
    }

    public async Task<object> Any(SendRaw request)
    {
        base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

        base.Request.ResponseContentType = request.ContentType ?? base.Request.AcceptTypes[0];
        return await request.RequestStream.ReadToEndAsync();
    }
}