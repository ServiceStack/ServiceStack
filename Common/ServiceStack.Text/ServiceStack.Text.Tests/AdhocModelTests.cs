using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Platform.Text;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class AdhocModelTests
	{
		public enum FlowPostType
		{
			Content,
			Text,
			Promo,
		}

		[TextRecord]
		public class FlowPostTransient
		{
			public FlowPostTransient()
			{
				this.TrackUrns = new List<string>();
			}

			[TextField]
			public long Id { get; set; }
			[TextField]
			public string Urn { get; set; }
			[TextField]
			public Guid UserId { get; set; }
			[TextField]
			public DateTime DateAdded { get; set; }
			[TextField]
			public DateTime DateModified { get; set; }
			[TextField]
			public Guid? TargetUserId { get; set; }
			[TextField]
			public long? ForwardedPostId { get; set; }
			[TextField]
			public Guid OriginUserId { get; set; }
			[TextField]
			public string OriginUserName { get; set; }
			[TextField]
			public Guid SourceUserId { get; set; }
			[TextField]
			public string SourceUserName { get; set; }
			[TextField]
			public string SubjectUrn { get; set; }
			[TextField]
			public string ContentUrn { get; set; }
			[TextField]
			public IList<string> TrackUrns { get; set; }
			[TextField]
			public string Caption { get; set; }
			[TextField]
			public Guid CaptionUserId { get; set; }
			[TextField]
			public string CaptionSourceName { get; set; }
			[TextField]
			public string ForwardedPostUrn { get; set; }
			[TextField]
			public FlowPostType PostType { get; set; }
			[TextField]
			public Guid? OnBehalfOfUserId { get; set; }

			public static FlowPostTransient Create()
			{
				return new FlowPostTransient {
					Caption = "Caption",
					CaptionSourceName = "CaptionSourceName",
					CaptionUserId = Guid.NewGuid(),
					ContentUrn = "ContentUrn",
					DateAdded = DateTime.Now,
					DateModified = DateTime.Now,
					ForwardedPostId = 1,
					ForwardedPostUrn = "ForwardedPostUrn",
					Id = 1,
					OnBehalfOfUserId = Guid.NewGuid(),
					OriginUserId = Guid.NewGuid(),
					OriginUserName = "OriginUserName",
					PostType = FlowPostType.Content,
					SourceUserId = Guid.NewGuid(),
					SourceUserName = "SourceUserName",
					SubjectUrn = "SubjectUrn ",
					TargetUserId = Guid.NewGuid(),
					TrackUrns = new List<string> { "track1", "track2" },
					Urn = "Urn ",
					UserId = Guid.NewGuid(),
				};
			}
		}

		[Test]
		public void Can_Deserialize_text()
		{
			var dtoString = "[{Id:1,Urn:urn:post:3a944f18-920c-498a-832d-cf38fed3d0d7/1,UserId:3a944f18920c498a832dcf38fed3d0d7,DateAdded:2010-02-17T12:04:45.2845615Z,DateModified:2010-02-17T12:04:45.2845615Z,OriginUserId:3a944f18920c498a832dcf38fed3d0d7,OriginUserName:testuser1,SourceUserId:3a944f18920c498a832dcf38fed3d0d7,SourceUserName:testuser1,SubjectUrn:urn:track:1,ContentUrn:urn:track:1,TrackUrns:[],CaptionUserId:3a944f18920c498a832dcf38fed3d0d7,CaptionSourceName:testuser1,PostType:Content}]";
			var fromString = TypeSerializer.DeserializeFromString<List<FlowPostTransient>>(dtoString);
		}

		[Test]
		public void Can_Serialize_single_FlowPostTransient()
		{
			var dto = FlowPostTransient.Create();
			var dtoString = TypeSerializer.SerializeToString(dto);
			var fromString = TypeSerializer.DeserializeFromString<FlowPostTransient>(dtoString);
		}

		[Test]
		public void Can_Serialize_multiple_FlowPostTransient()
		{
			var dtos = new List<FlowPostTransient> {
				FlowPostTransient.Create(), 
				FlowPostTransient.Create()
			};
			var dtoString = TypeSerializer.SerializeToString(dtos);
			var fromString = TypeSerializer.DeserializeFromString<List<FlowPostTransient>>(dtoString);
		}
	}
}