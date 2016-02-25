using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/files/{Path*}")]
    public class GetFile
    {
        public string Path { get; set; }
    }

    public class FileServices : Service
    {
        public object Any(GetFile request)
        {
            var file = VirtualFileSources.GetFile(request.Path);
            if (file == null)
                throw HttpError.NotFound("File '{0}' does not exist".Fmt(request.Path));

            return new HttpResult(file) {
                ContentType = MimeTypes.GetMimeType(file.Extension)
            };
        }
    }
}