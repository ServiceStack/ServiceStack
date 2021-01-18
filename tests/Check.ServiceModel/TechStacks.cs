using System;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    public interface IRegisterStats
    {
        string GetStatsId();
    }

    [Route("/technology/{Slug}")]
    public class GetTechnology : IReturn<GetTechnologyResponse>, IRegisterStats
    {
        public string Slug { get; set; }

        public long Id
        {
            set => Slug = value.ToString();
        }

        public string GetStatsId()
        {
            return "/tech/" + Slug;
        }
    }

    public class GetTechnologyResponse
    {
        public DateTime Created { get; set; }

        public Technology Technology { get; set; }

        public List<TechnologyStack> TechnologyStacks { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public enum TechnologyTier
    {
        [Description("Programming Languages")]
        ProgrammingLanguage,

        [Description("Client Libraries")]
        Client,

        [Description("HTTP Server Technologies")]
        Http,
        
        [Description("Server Libraries")]
        Server,

        [Description("Databases and NoSQL Datastores")]
        Data,
        
        [Description("Server Software")]
        SoftwareInfrastructure,
        
        [Description("Operating Systems")]
        OperatingSystem,
        
        [Description("Cloud/Hardware Infrastructure")]
        HardwareInfrastructure,

        [Description("3rd Party APIs/Services")]
        ThirdPartyServices,
    }
    
    public class Technology : TechnologyBase {}

    public abstract class TechnologyBase
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Name { get; set; }
        public string VendorName { get; set; }
        public string VendorUrl { get; set; }
        public string ProductUrl { get; set; }
        public string LogoUrl { get; set; }
        public string Description { get; set; }

        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }
        public string OwnerId { get; set; }

        [Index]
        public string Slug { get; set; }

        public bool LogoApproved { get; set; }
        public bool IsLocked { get; set; }

        public TechnologyTier Tier { get; set; }

        public DateTime? LastStatusUpdate { get; set; }

        public int? OrganizationId { get; set; }

        public long? CommentsPostId { get; set; }

        [Default(0)]
        public int ViewCount { get; set; }

        [Default(0)]
        public int FavCount { get; set; }
    }

    public class TechnologyStack : TechnologyStackBase {}

    public abstract class TechnologyStackBase
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Name { get; set; }
        public string VendorName { get; set; }
        public string Description { get; set; }
        public string AppUrl { get; set; }
        public string ScreenshotUrl { get; set; }

        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }

        public bool IsLocked { get; set; }

        public string OwnerId { get; set; }

        [Index]
        public string Slug { get; set; }

        [StringLength(StringLengthAttribute.MaxText)]
        public string Details { get; set; }

        [StringLength(StringLengthAttribute.MaxText)]
        public string DetailsHtml { get; set; }

        public DateTime? LastStatusUpdate { get; set; }

        public int? OrganizationId { get; set; }

        public long? CommentsPostId { get; set; }

        [Default(0)]
        public int ViewCount { get; set; }

        [Default(0)]
        public int FavCount { get; set; }
    }

}