namespace ServiceStack;

[DefaultRequest(typeof(GetFile))]
public class GetFileService : Service
{
    public object Get(GetFile request)
    {
        var file = VirtualFileSources.GetFile(request.Path);
        if (file == null)
            throw HttpError.NotFound("File does not exist");

        var bytes = file.ReadAllBytes();
        var to = new FileContent {
            Name = file.Name,
            Type = MimeTypes.GetMimeType(file.Extension),
            Body = bytes,
            Length = bytes.Length,
        };
        return to;
    }
}