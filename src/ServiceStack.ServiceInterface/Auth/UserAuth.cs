using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
    public class UserAuth
    {
        public UserAuth()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        [AutoIncrement]
        public virtual int Id { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        public virtual string PrimaryEmail { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string Salt { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string DigestHA1Hash { get; set; }
        public virtual List<string> Roles { get; set; }
        public virtual List<string> Permissions { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }

        public T Get<T>()
        {
            string str = null;
            if (Meta != null) Meta.TryGetValue(typeof(T).Name, out str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public void Set<T>(T value)
        {
            if (Meta == null) Meta = new Dictionary<string, string>();
            Meta[typeof(T).Name] = TypeSerializer.SerializeToString(value);
        }

        public virtual void PopulateMissing(UserOAuthProvider authProvider)
        {
            if (!authProvider.FirstName.IsNullOrEmpty())
                this.FirstName = authProvider.FirstName;
            if (!authProvider.LastName.IsNullOrEmpty())
                this.LastName = authProvider.LastName;
            if (!authProvider.DisplayName.IsNullOrEmpty())
                this.DisplayName = authProvider.DisplayName;
            if (!authProvider.Email.IsNullOrEmpty())
                this.PrimaryEmail = authProvider.Email;
        }
    }

    public class UserOAuthProvider : IOAuthTokens
    {
        public UserOAuthProvider()
        {
            this.Items = new Dictionary<string, string>();
        }

        [AutoIncrement]
        public virtual int Id { get; set; }
        public virtual int UserAuthId { get; set; }
        public virtual string Provider { get; set; }
        public virtual string UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string Email { get; set; }
        public virtual string RequestToken { get; set; }
        public virtual string RequestTokenSecret { get; set; }
        public virtual Dictionary<string, string> Items { get; set; }
        public virtual string AccessToken { get; set; }
        public virtual string AccessTokenSecret { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }

        public T Get<T>()
        {
            string str = null;
            if (Meta != null) Meta.TryGetValue(typeof(T).Name, out str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public void Set<T>(T value)
        {
            if (Meta == null) Meta = new Dictionary<string, string>();
            Meta[typeof(T).Name] = TypeSerializer.SerializeToString(value);
        }

        public virtual void PopulateMissing(IOAuthTokens withTokens)
        {
            if (!withTokens.UserId.IsNullOrEmpty())
                this.UserId = withTokens.UserId;
            if (!withTokens.UserName.IsNullOrEmpty())
                this.UserName = withTokens.UserName;
            if (!withTokens.RequestToken.IsNullOrEmpty())
                this.RequestToken = withTokens.RequestToken;
            if (!withTokens.RequestTokenSecret.IsNullOrEmpty())
                this.RequestTokenSecret = withTokens.RequestTokenSecret;
            if (!withTokens.AccessToken.IsNullOrEmpty())
                this.AccessToken = withTokens.AccessToken;
            if (!withTokens.AccessTokenSecret.IsNullOrEmpty())
                this.AccessTokenSecret = withTokens.AccessTokenSecret;
            if (!withTokens.DisplayName.IsNullOrEmpty())
                this.DisplayName = withTokens.DisplayName;
            if (!withTokens.FirstName.IsNullOrEmpty())
                this.FirstName = withTokens.FirstName;
            if (!withTokens.LastName.IsNullOrEmpty())
                this.LastName = withTokens.LastName;
            if (!withTokens.Email.IsNullOrEmpty())
                this.Email = withTokens.Email;
        }
    }

}