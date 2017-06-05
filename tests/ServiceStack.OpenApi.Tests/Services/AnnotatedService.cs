namespace ServiceStack.OpenApi.Tests.Services
{
    [Api("Gets the movie")]
    [Route("/movie/{Id}")]
    public class GetMovie : IReturn<MovieResponse>
    {
        [ApiMember(IsRequired = true, Description = "Required ID of Movie.", DataType = "integer", ParameterType = "path")]
        public long Id { get; set; }

        [ApiMember(IsRequired = false, AllowMultiple = true, Description = "List of additional objects to include in the movie response.")]
        [ApiAllowableValues("Includes", Values = new string[] { "Genres", "Releases", "Contributors", "AlternateTitles", "Descriptions", "Companies", "Tags", "Images", "Videos" })]   // This breaks the swagger UI
        public string[] Includes { get; set; }
    }

    [Api("Movie response with includes")]
    public class MovieResponse
    {
        public string[] Includes { get; set; }
    }

    public class AnnotatedService : Service
    {
        public object Any(GetMovie request) => new MovieResponse {Includes = request.Includes};
    }
}