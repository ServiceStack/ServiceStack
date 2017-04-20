namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/verbonly/post", "POST")]
    public class PostOnly : IReturn<PostOnly>
    {
        public string Id { get; set; }
    }

    [Route("/verbonly/put", "PUT")]
    public class PutOnly : IReturn<PutOnly>
    {
        public string Id { get; set; }
    }

    [Route("/verbonly/patch", "PATCH")]
    public class PatchOnly : IReturn<PatchOnly>
    {
        public string Id { get; set; }
    }

    [Route("/verbonly/get", "GET")]
    public class GetOnly : IReturn<GetOnly>
    {
        public string Id { get; set; }
    }

    [Route("/verbonly/delete", "DELETE")]
    public class DeleteOnly : IReturn<DeleteOnly>
    {
        public string Id { get; set; }
    }

    public class VerbOnlyService : Service
    {
        public object Any(PostOnly request) => request;
        public object Any(PutOnly request) => request;
        public object Any(PatchOnly request) => request;
        public object Any(GetOnly request) => request;
        public object Any(DeleteOnly request) => request;
    }
}