using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DdnDtoTests
         : TestBase
    {
        const string UserViewString =
            "{Id:f830c3fde66447fab09a7d06a9db4afd,Profile:{Id:f830c3fde66447fab09a7d06a9db4afd,UserType:Normal,UserName:yrstruely,FullName:Kerry Harris,Country:gb,LanguageCode:en,FlowPostCount:23,BuyCount:0,ClientTracksCount:0,ViewCount:29,FollowerUsers:[{Id:69593133bffb41869807756678207d87,UserType:Normal,UserName:mythz,FlowPostCount:0,ClientTracksCount:0,FollowingCount:1,FollowersCount:1,ViewCount:0,ActivationDate:0001-01-01}],FollowingUsers:[{Id:69593133bffb41869807756678207d87,UserType:Normal,UserName:mythz,FlowPostCount:0,ClientTracksCount:0,FollowingCount:1,FollowersCount:1,ViewCount:0,ActivationDate:0001-01-01}],UserFileTypes:[],AboutMe:},Posts:[{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/1,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/1,DateAdded:2010-02-18T12:10:29.804602Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:cf80f05ff3564676b3b66e2550bf4f3a,ContentUrn:urn:album:cf80f05ff3564676b3b66e2550bf4f3a,TrackUrns:[],Caption:woof,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/2,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/2,DateAdded:2010-02-18T12:12:58.358602Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:8806fa704e694ad7beedb31a06cd405e,ContentUrn:urn:track:8806fa704e694ad7beedb31a06cd405e,TrackUrns:[],Caption:sdgh,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/3,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/3,DateAdded:2010-02-18T12:28:56.089607Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:a246a69a1c5a4d0c8000a91519561eeb,ContentUrn:urn:album:a246a69a1c5a4d0c8000a91519561eeb,TrackUrns:[],Caption:killa!,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/4,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/4,DateAdded:2010-02-18T12:58:12.382604Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:da bomb,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/5,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/5,DateAdded:2010-02-18T12:58:18.651604Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:send it,CaptionUserId:69593133bffb41869807756678207d87,CaptionUserName:mythz,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/7,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/7,DateAdded:2010-02-18T16:28:24.642609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:a388179c38c64ae08e337d1d87018516,ContentUrn:urn:album:a388179c38c64ae08e337d1d87018516,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/8,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/8,DateAdded:2010-02-18T16:42:29.661609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:f9dede6b6fbc463ea66ffe885a7a9a85,ContentUrn:urn:track:f9dede6b6fbc463ea66ffe885a7a9a85,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/9,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/9,DateAdded:2010-02-18T16:44:54.476609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,ContentUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/10,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/10,DateAdded:2010-02-18T16:46:39.813609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,ContentUrn:urn:track:0512382ee29f48b68eb7800f0be09eb6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/11,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/11,DateAdded:2010-02-18T16:49:09.140609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:855ebcb76a2b4915a7426028008a6b5a,ContentUrn:urn:album:855ebcb76a2b4915a7426028008a6b5a,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/12,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/12,DateAdded:2010-02-18T17:15:50.970609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:5cf1a7287dc74487b07f4c3af87a657b,ContentUrn:urn:track:5cf1a7287dc74487b07f4c3af87a657b,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/13,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/13,DateAdded:2010-02-18T17:16:39.922609Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:118eef26eaad48cebe44a834a5ebafc5,ContentUrn:urn:track:118eef26eaad48cebe44a834a5ebafc5,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/14,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/14,DateAdded:2010-02-18T17:23:35.473609Z,CanPreviewFullLength:False,OriginUserId:69593133bffb41869807756678207d87,OriginUserName:mythz,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,ContentUrn:urn:album:1dc1f6b2e81e4bc19fb3a8438468fa59,TrackUrns:[],Caption:send it,CaptionUserId:69593133bffb41869807756678207d87,CaptionUserName:mythz,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/15,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/15,DateAdded:2010-02-18T22:12:35.205617Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:d76ec030a6234c96b00d9e9ae2493472,ContentUrn:urn:track:d76ec030a6234c96b00d9e9ae2493472,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/16,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/16,DateAdded:2010-02-18T22:16:13.85053Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:69f01aed61b548c7ace2df0955fa234d,ContentUrn:urn:album:69f01aed61b548c7ace2df0955fa234d,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/17,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/17,DateAdded:2010-02-18T22:16:18.566059Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:9dea76670a994bc78922694a05bf516c,ContentUrn:urn:track:9dea76670a994bc78922694a05bf516c,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/18,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/18,DateAdded:2010-02-18T22:16:21.55376Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:0e70ebf49de8418f9ef1fba91318476c,ContentUrn:urn:track:0e70ebf49de8418f9ef1fba91318476c,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/19,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/19,DateAdded:2010-02-18T22:17:31.905724Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:206bfb37082a4e859e383234078fedc6,ContentUrn:urn:album:206bfb37082a4e859e383234078fedc6,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/20,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/20,DateAdded:2010-02-18T22:17:41.520762Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:album:15fe23d6ba924316b5384625be2585f4,ContentUrn:urn:album:15fe23d6ba924316b5384625be2585f4,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content},{Id:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/21,Urn:urn:post:f830c3fd-e664-47fa-b09a-7d06a9db4afd/21,DateAdded:2010-02-18T22:21:29.706619Z,CanPreviewFullLength:False,OriginUserId:f830c3fde66447fab09a7d06a9db4afd,OriginUserName:yrstruely,SourceUserId:f830c3fde66447fab09a7d06a9db4afd,SourceUserName:yrstruely,SubjectUrn:urn:track:f479af815a204932b41c5de6beeffdf9,ContentUrn:urn:track:f479af815a204932b41c5de6beeffdf9,TrackUrns:[],Caption:,CaptionUserId:f830c3fde66447fab09a7d06a9db4afd,CaptionUserName:yrstruely,PostType:Content}]}";

        [Test]
        public void Can_serializer_UserPublicView()
        {
            var dto = TypeSerializer.DeserializeFromString<UserPublicView>(UserViewString);
            Serialize(dto);
        }

        [Test]
        public void Can_serialize_ResponseStats()
        {
            var dto = new ResponseStatus
            {
                ErrorCode = null
            };

            var dtoString = TypeSerializer.SerializeToString(dto);

            Assert.That(dtoString, Is.EqualTo("{}"));

            Console.WriteLine(dtoString);
        }

        [Test]
        public void Can_serialize_GetContentStatsResponse()
        {
            var dto = new GetContentStatsResponse
            {
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

            Serialize(dto);
        }

        [Test]
        public void Can_serialize_ProUserPublicProfile()
        {
            var dtoString =
                @"{Id:81ae7b3ae5404f5f827d0303949fcb2f,Alias:Mike Halliday,ProUserType:Celebrity,ProUserSalesType:OthersMusic,ProUserLink:{},ProUserLinkHtml:""<a href=""""""""></a>"",SocialLinks:[],BannerImageBackgroundColor:#000000,ArtistImages:[],Genres:[],BiographyPageHtml:""<style type=""""text/css"""">
#prouser-biography H1
{
        font: normal 20px arial;
        color: #fff;
}
#prouser-biography STRONG
,#prouser-biography B
,#prouser-biography A
{
        color: #fff;
        font-weight: bold;
        text-decoration: none;
}
</style>
<table id=""""prouser-biography"""" cellspacing=""""0"""" cellpadding=""""0"""" width=""""860""""
       style=""""font: 12px arial; color: #cdcdcd; margin-top: 10px;"""">
<tr>
    <td id=""""prouser-bio-content"""" style=""""vertical-align: top;"""">
        
        <table id=""""prouser-bio-content-table"""" cellpadding=""""10"""">
            <tr>
                <td id=""""prouser-bio-html""""></td>
            </tr>
        </table>
    </td>
    <td id=""""prouser-bio-info"""" width=""""220"""" style=""""vertical-align: top;"""">
        
        <table id=""""prouser-bio-info-table"""" style=""""background:#475762;"""" cellspacing=""""1"""" cellpadding=""""0"""">
            <tr>
                <td style=""""background:#192E3B;"""">
                    <table cellpadding=""""5"""">
                        <tr>
                            <td id=""""prouser-bio-info-html""""></td>
                        </tr>
                    </table>
                </td>
            </tr>
            
        </table>
        
    </td>
</tr>
</table>"",Posts:[],FollowerUsers:[],FollowerUsersCount:0,FollowingUsers:[{Id:89b82c7cb6b042a6b1e8ab80cdb6b387,UserType:Mflow,UserName:mflow,LanguageCode:en,FlowPostCount:1372,ClientTracksCount:0,FollowingCount:0,FollowersCount:8457,ViewCount:1627,ActivationDate:2009-11-05T20:52:11.6156Z,UserImage:{RelativePath:89/b8/89b82c7cb6b042a6b1e8ab80cdb6b387/Profile75X75.jpg,Hash:GoLberqSAvzBc7L1298Ekw==,Width:75,Height:75}},{Id:b7c07996891941b399444733fd32810c,UserType:Channel,UserName:mflowalternative,FullName:mflow channel,FirstName:mflow,LastName:channel,LanguageCode:en,FlowPostCount:419,ClientTracksCount:0,FollowingCount:6,FollowersCount:6944,ViewCount:2167,ActivationDate:2009-11-05T20:52:11.7248Z,UserImage:{RelativePath:b7/c0/b7c07996891941b399444733fd32810c/Profile75X75.jpg,Hash:2N9IkhxXWV3TBdzmkI9tKA==,Width:75,Height:75}},{Id:7039f393fc8d45479c11636d90979adc,UserType:Channel,UserName:mflowfrontline,FullName:mflow channel,FirstName:mflow,LastName:channel,LanguageCode:en,FlowPostCount:115,ClientTracksCount:0,FollowingCount:5,FollowersCount:6883,ViewCount:559,ActivationDate:2009-11-05T20:52:11.834Z,UserImage:{RelativePath:70/39/7039f393fc8d45479c11636d90979adc/Profile75X75.jpg,Hash:LQq2I3aykfHW4OsoMQM8Jw==,Width:75,Height:75}},{Id:a2ba8e58e4494ee388499abe506abb06,UserType:ProUser,UserName:mojo,FullName:mojo,LanguageCode:en,FlowPostCount:0,ClientTracksCount:0,FollowingCount:0,FollowersCount:157,ViewCount:9,ActivationDate:2010-03-05T15:26:20.819008Z,UserImage:{RelativePath:a2/ba/a2ba8e58e4494ee388499abe506abb06/Profile75X75.jpg,Hash:TZ7Y5gs3fnbxT4ZtC8zkGg==,Width:75,Height:75}},{Id:106c3757c6424007a45bc00708c572bc,UserType:ProUser,UserName:islandrecords,FullName:Island Records,FirstName:Island,LastName:Records,LanguageCode:en,FlowPostCount:1,ClientTracksCount:0,FollowingCount:0,FollowersCount:147,ViewCount:60,ActivationDate:2010-03-16T19:50:46.691695Z,UserImage:{RelativePath:10/6c/106c3757c6424007a45bc00708c572bc/Profile75X75.jpg,Hash:46bc2QtzsWPlonljLaaPng==,Width:75,Height:75}}],FollowingUsersCount:5}";

            var dto = TypeSerializer.DeserializeFromString<ProUserPublicProfile>(dtoString);
            Serialize(dto);
        }

        public UserSearchResult CreateUserSearchResult(int i)
        {
            return new UserSearchResult
            {
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
            return new Post
            {
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