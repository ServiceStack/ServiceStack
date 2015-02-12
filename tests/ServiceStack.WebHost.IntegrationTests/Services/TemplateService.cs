namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/templates", "POST")]
    public class PostTemplateRequest : IReturn<PostTemplateResponse>
    {
        public string Template { get; set; }
    }

    public class PostTemplateResponse
    {
        public string PostResult { get; set; }
    }

    [Route("/templates", "GET")]
    public class GetTemplatesRequest : IReturn<GetTemplatesResponse>
    {
        public string Name { get; set; }
    }

    public class GetTemplatesResponse
    {
        public string GetResult { get; set; }
    }

    [Route("/templates/{Name}", "GET")]
    public class GetTemplateRequest : IReturn<GetTemplateResponse>
    {
        public string Name { get; set; }
    }

    public class GetTemplateResponse
    {
        public string GetSingleResult { get; set; }
    }

    public class TemplateService : Service
    {
        public object Post(PostTemplateRequest request)
        {
            return new PostTemplateResponse { PostResult = request.Template };
        }

        public object Get(GetTemplatesRequest request)
        {
            return new GetTemplatesResponse { GetResult = request.Name };
        }

        public object Get(GetTemplateRequest request)
        {
            return new GetTemplateResponse { GetSingleResult = request.Name };
        }
    }
}