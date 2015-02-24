using System;
using System.Runtime.Serialization;
using ServiceStack.Configuration;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public interface IRequiresSession
    {
        Guid SessionId { get; }
    }

    [DataContract]
    public class Secure : IRequiresSession
    {
        [DataMember]
        public Guid SessionId { get; set; }

        [DataMember]
        public int StatusCode { get; set; }
    }

    [DataContract]
    public class SecureResponse
    {
        [DataMember]
        public string Value { get; set; }
    }

    [Authenticate]
    [Route("/requiresadmin")]
    public class RequiresRole
    {
        public string Role { get; set; }
    }

    public class SecureService : Service
    {
        public object Any(Secure request)
        {
            throw new UnauthorizedAccessException("You shouldn't be able to see this");
        }

        public object Any(RequiresRole request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, request.Role ?? RoleNames.Admin);

            return request;
        }
    }
}