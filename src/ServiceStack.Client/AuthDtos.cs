// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    [DataContract]
    public class Authenticate : IReturn<AuthenticateResponse>, IMeta
    {
        [DataMember(Order = 1)] public string provider { get; set; }
        [DataMember(Order = 2)] public string State { get; set; }
        [DataMember(Order = 3)] public string oauth_token { get; set; }
        [DataMember(Order = 4)] public string oauth_verifier { get; set; }
        [DataMember(Order = 5)] public string UserName { get; set; }
        [DataMember(Order = 6)] public string Password { get; set; }
        [DataMember(Order = 7)] public bool? RememberMe { get; set; }
        [DataMember(Order = 8)] public string Continue { get; set; }
        // Thise are used for digest auth
        [DataMember(Order = 9)] public string nonce { get; set; }
        [DataMember(Order = 10)] public string uri { get; set; }
        [DataMember(Order = 11)] public string response { get; set; }
        [DataMember(Order = 12)] public string qop { get; set; }
        [DataMember(Order = 13)] public string nc { get; set; }
        [DataMember(Order = 14)] public string cnonce { get; set; }
        [DataMember(Order = 15)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class AuthenticateResponse : IMeta
    {
        public AuthenticateResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order = 1)] public string UserId { get; set; }
        [DataMember(Order = 2)] public string SessionId { get; set; }
        [DataMember(Order = 3)] public string UserName { get; set; }
        [DataMember(Order = 4)] public string DisplayName { get; set; }
        [DataMember(Order = 5)] public string ReferrerUrl { get; set; }

        [DataMember(Order = 6)] public ResponseStatus ResponseStatus { get; set; }
        [DataMember(Order = 7)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class Register : IReturn<RegisterResponse>
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
    public class RegisterResponse : IMeta
    {
        public RegisterResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember(Order = 1)] public string UserId { get; set; }
        [DataMember(Order = 2)] public string SessionId { get; set; }
        [DataMember(Order = 3)] public string UserName { get; set; }
        [DataMember(Order = 4)] public string ReferrerUrl { get; set; }

        [DataMember(Order = 5)] public ResponseStatus ResponseStatus { get; set; }
        [DataMember(Order = 6)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class AssignRoles : IReturn<AssignRolesResponse>
    {
        public AssignRoles()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        [DataMember(Order = 1)]
        public string UserName { get; set; }

        [DataMember(Order = 2)]
        public List<string> Permissions { get; set; }

        [DataMember(Order = 3)]
        public List<string> Roles { get; set; }
    }

    [DataContract]
    public class AssignRolesResponse : IHasResponseStatus
    {
        public AssignRolesResponse()
        {
            this.AllRoles = new List<string>();
            this.AllPermissions = new List<string>();
        }

        [DataMember(Order = 1)]
        public List<string> AllRoles { get; set; }

        [DataMember(Order = 2)]
        public List<string> AllPermissions { get; set; }

        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class UnAssignRoles : IReturn<UnAssignRolesResponse>
    {
        public UnAssignRoles()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        [DataMember(Order = 1)]
        public string UserName { get; set; }

        [DataMember(Order = 2)]
        public List<string> Permissions { get; set; }

        [DataMember(Order = 3)]
        public List<string> Roles { get; set; }
    }

    [DataContract]
    public class UnAssignRolesResponse : IHasResponseStatus
    {
        public UnAssignRolesResponse()
        {
            this.AllRoles = new List<string>();
        }

        [DataMember(Order = 1)]
        public List<string> AllRoles { get; set; }

        [DataMember(Order = 2)]
        public List<string> AllPermissions { get; set; }

        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CancelRequest : IReturn<CancelRequestResponse>
    {
        [DataMember(Order = 1)]
        public string Tag { get; set; }
    }

    [DataContract]
    public class CancelRequestResponse
    {
        [DataMember(Order = 1)]
        public string Tag { get; set; }

        [DataMember(Order = 2)]
        public TimeSpan Elapsed { get; set; }

        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }
}
