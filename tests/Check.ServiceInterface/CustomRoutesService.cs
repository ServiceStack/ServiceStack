﻿using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/Routing/LeadPost.aspx")]
    public class LegacyLeadPost
    {
        public string LeadType { get; set; }
        public int MyId { get; set; }
    }

    public class CustomRoutesService : Service
    {
        public object Any(LegacyLeadPost request)
        {
            return request;
        }
    }
}