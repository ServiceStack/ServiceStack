using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.Text.Tests.Support
{
    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class UserPublicView
    {
        /// <summary>
        /// I'm naming this 'Id' instead of 'UserId' as this is dto is 
        /// meant to be cached and we may want to handle all caches generically at some point.
        /// </summary>
        /// <value>The id.</value>
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public UserPublicProfile Profile { get; set; }

        [DataMember]
        public ArrayOfPost Posts { get; set; }
    }

#if !NETCORE
    [Serializable]
#endif
    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class UserPublicProfile
    {
        public UserPublicProfile()
        {
            this.FollowerUsers = new List<UserSearchResult>();
            this.FollowingUsers = new List<UserSearchResult>();
            this.UserFileTypes = new ArrayOfString();
        }

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public string UserType
        {
            get;
            set;
        }

        [DataMember]
        public string UserName
        {
            get;
            set;
        }

        [DataMember]
        public string FullName
        {
            get;
            set;
        }

        [DataMember]
        public string Country
        {
            get;
            set;
        }

        [DataMember]
        public string LanguageCode
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? DateOfBirth
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? LastLoginDate
        {
            get;
            set;
        }

        [DataMember]
        public long FlowPostCount
        {
            get;
            set;
        }

        [DataMember]
        public int BuyCount
        {
            get;
            set;
        }

        [DataMember]
        public int ClientTracksCount
        {
            get;
            set;
        }

        [DataMember]
        public int ViewCount
        {
            get;
            set;
        }

        [DataMember]
        public List<UserSearchResult> FollowerUsers
        {
            get;
            set;
        }

        [DataMember]
        public List<UserSearchResult> FollowingUsers
        {
            get;
            set;
        }

        ///ArrayOfString causes translation error
        [DataMember]
        public ArrayOfString UserFileTypes
        {
            get;
            set;
        }

        [DataMember]
        public string OriginalProfileBase64Hash
        {
            get;
            set;
        }

        [DataMember]
        public string AboutMe
        {
            get;
            set;
        }
    }

#if !NETCORE
    [Serializable]
#endif
    [CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "String")]
    public class ArrayOfString : List<string>
    {
        public ArrayOfString() { }
        public ArrayOfString(IEnumerable<string> collection) : base(collection) { }

        //TODO: allow params[] constructor, fails on: 
        //Profile = user.TranslateTo<UserPrivateProfile>()
        public static ArrayOfString New(params string[] ids) { return new ArrayOfString(ids); }
        //public ArrayOfString(params string[] ids) : base(ids) { }
    }

#if !NETCORE
    [Serializable]
