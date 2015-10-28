/* Options:
Date: 2015-10-27 19:01:09
Version: 4.046
BaseUrl: http://techstacks.io

//GlobalNamespace: 
//MakePartial: True
//MakeVirtual: True
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//IncludeTypes: 
//ExcludeTypes: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using TechStacks.ServiceModel.Types;
using TechStacks.ServiceModel;
using TechStacks.ServiceInterface;


namespace TechStacks.ServiceInterface
{

    [Route("/tech")]
    public partial class ClientAllTechnologies
    {
    }

    [Route("/stacks")]
    public partial class ClientAllTechnologyStacks
    {
    }

    [Route("/tech/{Slug}")]
    public partial class ClientTechnology
    {
        public virtual string Slug { get; set; }
    }

    [Route("/users/{UserName}")]
    public partial class ClientUser
    {
        public virtual string UserName { get; set; }
    }

    [Route("/{PathInfo*}")]
    public partial class FallbackForClientRoutes
    {
        public virtual string PathInfo { get; set; }
    }

    [Route("/ping")]
    public partial class Ping
    {
    }

    public partial class Post
    {
        public Post()
        {
            Comments = new List<PostComment>{};
        }

        public virtual int Id { get; set; }
        public virtual string UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Date { get; set; }
        public virtual string ShortDate { get; set; }
        public virtual string TextHtml { get; set; }
        public virtual List<PostComment> Comments { get; set; }
    }

    public partial class PostComment
    {
        public virtual int Id { get; set; }
        public virtual int PostId { get; set; }
        public virtual string UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Date { get; set; }
        public virtual string ShortDate { get; set; }
        public virtual string TextHtml { get; set; }
    }

    [Route("/posts")]
    public partial class QueryPosts
        : QueryBase<Post>, IReturn<QueryResponse<Post>>
    {
    }
}

namespace TechStacks.ServiceModel
{

    [Route("/favorites/technology/{TechnologyId}", "PUT")]
    public partial class AddFavoriteTechnology
        : IReturn<FavoriteTechnologyResponse>
    {
        public virtual int TechnologyId { get; set; }
    }

    [Route("/favorites/techtacks/{TechnologyStackId}", "PUT")]
    public partial class AddFavoriteTechStack
        : IReturn<FavoriteTechStackResponse>
    {
        public virtual int TechnologyStackId { get; set; }
    }

    [Route("/app-overview")]
    public partial class AppOverview
        : IReturn<AppOverviewResponse>
    {
        public virtual bool Reload { get; set; }
    }

    public partial class AppOverviewResponse
    {
        public AppOverviewResponse()
        {
            AllTiers = new List<Option>{};
            TopTechnologies = new List<TechnologyInfo>{};
        }

        public virtual DateTime Created { get; set; }
        public virtual List<Option> AllTiers { get; set; }
        public virtual List<TechnologyInfo> TopTechnologies { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/technology", "POST")]
    public partial class CreateTechnology
        : IReturn<CreateTechnologyResponse>
    {
        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string VendorUrl { get; set; }
        public virtual string ProductUrl { get; set; }
        public virtual string LogoUrl { get; set; }
        public virtual string Description { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual TechnologyTier Tier { get; set; }
    }

    public partial class CreateTechnologyResponse
    {
        public virtual Technology Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/techstacks", "POST")]
    public partial class CreateTechnologyStack
        : IReturn<CreateTechnologyStackResponse>
    {
        public CreateTechnologyStack()
        {
            TechnologyIds = new List<long>{};
        }

        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string AppUrl { get; set; }
        public virtual string ScreenshotUrl { get; set; }
        public virtual string Description { get; set; }
        public virtual string Details { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual List<long> TechnologyIds { get; set; }
    }

    public partial class CreateTechnologyStackResponse
    {
        public virtual TechStackDetails Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/technology/{Id}", "DELETE")]
    public partial class DeleteTechnology
        : IReturn<DeleteTechnologyResponse>
    {
        public virtual long Id { get; set; }
    }

    public partial class DeleteTechnologyResponse
    {
        public virtual Technology Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/techstacks/{Id}", "DELETE")]
    public partial class DeleteTechnologyStack
        : IReturn<DeleteTechnologyStackResponse>
    {
        public virtual long Id { get; set; }
    }

    public partial class DeleteTechnologyStackResponse
    {
        public virtual TechStackDetails Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class FavoriteTechnologyResponse
    {
        public virtual Technology Result { get; set; }
    }

    public partial class FavoriteTechStackResponse
    {
        public virtual TechnologyStack Result { get; set; }
    }

    [Route("/technology/search")]
    [AutoQueryViewer(Title="Find Technologies", Description="Explore different Technologies", IconUrl="/img/app/tech-white-75.png", DefaultSearchField="Tier", DefaultSearchType="=", DefaultSearchText="Data")]
    public partial class FindTechnologies
        : QueryBase<Technology>, IReturn<QueryResponse<Technology>>
    {
        public virtual string Name { get; set; }
        public virtual bool Reload { get; set; }
    }

    [Route("/techstacks/search")]
    [AutoQueryViewer(Title="Find Technology Stacks", Description="Explore different Technology Stacks", IconUrl="/img/app/stacks-white-75.png", DefaultSearchField="Description", DefaultSearchType="Contains", DefaultSearchText="ServiceStack")]
    public partial class FindTechStacks
        : QueryBase<TechnologyStack>, IReturn<QueryResponse<TechnologyStack>>
    {
        public virtual bool Reload { get; set; }
    }

    [Route("/technology", "GET")]
    public partial class GetAllTechnologies
        : IReturn<GetAllTechnologiesResponse>
    {
    }

    public partial class GetAllTechnologiesResponse
    {
        public GetAllTechnologiesResponse()
        {
            Results = new List<Technology>{};
        }

        public virtual List<Technology> Results { get; set; }
    }

    [Route("/techstacks", "GET")]
    public partial class GetAllTechnologyStacks
        : IReturn<GetAllTechnologyStacksResponse>
    {
    }

    public partial class GetAllTechnologyStacksResponse
    {
        public GetAllTechnologyStacksResponse()
        {
            Results = new List<TechnologyStack>{};
        }

        public virtual List<TechnologyStack> Results { get; set; }
    }

    [Route("/config")]
    public partial class GetConfig
        : IReturn<GetConfigResponse>
    {
    }

    public partial class GetConfigResponse
    {
        public GetConfigResponse()
        {
            AllTiers = new List<Option>{};
        }

        public virtual List<Option> AllTiers { get; set; }
    }

    [Route("/favorites/technology", "GET")]
    public partial class GetFavoriteTechnologies
        : IReturn<GetFavoriteTechnologiesResponse>
    {
        public virtual int TechnologyId { get; set; }
    }

    public partial class GetFavoriteTechnologiesResponse
    {
        public GetFavoriteTechnologiesResponse()
        {
            Results = new List<Technology>{};
        }

        public virtual List<Technology> Results { get; set; }
    }

    [Route("/favorites/techtacks", "GET")]
    public partial class GetFavoriteTechStack
        : IReturn<GetFavoriteTechStackResponse>
    {
        public virtual int TechnologyStackId { get; set; }
    }

    public partial class GetFavoriteTechStackResponse
    {
        public GetFavoriteTechStackResponse()
        {
            Results = new List<TechnologyStack>{};
        }

        public virtual List<TechnologyStack> Results { get; set; }
    }

    [Route("/technology/{Slug}")]
    public partial class GetTechnology
        : IReturn<GetTechnologyResponse>
    {
        public virtual bool Reload { get; set; }
        public virtual string Slug { get; set; }
    }

    [Route("/technology/{Slug}/favorites")]
    public partial class GetTechnologyFavoriteDetails
        : IReturn<GetTechnologyFavoriteDetailsResponse>
    {
        public virtual string Slug { get; set; }
        public virtual bool Reload { get; set; }
    }

    public partial class GetTechnologyFavoriteDetailsResponse
    {
        public GetTechnologyFavoriteDetailsResponse()
        {
            Users = new List<string>{};
        }

        public virtual List<string> Users { get; set; }
        public virtual int FavoriteCount { get; set; }
    }

    [Route("/technology/{Slug}/previous-versions", "GET")]
    public partial class GetTechnologyPreviousVersions
        : IReturn<GetTechnologyPreviousVersionsResponse>
    {
        public virtual string Slug { get; set; }
    }

    public partial class GetTechnologyPreviousVersionsResponse
    {
        public GetTechnologyPreviousVersionsResponse()
        {
            Results = new List<TechnologyHistory>{};
        }

        public virtual List<TechnologyHistory> Results { get; set; }
    }

    public partial class GetTechnologyResponse
    {
        public GetTechnologyResponse()
        {
            TechnologyStacks = new List<TechnologyStack>{};
        }

        public virtual DateTime Created { get; set; }
        public virtual Technology Technology { get; set; }
        public virtual List<TechnologyStack> TechnologyStacks { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/techstacks/{Slug}", "GET")]
    public partial class GetTechnologyStack
        : IReturn<GetTechnologyStackResponse>
    {
        public virtual bool Reload { get; set; }
        public virtual string Slug { get; set; }
    }

    [Route("/techstacks/{Slug}/favorites")]
    public partial class GetTechnologyStackFavoriteDetails
        : IReturn<GetTechnologyStackFavoriteDetailsResponse>
    {
        public virtual string Slug { get; set; }
        public virtual bool Reload { get; set; }
    }

    public partial class GetTechnologyStackFavoriteDetailsResponse
    {
        public GetTechnologyStackFavoriteDetailsResponse()
        {
            Users = new List<string>{};
        }

        public virtual List<string> Users { get; set; }
        public virtual int FavoriteCount { get; set; }
    }

    [Route("/techstacks/{Slug}/previous-versions", "GET")]
    public partial class GetTechnologyStackPreviousVersions
        : IReturn<GetTechnologyStackPreviousVersionsResponse>
    {
        public virtual string Slug { get; set; }
    }

    public partial class GetTechnologyStackPreviousVersionsResponse
    {
        public GetTechnologyStackPreviousVersionsResponse()
        {
            Results = new List<TechnologyStackHistory>{};
        }

        public virtual List<TechnologyStackHistory> Results { get; set; }
    }

    public partial class GetTechnologyStackResponse
    {
        public virtual DateTime Created { get; set; }
        public virtual TechStackDetails Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/my-feed")]
    public partial class GetUserFeed
        : IReturn<GetUserFeedResponse>
    {
    }

    public partial class GetUserFeedResponse
    {
        public GetUserFeedResponse()
        {
            Results = new List<TechStackDetails>{};
        }

        public virtual List<TechStackDetails> Results { get; set; }
    }

    [Route("/userinfo/{UserName}")]
    public partial class GetUserInfo
        : IReturn<GetUserInfoResponse>
    {
        public virtual bool Reload { get; set; }
        public virtual string UserName { get; set; }
    }

    public partial class GetUserInfoResponse
    {
        public GetUserInfoResponse()
        {
            TechStacks = new List<TechnologyStack>{};
            FavoriteTechStacks = new List<TechnologyStack>{};
            FavoriteTechnologies = new List<Technology>{};
        }

        public virtual string UserName { get; set; }
        public virtual DateTime Created { get; set; }
        public virtual string AvatarUrl { get; set; }
        public virtual List<TechnologyStack> TechStacks { get; set; }
        public virtual List<TechnologyStack> FavoriteTechStacks { get; set; }
        public virtual List<Technology> FavoriteTechnologies { get; set; }
    }

    public partial class LockStackResponse
    {
    }

    [Route("/admin/technology/{TechnologyId}/lock")]
    public partial class LockTech
        : IReturn<LockStackResponse>
    {
        public virtual long TechnologyId { get; set; }
        public virtual bool IsLocked { get; set; }
    }

    [Route("/admin/techstacks/{TechnologyStackId}/lock")]
    public partial class LockTechStack
        : IReturn<LockStackResponse>
    {
        public virtual long TechnologyStackId { get; set; }
        public virtual bool IsLocked { get; set; }
    }

    [Route("/admin/technology/{TechnologyId}/logo")]
    public partial class LogoUrlApproval
        : IReturn<LogoUrlApprovalResponse>
    {
        public virtual long TechnologyId { get; set; }
        public virtual bool Approved { get; set; }
    }

    public partial class LogoUrlApprovalResponse
    {
        public virtual Technology Result { get; set; }
    }

    [DataContract]
    public partial class Option
    {
        [DataMember(Name="name")]
        public virtual string Name { get; set; }

        [DataMember(Name="title")]
        public virtual string Title { get; set; }

        [DataMember(Name="value")]
        public virtual TechnologyTier? Value { get; set; }
    }

    [Route("/overview")]
    public partial class Overview
        : IReturn<OverviewResponse>
    {
        public virtual bool Reload { get; set; }
    }

    public partial class OverviewResponse
    {
        public OverviewResponse()
        {
            TopUsers = new List<UserInfo>{};
            TopTechnologies = new List<TechnologyInfo>{};
            LatestTechStacks = new List<TechStackDetails>{};
            TopTechnologiesByTier = new Dictionary<TechnologyTier, List<TechnologyInfo>>{};
        }

        public virtual DateTime Created { get; set; }
        public virtual List<UserInfo> TopUsers { get; set; }
        public virtual List<TechnologyInfo> TopTechnologies { get; set; }
        public virtual List<TechStackDetails> LatestTechStacks { get; set; }
        public virtual Dictionary<TechnologyTier, List<TechnologyInfo>> TopTechnologiesByTier { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/favorites/technology/{TechnologyId}", "DELETE")]
    public partial class RemoveFavoriteTechnology
        : IReturn<FavoriteTechnologyResponse>
    {
        public virtual int TechnologyId { get; set; }
    }

    [Route("/favorites/techtacks/{TechnologyStackId}", "DELETE")]
    public partial class RemoveFavoriteTechStack
        : IReturn<FavoriteTechStackResponse>
    {
        public virtual int TechnologyStackId { get; set; }
    }

    [Route("/my-session")]
    public partial class SessionInfo
    {
    }

    public partial class TechnologyInfo
    {
        public virtual TechnologyTier Tier { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Name { get; set; }
        public virtual string LogoUrl { get; set; }
        public virtual int StacksCount { get; set; }
    }

    public partial class TechnologyInStack
        : TechnologyBase
    {
        public virtual long TechnologyId { get; set; }
        public virtual long TechnologyStackId { get; set; }
        public virtual string Justification { get; set; }
    }

    public partial class TechStackDetails
        : TechnologyStackBase
    {
        public TechStackDetails()
        {
            TechnologyChoices = new List<TechnologyInStack>{};
        }

        public virtual string DetailsHtml { get; set; }
        public virtual List<TechnologyInStack> TechnologyChoices { get; set; }
    }

    [Route("/technology/{Id}", "PUT")]
    public partial class UpdateTechnology
        : IReturn<UpdateTechnologyResponse>
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string VendorUrl { get; set; }
        public virtual string ProductUrl { get; set; }
        public virtual string LogoUrl { get; set; }
        public virtual string Description { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual TechnologyTier Tier { get; set; }
    }

    public partial class UpdateTechnologyResponse
    {
        public virtual Technology Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/techstacks/{Id}", "PUT")]
    public partial class UpdateTechnologyStack
        : IReturn<UpdateTechnologyStackResponse>
    {
        public UpdateTechnologyStack()
        {
            TechnologyIds = new List<long>{};
        }

        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string AppUrl { get; set; }
        public virtual string ScreenshotUrl { get; set; }
        public virtual string Description { get; set; }
        public virtual string Details { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual List<long> TechnologyIds { get; set; }
    }

    public partial class UpdateTechnologyStackResponse
    {
        public virtual TechStackDetails Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class UserInfo
    {
        public virtual string UserName { get; set; }
        public virtual string AvatarUrl { get; set; }
        public virtual int StacksCount { get; set; }
    }
}

namespace TechStacks.ServiceModel.Types
{

    public partial class Technology
        : TechnologyBase
    {
    }

    public partial class TechnologyBase
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string VendorUrl { get; set; }
        public virtual string ProductUrl { get; set; }
        public virtual string LogoUrl { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime Created { get; set; }
        public virtual string CreatedBy { get; set; }
        public virtual DateTime LastModified { get; set; }
        public virtual string LastModifiedBy { get; set; }
        public virtual string OwnerId { get; set; }
        public virtual string Slug { get; set; }
        public virtual bool LogoApproved { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual TechnologyTier Tier { get; set; }
        public virtual DateTime? LastStatusUpdate { get; set; }
    }

    public partial class TechnologyHistory
        : TechnologyBase
    {
        public virtual long TechnologyId { get; set; }
        public virtual string Operation { get; set; }
    }

    public partial class TechnologyStack
        : TechnologyStackBase
    {
    }

    public partial class TechnologyStackBase
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string VendorName { get; set; }
        public virtual string Description { get; set; }
        public virtual string AppUrl { get; set; }
        public virtual string ScreenshotUrl { get; set; }
        public virtual DateTime Created { get; set; }
        public virtual string CreatedBy { get; set; }
        public virtual DateTime LastModified { get; set; }
        public virtual string LastModifiedBy { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual string OwnerId { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Details { get; set; }
        public virtual DateTime? LastStatusUpdate { get; set; }
    }

    public partial class TechnologyStackHistory
        : TechnologyStackBase
    {
        public TechnologyStackHistory()
        {
            TechnologyIds = new List<long>{};
        }

        public virtual long TechnologyStackId { get; set; }
        public virtual string Operation { get; set; }
        public virtual List<long> TechnologyIds { get; set; }
    }

    public enum TechnologyTier
    {
        ProgrammingLanguage,
        Client,
        Http,
        Server,
        Data,
        SoftwareInfrastructure,
        OperatingSystem,
        HardwareInfrastructure,
        ThirdPartyServices,
    }
}

