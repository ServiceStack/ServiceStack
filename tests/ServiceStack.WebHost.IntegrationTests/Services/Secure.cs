using System;
using System.Collections.Generic;
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
    public class RequiresRoleInService
    {
        public string Role { get; set; }
    }


    [Authenticate]
    [Route("/testauth")]
    public class TestAuth
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    [Authenticate]
    public class RequiresAuthRequest : IReturn<RequiresAuthRequest>
    {
        public string Name { get; set; }
    }

    public class RequiresAuthAction : IReturn<RequiresAuthAction>
    {
        public string Name { get; set; }
    }

    [RequiredRole("TheRole")]
    public class RequiresRoleRequest : IReturn<RequiresRoleRequest>
    {
        public string Name { get; set; }
    }

    public class RequiresRoleAction : IReturn<RequiresRoleAction>
    {
        public string Name { get; set; }
    }

    [RequiresAnyRole("TheRole", "TheRole2")]
    public class RequiresAnyRoleRequest : IReturn<RequiresAnyRoleRequest>
    {
        public List<string> Roles { get; set; }

        public RequiresAnyRoleRequest()
        {
            Roles = new List<string>();
        }
    }

    [RequiredPermission("ThePermission")]
    public class RequiresPermissionRequest : IReturn<RequiresPermissionRequest>
    {
        public string Name { get; set; }
    }

    [RequiresAnyPermission("ThePermission", "ThePermission2")]
    public class RequiresAnyPermissionRequest : IReturn<RequiresAnyPermissionRequest>
    {
        public List<string> Permissions { get; set; }

        public RequiresAnyPermissionRequest()
        {
            Permissions = new List<string>();
        }
    }

    public class RequiresRolesAndPermissionsOnRequestService : Service
    {
        public object Any(RequiresAuthRequest request)
        {
            return request;
        }

        public object Any(RequiresRoleRequest request)
        {
            return request;
        }

        public object Any(RequiresAnyRoleRequest request)
        {
            return request;
        }

        public object Any(RequiresPermissionRequest request)
        {
            return request;
        }

        public object Any(RequiresAnyPermissionRequest request)
        {
            return request;
        }

        [RequiredRole("TheRole")]
        public object Any(RequiresRoleAction request)
        {
            return request;
        }

        [Authenticate]
        public object Any(RequiresAuthAction request)
        {
            return request;
        }
    }

    public class SecureService : Service
    {
        public object Any(Secure request)
        {
            throw new UnauthorizedAccessException("You shouldn't be able to see this");
        }

        public object Any(RequiresRoleInService request)
        {
            RequiredRoleAttribute.AssertRequiredRoles(Request, request.Role ?? RoleNames.Admin);

            return request;
        }

        public object Any(TestAuth request)
        {
            return request;
        }
    }
}