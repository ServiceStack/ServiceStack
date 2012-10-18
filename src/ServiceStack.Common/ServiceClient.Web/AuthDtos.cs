using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Common.ServiceClient.Web
{
    //Copy from ServiceStack.ServiceInterface.Auth to avoid deps
    public class Auth
    {
        public string provider { get; set; }
        public string State { get; set; }
        public string oauth_token { get; set; }
        public string oauth_verifier { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool? RememberMe { get; set; }
    }

    public class AuthResponse
    {
        public AuthResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        public string SessionId { get; set; }

        public string UserName { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }
}