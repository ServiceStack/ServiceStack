using System;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceInterface
{
    [References(typeof(acsprofileResponse))]
    [Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")]
    [Route("/api/acsprofiles/{profileId}")]
    [Alias("ACSProfiles")]
    public class ACSProfile : IReturn<acsprofileResponse>, IHasVersion, IHasSessionId
    {
        [PrimaryKey]
        public string profileId { get; set; }

        [StringLength(20)]
        public string shortName { get; set; }

        [StringLength(60)]
        public string longName { get; set; }

        [StringLength(20)]
        [Index(Unique = false)]
        public string regionId { get; set; }

        [StringLength(20)]
        [Index(Unique = false)]
        public string groupId { get; set; }

        [StringLength(12)]
        [Index(Unique = false)]
        public string deviceID { get; set; }

        public DateTime lastUpdated { get; set; }

        public bool enabled { get; set; }
        public int Version { get; set; }
        public string SessionId { get; set; }
    }

    public class acsprofileResponse
    {
        public string profileId { get; set; }
    }

    public class ACSProfileService : Service
    {
        public object Any(ACSProfile request)
        {
            return new acsprofileResponse { profileId = request.profileId };
        }
    }
}