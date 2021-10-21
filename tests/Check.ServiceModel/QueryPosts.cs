using System;
using System.Collections.Generic;
using Check.ServiceModel.Types;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    [Route("/posts", "GET")]
    public partial class QueryPosts
        : QueryDb<Post>, IReturn<QueryResponse<Post>>, IGet
    {
        public QueryPosts()
        {
            Ids = new int[]{};
            OrganizationIds = new List<int>{};
            Types = new HashSet<string>{};
            AnyTechnologyIds = new HashSet<int>{};
            Is = new string[]{};
        }

        public virtual int[] Ids { get; set; }
        public virtual int? OrganizationId { get; set; }
        public virtual List<int> OrganizationIds { get; set; }
        public virtual HashSet<string> Types { get; set; }
        public virtual HashSet<int> AnyTechnologyIds { get; set; }
        public virtual string[] Is { get; set; }
    }

    public partial class Post
    {
        public Post()
        {
            TechnologyIds = new int[]{};
            Labels = new string[]{};
            RefUserIds = new int[]{};
            RefLinks = new string[]{};
            MuteUserIds = new int[]{};
        }

        public virtual long Id { get; set; }
        public virtual int OrganizationId { get; set; }
        public virtual int UserId { get; set; }
        public virtual PostType Type { get; set; }
        public virtual int CategoryId { get; set; }
        public virtual string Title { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Url { get; set; }
        public virtual string ImageUrl { get; set; }
        [StringLength(int.MaxValue)]
        public virtual string Content { get; set; }

        [StringLength(int.MaxValue)]
        public virtual string ContentHtml { get; set; }

        public virtual long? PinCommentId { get; set; }
        public virtual int[] TechnologyIds { get; set; }
        public virtual DateTime? FromDate { get; set; }
        public virtual DateTime? ToDate { get; set; }
        public virtual string Location { get; set; }
        public virtual string MetaType { get; set; }
        public virtual string Meta { get; set; }
        public virtual bool Approved { get; set; }
        public virtual long UpVotes { get; set; }
        public virtual long DownVotes { get; set; }
        public virtual long Points { get; set; }
        public virtual long Views { get; set; }
        public virtual long Favorites { get; set; }
        public virtual int Subscribers { get; set; }
        public virtual int ReplyCount { get; set; }
        public virtual int CommentsCount { get; set; }
        public virtual int WordCount { get; set; }
        public virtual int ReportCount { get; set; }
        public virtual int LinksCount { get; set; }
        public virtual int LinkedToCount { get; set; }
        public virtual int Score { get; set; }
        public virtual int Rank { get; set; }
        public virtual string[] Labels { get; set; }
        public virtual int[] RefUserIds { get; set; }
        public virtual string[] RefLinks { get; set; }
        public virtual int[] MuteUserIds { get; set; }
        public virtual DateTime? LastCommentDate { get; set; }
        public virtual long? LastCommentId { get; set; }
        public virtual int? LastCommentUserId { get; set; }
        public virtual DateTime? Deleted { get; set; }
        public virtual string DeletedBy { get; set; }
        public virtual DateTime? Locked { get; set; }
        public virtual string LockedBy { get; set; }
        public virtual DateTime? Hidden { get; set; }
        public virtual string HiddenBy { get; set; }
        public virtual string Status { get; set; }
        public virtual DateTime? StatusDate { get; set; }
        public virtual string StatusBy { get; set; }
        public virtual bool Archived { get; set; }
        public virtual DateTime? Bumped { get; set; }
        public virtual DateTime Created { get; set; }
        public virtual string CreatedBy { get; set; }
        public virtual DateTime Modified { get; set; }
        public virtual string ModifiedBy { get; set; }
        public virtual long? RefId { get; set; }
        public virtual string RefSource { get; set; }
        public virtual string RefUrn { get; set; }
    }

    public enum PostType
    {
        Announcement,
        Post,
        Showcase,
        Question,
        Request,
    }
    
    [Route("/echo/types")]
    public partial class EchoTypes
        : IReturn<EchoTypes>
    {
        public virtual byte Byte { get; set; }
        public virtual short Short { get; set; }
        public virtual int Int { get; set; }
        public virtual long Long { get; set; }
        public virtual ushort UShort { get; set; }
        public virtual uint UInt { get; set; }
        public virtual ulong ULong { get; set; }
        public virtual float Float { get; set; }
        public virtual double Double { get; set; }
        public virtual decimal Decimal { get; set; }
        public virtual string String { get; set; }
        public virtual DateTime DateTime { get; set; }
        public virtual TimeSpan TimeSpan { get; set; }
        public virtual DateTimeOffset DateTimeOffset { get; set; }
        public virtual Guid Guid { get; set; }
        public virtual Char Char { get; set; }
    }
    
}