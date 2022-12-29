using System;
using ServiceStack;

namespace Check.ServiceModel
{
    public class Organization
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
    }

    [Route("/organizations/{Id}", Verbs = "GET")]
    public class GetOrganizationRequest : IReturn<Organization>
    {
        public Guid Id { get; set; }
        public bool IncludeAddresses { get; set; }
    }
}