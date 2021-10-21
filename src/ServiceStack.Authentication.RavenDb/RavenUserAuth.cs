using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using System;

namespace ServiceStack.Authentication.RavenDb
{
    [Index(Name = nameof(Key))]
    public class RavenUserAuth : UserAuth
    {
        public string Key { get; set; }
    }
}