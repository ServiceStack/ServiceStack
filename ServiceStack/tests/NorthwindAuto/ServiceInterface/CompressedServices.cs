using ServiceStack;

namespace MyApp.ServiceInterface;

[Route("/compress/{**Path}")]
public class CompressFile : IGet, IReturn<byte[]>
{
    public string Path { get; set; }
}

[CompressResponse]
public class CompressedServices : Service
{
    public object Any(CompressFile request)
    {
        var file = VirtualFileSources.GetFile(request.Path);
        if (file == null)
            throw HttpError.NotFound($"{request.Path} does not exist");

        return new HttpResult(file);
    }
}
