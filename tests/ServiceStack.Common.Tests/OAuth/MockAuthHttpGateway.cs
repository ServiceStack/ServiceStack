#if !NETCORE_SUPPORT
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.OAuth
{
    public class MockAuthHttpGateway : IAuthHttpGateway
    {
        static MockAuthHttpGateway()
        {
            Tokens = new AuthTokens
            {
                UserId = "623501766",
                DisplayName = "Demis Bellot",
                FirstName = "Demis",
                LastName = "Bellot",
                Email = "demis.bellot@gmail.com",
            };
        }

        public static IAuthTokens Tokens { get; set; }

        static string JsonFacebook = @"{{
   ""id"": ""{0}"",
   ""name"": ""{1}"",
   ""first_name"": ""{2}"",
   ""last_name"": ""{3}"",
   ""link"": ""http://www.facebook.com/newmovie"",
   ""username"": ""newmovie"",
   ""location"": {{
      ""id"": ""106078429431815"",
      ""name"": ""London, United Kingdom""
   }},
   ""bio"": ""I am man."",
   ""quotes"": ""100\u0025 of the shots you don't take don't go in.\n  --Wayne Gretzky\n"",
   ""gender"": ""male"",
   ""email"": ""{4}"",
   ""timezone"": -4,
   ""locale"": ""en_GB"",
   ""verified"": true,
   ""updated_time"": ""2011-10-08T08:43:41+0000""
}}";
        static string JsonTwitter = @"[{{""is_translator"":false,""geo_enabled"":false,""profile_background_color"":""000000"",""protected"":false,""default_profile"":false,""profile_background_tile"":false,""created_at"":""Sun Nov 23 17:42:51 +0000 2008"",""name"":""{0}"",""profile_background_image_url_https"":""https:\/\/si0.twimg.com\/profile_background_images\/192991651\/twitter-bg.jpg"",""profile_sidebar_fill_color"":""2A372F"",""listed_count"":36,""notifications"":null,""utc_offset"":0,""friends_count"":267,""description"":""StackExchangarista, JavaScript, C#, Web & Mobile developer. Creator of the ServiceStack.NET projects. "",""following"":null,""verified"":false,""profile_sidebar_border_color"":""D9D082"",""followers_count"":796,""profile_image_url"":""http:\/\/a2.twimg.com\/profile_images\/1598852740\/avatar_normal.png"",""contributors_enabled"":false,""profile_image_url_https"":""https:\/\/si0.twimg.com\/profile_images\/1598852740\/avatar_normal.png"",""status"":{{""possibly_sensitive"":false,""place"":null,""retweet_count"":37,""in_reply_to_screen_name"":null,""created_at"":""Mon Nov 07 02:34:23 +0000 2011"",""retweeted"":false,""in_reply_to_status_id_str"":null,""in_reply_to_user_id_str"":null,""contributors"":null,""id_str"":""133371690876022785"",""retweeted_status"":{{""possibly_sensitive"":false,""place"":null,""retweet_count"":37,""in_reply_to_screen_name"":null,""created_at"":""Mon Nov 07 02:32:15 +0000 2011"",""retweeted"":false,""in_reply_to_status_id_str"":null,""in_reply_to_user_id_str"":null,""contributors"":null,""id_str"":""133371151551447041"",""in_reply_to_user_id"":null,""in_reply_to_status_id"":null,""source"":""\u003Ca href=\""http:\/\/www.arstechnica.com\"" rel=\""nofollow\""\u003EArs auto-tweeter\u003C\/a\u003E"",""geo"":null,""favorited"":false,""id"":133371151551447041,""coordinates"":null,""truncated"":false,""text"":""Google: Microsoft uses patents when products \""stop succeeding\"": http:\/\/t.co\/50QFc1uJ by @binarybits""}},""in_reply_to_user_id"":null,""in_reply_to_status_id"":null,""source"":""web"",""geo"":null,""favorited"":false,""id"":133371690876022785,""coordinates"":null,""truncated"":false,""text"":""RT @arstechnica: Google: Microsoft uses patents when products \""stop succeeding\"": http:\/\/t.co\/50QFc1uJ by @binarybits""}},""profile_use_background_image"":true,""favourites_count"":238,""location"":""New York"",""id_str"":""17575623"",""default_profile_image"":false,""show_all_inline_media"":false,""profile_text_color"":""ABB8AF"",""screen_name"":""demisbellot"",""statuses_count"":9638,""profile_background_image_url"":""http:\/\/a0.twimg.com\/profile_background_images\/192991651\/twitter-bg.jpg"",""url"":""http:\/\/www.servicestack.net\/mythz_blog\/"",""time_zone"":""London"",""profile_link_color"":""43594A"",""id"":17575623,""follow_request_sent"":null,""lang"":""en""}}]";

        static string JsonYammer = @"{
    ""job_title"":""Developer"",
    ""summary"":""I am a developer at XYZ Corp."",
    ""activated_at"":""2012/12/21 12:21:12 +0000"",
    ""schools"":[{
        ""start_year"":2000,
        ""degree"":""Code Master"",
        ""end_year"":2001,
        ""description"":"""",
        ""school"":""Code School""
    }],
    ""admin"":""false"",
    ""interests"":""Coding"",
    ""network_id"":12345,
    ""timezone"":""Perth"",
    ""state"":""active"",
    ""stats"":{
        ""updates"":212,
        ""following"":8,
        ""followers"":88
    },
    ""can_broadcast"":""false"",
    ""name"":""{2}{3}"",
    ""show_ask_for_photo"":false,
    ""settings"":{
        ""xdr_proxy"":""https://xdrproxy.yammer.com""
    },
    ""mugshot_url_template"":""https://mug0.assets-yammer.com/mugshot/images/{width}x{height}/someblah"",
    ""guid"":null,
    ""mugshot_url"":""https://mug0.assets-yammer.com/mugshot/images/48x48/someblah"",
    ""contact"":{
        ""has_fake_email"":false,
        ""phone_numbers"":[{
            ""number"":""+61888888888 x88888"",
            ""type"":""work""
        },{
            ""number"":""+61400123456"",
            ""type"":""mobile""
        }],
        ""im"":{
            ""provider"":""aim"",""username"":""""
        },
        ""email_addresses"":[{
            ""address"":""{4}"",""type"":""primary""
        }]
    },
    ""previous_companies"":[{
        ""position"":""Junior Developer"",
        ""start_year"":2001,
        ""end_year"":2002,
        ""description"":"""",
        ""employer"":""UVW Corp""}],
    ""network_name"":""ServiceStack"",
    ""network_domains"":[""servicestack.net""],
    ""verified_admin"":""false"",
    ""first_name"":""{2}"",
    ""significant_other"":"""",
    ""birth_date"":"""",
    ""id"":{0},
    ""url"":""https://www.yammer.com/api/v1/users/{0}"",
    ""type"":""user"",
    ""expertise"":""ServiceStack."",
    ""last_name"":""{3}"",
    ""location"":""Perth, WA, Australia"",
    ""web_url"":""https://www.yammer.com/servicestack.net/users/{2}{3}"",
    ""kids_names"":"""",
    ""hire_date"":null,
    ""department"":""IT"",
    ""external_urls"":[
        ""http://servicestack.net"",
        ""https://github.com/ServiceStack/ServiceStack""
    ],
    ""full_name"":""{1}""
}";

        public string DownloadTwitterUserInfo(OAuthAccessToken oauthToken, string twitterUserId)
        {
            twitterUserId.ThrowIfNullOrEmpty("twitterUserId");

            return JsonTwitter.Fmt(Tokens.DisplayName);
        }

        public string DownloadFacebookUserInfo(string facebookCode, params string[] fields)
        {
            facebookCode.ThrowIfNullOrEmpty("facebookCode");

            return JsonFacebook.Fmt(Tokens.UserId, Tokens.DisplayName,
                Tokens.FirstName, Tokens.LastName, Tokens.Email);
        }

        public string DownloadYammerUserInfo(string yammerUserId)
        {
            yammerUserId.ThrowIfNullOrEmpty("yammerUserId");

            return JsonYammer.Fmt(Tokens.UserId, Tokens.DisplayName,
                Tokens.FirstName, Tokens.LastName, Tokens.Email);
        }
    }
}
#endif