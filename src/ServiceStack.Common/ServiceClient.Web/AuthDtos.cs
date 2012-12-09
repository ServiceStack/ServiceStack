using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Common.ServiceClient.Web
{
    //Copy from ServiceStack.ServiceInterface.Auth to avoid deps
    [DataContract]
    public class Auth : IReturn<AuthResponse>
    {
        [DataMember(Order=1)] public string provider { get; set; }
        [DataMember(Order=2)] public string State { get; set; }
        [DataMember(Order=3)] public string oauth_token { get; set; }
        [DataMember(Order=4)] public string oauth_verifier { get; set; }
        [DataMember(Order=5)] public string UserName { get; set; }
        [DataMember(Order=6)] public string Password { get; set; }
        [DataMember(Order=7)] public bool? RememberMe { get; set; }
        [DataMember(Order=8)] public string Continue { get; set; }
        // Thise are used for digest auth
        [DataMember(Order=9)] public string nonce { get; set; }
        [DataMember(Order=10)] public string uri { get; set; }
        [DataMember(Order=11)] public string response { get; set; }
        [DataMember(Order=12)] public string qop { get; set; }
        [DataMember(Order=13)] public string nc { get; set; }
        [DataMember(Order=14)] public string cnonce { get; set; }
    }

    [DataContract]
    public class AuthResponse
    {
        public AuthResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order=1)] public string SessionId { get; set; }
        [DataMember(Order=2)] public string UserName { get; set; }
        [DataMember(Order=3)] public string ReferrerUrl { get; set; }
        [DataMember(Order=4)] public ResponseStatus ResponseStatus { get; set; }
    }


    [DataContract]
    public class Registration : IReturn<RegistrationResponse>
    {
        [DataMember(Order = 1)] public string UserName { get; set; }
        [DataMember(Order = 2)] public string FirstName { get; set; }
        [DataMember(Order = 3)] public string LastName { get; set; }
        [DataMember(Order = 4)] public string DisplayName { get; set; }
        [DataMember(Order = 5)] public string Email { get; set; }
        [DataMember(Order = 6)] public string Password { get; set; }
        [DataMember(Order = 7)] public bool? AutoLogin { get; set; }
        [DataMember(Order = 8)] public string Continue { get; set; }
    }

    [DataContract]
    public class RegistrationResponse
    {
        public RegistrationResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order = 1)] public string UserId { get; set; }
        [DataMember(Order = 2)] public string SessionId { get; set; }
        [DataMember(Order = 3)] public string UserName { get; set; }
        [DataMember(Order = 4)] public string ReferrerUrl { get; set; }
        [DataMember(Order = 5)] public ResponseStatus ResponseStatus { get; set; }
    }
}