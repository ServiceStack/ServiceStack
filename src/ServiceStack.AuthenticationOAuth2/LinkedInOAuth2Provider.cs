﻿namespace ServiceStack.Authentication.OAuth2
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using ServiceStack.Configuration;
    using ServiceStack.Text;

    public class LinkedInOAuth2Provider : OAuth2Provider
    {
        public const string Name = "LinkedIn";

        public const string Realm = "https://www.linkedin.com/uas/oauth2/authorization";

        public LinkedInOAuth2Provider(IResourceManager appSettings)
            : base(appSettings, Realm, Name)
        {
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl + accessToken;
            string contents = url.GetStringFromUrl();
            XDocument xml = XDocument.Parse(contents);
            var authInfo = new Dictionary<string, string>
                               {
                                   { "user_id", xml.Descendants("id").FirstOrDefault().Value }, 
                                   { "username", xml.Descendants("email-address").FirstOrDefault().Value }, 
                                   { "email", xml.Descendants("email-address").FirstOrDefault().Value }, 
                                   { "name", xml.Descendants("formatted-name").FirstOrDefault().Value }, 
                                   { "first_name", xml.Descendants("first-name").FirstOrDefault().Value }, 
                                   { "last_name", xml.Descendants("last-name").FirstOrDefault().Value }
                               };
            return authInfo;
        }
    }
}