using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Text
{
	[TestFixture]
	public class DdnDtoTests
	{
		const string userViewString =
			"{Id:f830c3fde66447fab09a7d06a9db4afd,Profile:{Id:f830c3fde66447fab09a7d06a9db4afd,UserType:Normal,UserName:yrstruely,FullName:Kerry Harris,Country:gb,LanguageCode:en,FlowPostCount:23,BuyCount:0,ClientTracksCount:0,ViewCount:29,FollowerUsers:[{Id:69593133bffb41869807756678207d87,UserType:Normal,UserName:mythz,FlowPostCount:0,ClientTracksCount:0,FollowingCount:1,FollowersCount:1,ViewCount:0,ActivationDate:0001-01-01}],FollowingUsers:[{Id:69593133bffb41869807756678207d87,UserType:Normal,UserName:mythz,FlowPostCount:0,ClientTracksCount:0,FollowingCount:1,FollowersCount:1,ViewCount:0,ActivationDate:0001-01-01}],UserFileTypes:[],AboutMe:},Posts:[{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/1,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/1,DateAdded:2010-02-18T12:10:29.804602Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:cf80f05ff3564676b3b66e2550bf4f3a,ContentUrn:urn:album:cf80f05ff3564676b3b66e2550bf4f3a,TrackUrns:[],Caption:woof,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/2,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/2,DateAdded:2010-02-18T12:12:58.358602Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:8806fa704e694ad7beedb31a06cd405e,ContentUrn:urn:track:8806fa704e694ad7beedb31a06cd405e,TrackUrns:[],Caption:sdgh,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/3,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/3,DateAdded:2010-02-18T12:28:56.089607Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:a246a69a1c5a4d0c8000a91519561eeb,ContentUrn:urn:album:a246a69a1c5a4d0c8000a91519561eeb,TrackUrns:[],Caption:killa!,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/4,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/4,DateAdded:2010-02-18T12:58:12.382604Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:da bomb,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/5,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/5,DateAdded:2010-02-18T12:58:18.651604Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:send it,CaptionUserId:69593133bffb41869807756678207d87,CaptionUserName:mythz,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/7,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/7,DateAdded:2010-02-18T16:28:24.642609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:a388179c38c64ae08e337d1d87018516,ContentUrn:urn:album:a388179c38c64ae08e337d1d87018516,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/8,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/8,DateAdded:2010-02-18T16:42:29.661609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:f9dede6b6fbc463ea66ffe885a7a9a85,ContentUrn:urn:track:f9dede6b6fbc463ea66ffe885a7a9a85,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/9,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/9,DateAdded:2010-02-18T16:44:54.476609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,ContentUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/10,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/10,DateAdded:2010-02-18T16:46:39.813609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,ContentUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/11,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/11,DateAdded:2010-02-18T16:49:09.140609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:855ebcb76a2b4915a7426028008a6b5a,ContentUrn:urn:album:855ebcb76a2b4915a7426028008a6b5a,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/12,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/12,DateAdded:2010-02-18T17:15:50.970609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:5cf1a7287dc74487b07f4c3af87a657b,ContentUrn:urn:track:5cf1a7287dc74487b07f4c3af87a657b,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/13,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/13,DateAdded:2010-02-18T17:16:39.922609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:118eef26eaad48cebe44a834a5ebafc5,ContentUrn:urn:track:118eef26eaad48cebe44a834a5ebafc5,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/14,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/14,DateAdded:2010-02-18T17:23:35.473609Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:send it,CaptionUserId:69593133bffb41869807756678207d87,CaptionUserName:mythz,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/15,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/15,DateAdded:2010-02-18T22:12:35.205617Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:d76ec030a6234c96b00d9e9ae2493472,ContentUrn:urn:track:d76ec030a6234c96b00d9e9ae2493472,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/16,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/16,DateAdded:2010-02-18T22:16:13.85053Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:69f01aed61b548c7ace2df0955fa234d,ContentUrn:urn:album:69f01aed61b548c7ace2df0955fa234d,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/17,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/17,DateAdded:2010-02-18T22:16:18.566059Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:9dea76670a994bc78922694a05bf516c,ContentUrn:urn:track:9dea76670a994bc78922694a05bf516c,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/18,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/18,DateAdded:2010-02-18T22:16:21.55376Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0e70ebf49de8418f9ef1fba91318476c,ContentUrn:urn:track:0e70ebf49de8418f9ef1fba91318476c,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/19,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/19,DateAdded:2010-02-18T22:17:31.905724Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:206bfb37082a4e859e383234078fedc6,ContentUrn:urn:album:206bfb37082a4e859e383234078fedc6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/20,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/20,DateAdded:2010-02-18T22:17:41.520762Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:15fe23d6ba924316b5384625be2585f4,ContentUrn:urn:album:15fe23d6ba924316b5384625be2585f4,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/21,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/21,DateAdded:2010-02-18T22:21:29.706619Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:f479af815a204932b41c5de6beeffdf9,ContentUrn:urn:track:f479af815a204932b41c5de6beeffdf9,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content}]}";

		[Test]
		public void Can_serializer_UserPublicView()
		{
			var dto = TypeSerializer.DeserializeFromString<UserPublicView>(userViewString);
			var dtoString = TypeSerializer.SerializeToString(dto);
		}

		[Test]
		public void Can_serializer_RecentPostsText()
		{
			var recentPostsText = "[{Id:534,Urn:urn:post:c12c7474-3e94-42fb-99b2-dbbfd8cd3254/534,UserId:c12c74743e9442fb99b2dbbfd8cd3254,DateAdded:2010-03-03T11:34:01.444616Z,DateModified:2010-03-03T11:34:01.444616Z,OriginUserId:c12c74743e9442fb99b2dbbfd8cd3254,OriginUserName:clash,SourceUserId:c12c74743e9442fb99b2dbbfd8cd3254,SourceUserName:clash,SubjectUrn:urn:album:16e13bd0b27a4ea58a2a49a897cc3b46,ContentUrn:urn:album:16e13bd0b27a4ea58a2a49a897cc3b46,TrackUrns:[],Caption:,CaptionUserId:c12c74743e9442fb99b2dbbfd8cd3254,CaptionSourceName:clash,PostType:Content}]";
			recentPostsText = "[{Id:534,Urn:urn:post:f3ee46c2-d134-4911-add8-f85c6ae511c9/534,UserId:f3ee46c2d1344911add8f85c6ae511c9,DateAdded:2010-03-03T11:27:38.344297Z,DateModified:2010-03-03T11:27:38.344297Z,OriginUserId:f3ee46c2d1344911add8f85c6ae511c9,OriginUserName:kissmekate,SourceUserId:f3ee46c2d1344911add8f85c6ae511c9,SourceUserName:kissmekate,SubjectUrn:urn:track:42745e4b6622437297f54aeba0871a2e,ContentUrn:urn:track:42745e4b6622437297f54aeba0871a2e,TrackUrns:[],Caption:Quite a nice cover of this classic minnie ripperton track ,CaptionUserId:f3ee46c2d1344911add8f85c6ae511c9,CaptionSourceName:kissmekate,PostType:Content},{Id:533,Urn:urn:post:f3ee46c2-d134-4911-add8-f85c6ae511c9/533,UserId:f3ee46c2d1344911add8f85c6ae511c9,DateAdded:2010-03-03T11:26:39.251393Z,DateModified:2010-03-03T11:26:39.251393Z,OriginUserId:f3ee46c2d1344911add8f85c6ae511c9,OriginUserName:kissmekate,SourceUserId:f3ee46c2d1344911add8f85c6ae511c9,SourceUserName:kissmekate,SubjectUrn:urn:track:66f873948daa4e21aa6bf099666220dd,ContentUrn:urn:track:66f873948daa4e21aa6bf099666220dd,TrackUrns:[],Caption:\"I seem to be in reminiscent mode today - i remember being 14 and singing along to this. Also, my first gig at Brixton Academy... \",CaptionUserId:f3ee46c2d1344911add8f85c6ae511c9,CaptionSourceName:kissmekate,PostType:Content},{Id:531,Urn:urn:post:f3ee46c2-d134-4911-add8-f85c6ae511c9/531,UserId:f3ee46c2d1344911add8f85c6ae511c9,DateAdded:2010-03-03T10:25:14.207767Z,DateModified:2010-03-03T10:25:14.207767Z,OriginUserId:f3ee46c2d1344911add8f85c6ae511c9,OriginUserName:kissmekate,SourceUserId:f3ee46c2d1344911add8f85c6ae511c9,SourceUserName:kissmekate,SubjectUrn:urn:track:07eb6e6effc24c15aceb9dd1477bdc3d,ContentUrn:urn:track:07eb6e6effc24c15aceb9dd1477bdc3d,TrackUrns:[],Caption:Memories of Uni... ,CaptionUserId:f3ee46c2d1344911add8f85c6ae511c9,CaptionSourceName:kissmekate,PostType:Content},{Id:528,Urn:urn:post:f3ee46c2-d134-4911-add8-f85c6ae511c9/528,UserId:f3ee46c2d1344911add8f85c6ae511c9,DateAdded:2010-03-03T10:22:22.84662Z,DateModified:2010-03-03T10:22:22.84662Z,OriginUserId:f3ee46c2d1344911add8f85c6ae511c9,OriginUserName:kissmekate,SourceUserId:f3ee46c2d1344911add8f85c6ae511c9,SourceUserName:kissmekate,SubjectUrn:urn:album:73d21e69b8cd45dcac8310707f037157,ContentUrn:urn:album:73d21e69b8cd45dcac8310707f037157,TrackUrns:[],Caption:Probably my fave album - perfect for chilling... ,CaptionUserId:f3ee46c2d1344911add8f85c6ae511c9,CaptionSourceName:kissmekate,PostType:Content},{Id:477,Urn:urn:post:f3ee46c2-d134-4911-add8-f85c6ae511c9/477,UserId:f3ee46c2d1344911add8f85c6ae511c9,DateAdded:2010-02-15T17:23:42.748422Z,DateModified:2010-02-15T17:23:42.748422Z,OriginUserId:f3ee46c2d1344911add8f85c6ae511c9,OriginUserName:kissmekate,SourceUserId:f3ee46c2d1344911add8f85c6ae511c9,SourceUserName:kissmekate,SubjectUrn:urn:track:88626491c71b4e7388f33a147d3d8345,ContentUrn:urn:track:88626491c71b4e7388f33a147d3d8345,TrackUrns:[],Caption:One of my fave Elbow songs... ,CaptionUserId:f3ee46c2d1344911add8f85c6ae511c9,CaptionSourceName:kissmekate,PostType:Content}]";
			var dto = TypeSerializer.DeserializeFromString<List<FlowPostTransient>>(recentPostsText);
			var dtoString = TypeSerializer.SerializeToString(dto);
		}

		[Test]
		public void Can_serialize_ResponseStats()
		{
			var dto = new ResponseStatus {
				ErrorCode = null
			};

			var dtoString = TypeSerializer.SerializeToString(dto);

			Assert.That(dtoString, Is.EqualTo("{Errors:[],IsSuccess:True}"));

			Console.WriteLine(dtoString);
		}

		[Test]
		public void Can_serialize_GetContentStatsResponse()
		{
			var dto = new GetContentStatsResponse {
				CreatedDate = DateTime.UtcNow,
				TopRecommenders = new List<UserSearchResult>
          		{
          			CreateUserSearchResult(1),
          			CreateUserSearchResult(2),
          		},
				LatestPosts = new List<Post> {
             		CreatePost(1),
             		CreatePost(2)
				 },
			};
			var dtoString = TypeSerializer.SerializeToString(dto);

			var fromDto = TypeSerializer.DeserializeFromString<GetContentStatsResponse>(dtoString);

			Assert.That(fromDto, Is.Not.Null);
		}

		public UserSearchResult CreateUserSearchResult(int i)
		{
			return new UserSearchResult {
				ActivationDate = DateTime.UtcNow,
				ClientTracksCount = i,
				FirstName = "FileName" + i,
				FlowPostCount = i,
				FollowersCount = i,
				FollowingCount = i,
				FullName = "FullName" + i,
				Id = Guid.NewGuid(),
				LanguageCode = "en",
				LastName = "LastName" + i,
				UserName = "UserName" + i,
				UserType = "UserType" + i,
				ViewCount = i,
			};
		}

		public Post CreatePost(int i)
		{
			return new Post {
				CanPreviewFullLength = false,
				Caption = "Caption" + i,
				CaptionUserId = Guid.NewGuid(),
				CaptionUserName = "CaptionUserName" + i,
				ContentUrn = "ContentUrn",
				DateAdded = DateTime.UtcNow,
				OnBehalfOfUserId = Guid.NewGuid(),
				OriginUserId = Guid.NewGuid(),
				OriginUserName = "OriginUserName" + i,
				PostType = "PostType" + i,
				SourceUserId = Guid.NewGuid(),
				SourceUserName = "SourceUserName",
				SubjectUrn = "SubjectUrn",
				TrackUrns = new ArrayOfStringId(new[] { "track" + i, }),
				Urn = "Urn" + i,
			};
		}
	}

}