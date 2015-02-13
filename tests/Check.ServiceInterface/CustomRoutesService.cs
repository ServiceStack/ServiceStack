using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/Routing/LeadPost.aspx")]
    public class LegacyLeadPost
    {
        public string LeadType { get; set; }
        public int MyId { get; set; }
    }

    [Route("/info/{Id}")]
    public class Info
    {
        public string Id { get; set; }
    }

    public class CustomRoutesService : Service
    {
        public object Any(LegacyLeadPost request)
        {
            return request;
        }

        public object Any(Info request)
        {
            return request.ToAbsoluteUri()
                + " | " + request.ToAbsoluteUri(base.Request);
        }
    }
}