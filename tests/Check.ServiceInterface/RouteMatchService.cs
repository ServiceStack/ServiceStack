using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/matchroute/html", MatchRule = "AcceptsHtml")]
    public class MatchesHtml : IReturn<MatchesHtml>
    {
        public string Name { get; set; }
    }

    [Route("/matchroute/json", MatchRule = "AcceptsJson")]
    public class MatchesJson : IReturn<MatchesJson>
    {
        public string Name { get; set; }
    }

    [Route("/matchroute/csv", MatchRule = "AcceptsCsv")]
    public class MatchesCsv : IReturn<MatchesCsv>
    {
        public string Name { get; set; }
    }

    [Route("/matchlast/{Id}", MatchRule = @"LastInt")]
    public class MatchesLastInt
    {
        public int Id { get; set; }
    }

    [Route("/matchlast/{Slug}", MatchRule = @"!LastInt")]
    public class MatchesNotLastInt
    {
        public string Slug { get; set; }
    }

    [Route("/matchregex/{Id}", MatchRule = @"PathInfo =~ \/[0-9]+$")]
    public class MatchesId
    {
        public int Id { get; set; }
    }

    [Route("/matchregex/{Slug}", MatchRule = @"PathInfo =~ \/[^0-9]+$")]
    public class MatchesSlug
    {
        public string Slug { get; set; }
    }

    public class RouteMatchService : Service
    {
        public object Any(MatchesHtml request) => request;
        public object Any(MatchesJson request) => request;
        //public object Any(MatchesCsv request) => request;

        public object Any(MatchesLastInt request) => request;
        public object Any(MatchesNotLastInt request) => request;

        public object Any(MatchesId request) => request;
        public object Any(MatchesSlug request) => request;
    }
}