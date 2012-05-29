using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
    public interface IOAuthTokens
    {
        string Provider { get; set; }
        string UserId { get; set; }
        string UserName { get; set; }
        string DisplayName { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        string AccessToken { get; set; }
        string AccessTokenSecret { get; set; }
        string RequestToken { get; set; }
        string RequestTokenSecret { get; set; }
        Dictionary<string, string> Items { get; set; }
    }

    public class OAuthTokens : IOAuthTokens
    {
        public OAuthTokens()
        {
            this.Items = new Dictionary<string, string>();
        }

        public string Provider { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        public string RequestToken { get; set; }
        public string RequestTokenSecret { get; set; }
        public Dictionary<string, string> Items { get; set; }
    }
}