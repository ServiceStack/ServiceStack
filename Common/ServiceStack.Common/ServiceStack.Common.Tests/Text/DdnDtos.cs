using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Common.Tests.Text
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

	[Serializable]
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

	[Serializable]
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

	[Serializable]
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

	[Serializable]
	[CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Post")]
	public class ArrayOfPost : List<Post>
	{
		public ArrayOfPost() { }
		public ArrayOfPost(IEnumerable<Post> collection) : base(collection) { }

		public static ArrayOfPost New(params Post[] ids) { return new ArrayOfPost(ids); }
	}

	[Serializable]
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

	[CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Id")]
	public class ArrayOfStringId : List<string>
	{
		public ArrayOfStringId() { }
		public ArrayOfStringId(IEnumerable<string> collection) : base(collection) { }

		//TODO: allow params[] constructor, fails on: o.TranslateTo<ArrayOfStringId>() 
		public static ArrayOfStringId New(params string[] ids) { return new ArrayOfStringId(ids); }
		//public ArrayOfStringId(params string[] ids) : base(ids) { }
	}

}