using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using System;

namespace ServiceStack.Authentication.RavenDb
{
    [Index(Name = nameof(Key))]
    public class RavenUserAuthDetails : UserAuthDetails
    {
        public string Key { get; set; }
    }
}