#endif
    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class UserSearchResult
        : IHasId<Guid>
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string UserType { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FullName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FirstName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string LastName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string LanguageCode { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int FlowPostCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int ClientTracksCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int FollowingCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int FollowersCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int ViewCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime ActivationDate { get; set; }
    }

#if !NETCORE
    [Serializable]
#endif
    [CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Post")]
    public class ArrayOfPost : List<Post>
    {
        public ArrayOfPost() { }
        public ArrayOfPost(IEnumerable<Post> collection) : base(collection) { }

        public static ArrayOfPost New(params Post[] ids) { return new ArrayOfPost(ids); }
    }

#if !NETCORE
    [Serializable]
#endif
    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class Post
        : IHasStringId
    {
        public Post()
        {
            this.TrackUrns = new ArrayOfStringId();
        }

        public string Id
        {
            get { return this.Urn; }
        }

        [DataMember]
        public string Urn
        {
            get;
            set;
        }

        [DataMember]
        public DateTime DateAdded
        {
            get;
            set;
        }

        [DataMember]
        public bool CanPreviewFullLength
        {
            get;
            set;
        }

        [DataMember]
        public Guid OriginUserId
        {
            get;
            set;
        }

        [DataMember]
        public string OriginUserName
        {
            get;
            set;
        }

        [DataMember]
        public Guid SourceUserId
        {
            get;
            set;
        }

        [DataMember]
        public string SourceUserName
        {
            get;
            set;
        }

        [DataMember]
        public string SubjectUrn
        {
            get;
            set;
        }

        [DataMember]
        public string ContentUrn
        {
            get;
            set;
        }

        [DataMember]
        public ArrayOfStringId TrackUrns
        {
            get;
            set;
        }

        [DataMember]
        public string Caption
        {
            get;
            set;
        }

        [DataMember]
        public Guid CaptionUserId
        {
            get;
            set;
        }

        [DataMember]
        public string CaptionUserName
        {
            get;
            set;
        }

        [DataMember]
        public string PostType
        {
            get;
            set;
        }

        [DataMember]
        public Guid? OnBehalfOfUserId
        {
            get;
            set;
        }
    }

    public enum FlowPostType
    {
        Content,
        Text,
        Promo,
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class Property
    {
        public Property()
        {
        }

        public Property(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.Name + "," + this.Value;
        }
    }

    [CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Property")]
    public class Properties
        : List<Property>
    {
        public Properties()
        {
        }

        public Properties(IEnumerable<Property> collection)
            : base(collection)
        {
        }

        public string GetPropertyValue(string name)
        {
            foreach (var property in this)
            {
                if (string.CompareOrdinal(property.Name, name) == 0)
                {
                    return property.Value;
                }
            }

            return null;
        }

        public Dictionary<string, string> ToDictionary()
        {
            var propertyDict = new Dictionary<string, string>();

            foreach (var property in this)
            {
                propertyDict[property.Name] = property.Value;
            }

            return propertyDict;
        }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class ResponseStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseStatus"/> class.
        /// 
        /// A response status without an errorcode == success
        /// </summary>
        public ResponseStatus()
        {
            this.Errors = new List<ResponseError>();
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string ErrorCode { get; set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string StackTrace { get; set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public List<ResponseError> Errors { get; set; }


        public bool IsSuccess
        {
            get { return this.ErrorCode == null; }
        }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class ResponseError
    {
        [DataMember]
        public string ErrorCode { get; set; }
        [DataMember]
        public string FieldName { get; set; }
        [DataMember]
        public string Message { get; set; }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class GetContentStatsResponse
#if !NETCORE
        : IExtensibleDataObject
#endif
    {
        public GetContentStatsResponse()
        {
            this.Version = 100;
            this.ResponseStatus = new ResponseStatus();

            this.TopRecommenders = new List<UserSearchResult>();
            this.LatestPosts = new List<Post>();
        }

        [DataMember]
        public DateTime CreatedDate { get; set; }

        [DataMember]
        public List<UserSearchResult> TopRecommenders { get; set; }

        [DataMember]
        public List<Post> LatestPosts { get; set; }

        #region Standard Response Properties

        [DataMember]
        public int Version
        {
            get;
            set;
        }

        [DataMember]
        public Properties Properties
        {
            get;
            set;
        }
#if !NETCORE
        public ExtensionDataObject ExtensionData
        {
            get;
            set;
        }
#endif
        [DataMember]
        public ResponseStatus ResponseStatus
        {
            get;
            set;
        }

        #endregion
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class ProUserPublicProfile
    {
        public ProUserPublicProfile()
        {
            this.SocialLinks = new List<SocialLinkUrl>();

            this.ArtistImages = new List<ImageAsset>();
            this.Genres = new List<string>();

            this.Posts = new ArrayOfPost();
            this.FollowerUsers = new List<UserSearchResult>();
            this.FollowingUsers = new List<UserSearchResult>();
        }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Alias { get; set; }

        [DataMember]
        public string RefUrn { get; set; }

        [DataMember]
        public string ProUserType { get; set; }

        [DataMember]
        public string ProUserSalesType { get; set; }

        #region Header

        [DataMember]
        public TextLink ProUserLink { get; set; }

        /// <summary>
        /// Same as above but in an [A] HTML link
        /// </summary>
        [DataMember]
        public string ProUserLinkHtml { get; set; }

        /// <summary>
        /// For the twitter and facebook icons
        /// </summary>
        [DataMember]
        public List<SocialLinkUrl> SocialLinks { get; set; }

        #endregion

        #region Theme
        [DataMember]
        public ImageAsset BannerImage { get; set; }

        [DataMember]
        public string BannerImageBackgroundColor { get; set; }

        [DataMember]
        public List<string> UserFileTypes { get; set; }

        [DataMember]
        public string OriginalProfileBase64Hash { get; set; }
        #endregion

        #region Music

        [DataMember]
        public List<ImageAsset> ArtistImages { get; set; }

        [DataMember]
        public List<string> Genres { get; set; }

        #endregion


        #region Biography

        [DataMember]
        public string BiographyPageHtml { get; set; }

        #endregion


        #region Outbox

        [DataMember]
        public ArrayOfPost Posts { get; set; }

        [DataMember]
        public List<UserSearchResult> FollowerUsers { get; set; }

        [DataMember]
        public int FollowerUsersCount { get; set; }

        [DataMember]
        public List<UserSearchResult> FollowingUsers { get; set; }

        [DataMember]
        public int FollowingUsersCount { get; set; }

        #endregion

    }

    public enum SocialLink
    {
        iTunes = 0,
        Bebo = 1,
        Blogger = 2,
        Delicious = 3,
        Digg = 4,
        Email = 5,
        EverNote = 6,
        Facebook = 7,
        Flickr = 8,
        FriendFeed = 9,
        GoogleWave = 10,
        GroveShark = 11,
        iLike = 12,
        LastFm = 13,
        Mix = 14,
        MySpace = 15,
        Posterous = 16,
        Reddit = 17,
        Rss = 18,
        StumbleUpon = 19,
        Twitter = 20,
        Vimeo = 21,
        Wikipedia = 22,
        WordPress = 23,
        Yahoo = 24,
        YahooBuzz = 25,
        YouTube = 26,
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class SocialLinkUrl
    {
        [References(typeof(SocialLink))]
        [DataMember(EmitDefaultValue = false)]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string LinkUrl
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
#if !NETCORE
    [Serializable]
#endif
    public class ImageAsset
    {
        [DataMember(EmitDefaultValue = false)]
        public string RelativePath { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AbsoluteUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Hash { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public long? SizeBytes { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Width { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Height { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string BackgroundColorHex { get; set; }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class TextLink
    {
        [DataMember(EmitDefaultValue = false)]
        public string Label
        {
            get;
            set;
        }

        [DataMember]
        public string LinkUrl
        {
            get;
            set;
        }
    }
}