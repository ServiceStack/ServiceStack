﻿// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    [DataContract]
    public class Authenticate : IPost, IReturn<AuthenticateResponse>, IMeta
    {
        [DataMember(Order = 1)] public string provider { get; set; }
        [DataMember(Order = 2)] public string State { get; set; }
        [DataMember(Order = 3)] public string oauth_token { get; set; }
        [DataMember(Order = 4)] public string oauth_verifier { get; set; }
        [DataMember(Order = 5)] public string UserName { get; set; }
        [DataMember(Order = 6)] public string Password { get; set; }
        [DataMember(Order = 7)] public bool? RememberMe { get; set; }
        [DataMember(Order = 8)] public string Continue { get; set; }
        [DataMember(Order = 9)] public string ErrorView { get; set; }

        // digest auth
        [DataMember(Order = 10)] public string nonce { get; set; }
        [DataMember(Order = 11)] public string uri { get; set; }
        [DataMember(Order = 12)] public string response { get; set; }
        [DataMember(Order = 13)] public string qop { get; set; }
        [DataMember(Order = 14)] public string nc { get; set; }
        [DataMember(Order = 15)] public string cnonce { get; set; }

        [DataMember(Order = 16)] public bool? UseTokenCookie { get; set; }

        [DataMember(Order = 17)] public string AccessToken { get; set; }
        [DataMember(Order = 18)] public string AccessTokenSecret { get; set; }

        [DataMember(Order = 19)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class AuthenticateResponse : IMeta, IHasSessionId, IHasBearerToken
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
        [DataMember(Order = 6)] public string BearerToken { get; set; }
        [DataMember(Order = 7)] public string RefreshToken { get; set; }
        [DataMember(Order = 8)] public List<string> Roles { get; set; } 
        [DataMember(Order = 9)] public List<string> Permissions { get; set; } 

        [DataMember(Order = 10)] public ResponseStatus ResponseStatus { get; set; }
        [DataMember(Order = 11)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class Register : IPost, IReturn<RegisterResponse>, IMeta
    {
        [DataMember(Order = 1)] public string UserName { get; set; }
        [DataMember(Order = 2)] public string FirstName { get; set; }
        [DataMember(Order = 3)] public string LastName { get; set; }
        [DataMember(Order = 4)] public string DisplayName { get; set; }
        [DataMember(Order = 5)] public string Email { get; set; }
        [DataMember(Order = 6)] public string Password { get; set; }
        [DataMember(Order = 7)] public string ConfirmPassword { get; set; }
        [DataMember(Order = 8)] public bool? AutoLogin { get; set; }
        [DataMember(Order = 9)] public string Continue { get; set; }
        [DataMember(Order = 10)] public string ErrorView { get; set; }
        [DataMember(Order = 11)] public Dictionary<string, string> Meta { get; set; }
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
        [DataMember(Order = 5)] public string BearerToken { get; set; }
        [DataMember(Order = 6)] public string RefreshToken { get; set; }

        [DataMember(Order = 7)] public ResponseStatus ResponseStatus { get; set; }
        [DataMember(Order = 8)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class AssignRoles : IPost, IReturn<AssignRolesResponse>, IMeta
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
        [DataMember(Order = 4)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class AssignRolesResponse : IHasResponseStatus, IMeta
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

        [DataMember(Order = 3)] public Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class UnAssignRoles : IPost, IReturn<UnAssignRolesResponse>, IMeta
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

        [DataMember(Order = 4)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class UnAssignRolesResponse : IHasResponseStatus, IMeta
    {
        public UnAssignRolesResponse()
        {
            this.AllRoles = new List<string>();
        }

        [DataMember(Order = 1)]
        public List<string> AllRoles { get; set; }

        [DataMember(Order = 2)]
        public List<string> AllPermissions { get; set; }

        [DataMember(Order = 3)] public Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CancelRequest : IPost, IReturn<CancelRequestResponse>, IMeta
    {
        [DataMember(Order = 1)]
        public string Tag { get; set; }

        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class CancelRequestResponse : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)]
        public string Tag { get; set; }

        [DataMember(Order = 2)]
        public TimeSpan Elapsed { get; set; }

        [DataMember(Order = 3)] public Dictionary<string, string> Meta { get; set; }
        
        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Exclude(Feature.Soap)]
    [DataContract]
    [Route("/event-subscribers/{Id}", "POST")]
    public class UpdateEventSubscriber : IPost, IReturn<UpdateEventSubscriberResponse>
    {
        [DataMember(Order = 1)]
        public string Id { get; set; }
        [DataMember(Order = 2)]
        public string[] SubscribeChannels { get; set; }
        [DataMember(Order = 3)]
        public string[] UnsubscribeChannels { get; set; }
    }

    [DataContract]
    public class UpdateEventSubscriberResponse
    {
        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Exclude(Feature.Soap)]
    public class GetEventSubscribers : IGet, IReturn<List<Dictionary<string, string>>>
    {
        public string[] Channels { get; set; }
    }

    [DataContract]
    public class GetApiKeys : IGet, IReturn<GetApiKeysResponse>, IMeta
    {
        [DataMember(Order = 1)] public string Environment { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class GetApiKeysResponse : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)] public List<UserApiKey> Results { get; set; }

        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
        [DataMember(Order = 3)] public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class RegenerateApiKeys : IPost, IReturn<RegenerateApiKeysResponse>, IMeta
    {
        [DataMember(Order = 1)] public string Environment { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class RegenerateApiKeysResponse : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)] public List<UserApiKey> Results { get; set; }

        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
        [DataMember(Order = 3)] public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class UserApiKey : IMeta
    {
        [DataMember(Order = 1)] public string Key { get; set; }
        [DataMember(Order = 2)] public string KeyType { get; set; }
        [DataMember(Order = 3)] public DateTime? ExpiryDate { get; set; }
        [DataMember(Order = 4)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class ConvertSessionToToken : IPost, IReturn<ConvertSessionToTokenResponse>, IMeta
    {
        [DataMember(Order = 1)]
        public bool PreserveSession { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class ConvertSessionToTokenResponse : IMeta
    {
        [DataMember(Order = 1)]
        public Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 2)]
        public string AccessToken { get; set; }

        [DataMember(Order = 3)]
        public string RefreshToken { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class GetAccessToken : IPost, IReturn<GetAccessTokenResponse>, IMeta
    {
        [DataMember(Order = 1)]
        public string RefreshToken { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
    }

    [DataContract]
    public class GetAccessTokenResponse : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)]
        public string AccessToken { get; set; }

        [DataMember(Order = 2)] public Dictionary<string, string> Meta { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    [ExcludeMetadata]
    [Route("/metadata/nav")]
    [DataContract]
    public class GetNavItems : IReturn<GetNavItemsResponse> {}

    [DataContract]
    public class GetNavItemsResponse : IMeta
    {
        [DataMember(Order = 1)]
        public List<NavItem> Results { get; set; }
        [DataMember(Order = 2)]
        public Dictionary<string, List<NavItem>> NavItemsMap { get; set; }
        [DataMember(Order = 3)]
        public Dictionary<string, string> Meta { get; set; }
        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

}
