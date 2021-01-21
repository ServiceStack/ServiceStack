using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using System;
using System.Linq;

namespace ServiceStack.Authentication.RavenDb
{
    [Index(Name = nameof(Key))]
    public class RavenUserAuth : UserAuth
    {
        string key;

        public string Key
        {
            get => key;

            set
            {
                key = value;
                Id = RavenIdConverter.ToInt(key);
            }
        }
    }
}