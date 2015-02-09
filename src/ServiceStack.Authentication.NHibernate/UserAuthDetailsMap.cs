using System.Collections.Generic;
using FluentNHibernate.Mapping;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.NHibernate
{
    public class UserAuthDetailsMap : ClassMap<UserAuthDetailsNHibernate>
    {
        public UserAuthDetailsMap()
        {
            Table("UserOAuthProvider");
            Id(x => x.Id)
                .GeneratedBy.Native();

            Map(x => x.AccessToken);
            Map(x => x.AccessTokenSecret);
            Map(x => x.CreatedDate);
            Map(x => x.DisplayName);
            Map(x => x.Email);
            Map(x => x.FirstName);
            Map(x => x.LastName);
            Map(x => x.ModifiedDate);
            Map(x => x.Provider);
            Map(x => x.RequestToken);
            Map(x => x.RequestTokenSecret);
            Map(x => x.UserAuthId);
            Map(x => x.UserId);
            Map(x => x.UserName);

            HasMany(x => x.Items1)
                .AsMap<string>(
                    index => index.Column("`Key`").Type<string>(),
                    element => element.Column("Value").Type<string>())
                .KeyColumn("UserOAuthProviderID")
                .Table("UserOAuthProvider_Items")
                .Not.LazyLoad()
                .Cascade.All();

        }

    }

    public class UserAuthDetailsNHibernate : UserAuthDetails 
    {
        public UserAuthDetailsNHibernate() : base() { }

        public UserAuthDetailsNHibernate(IUserAuthDetails userAuthDetails)
        {
            Id = userAuthDetails.Id;
            UserAuthId = userAuthDetails.UserAuthId;
            Provider = userAuthDetails.Provider;
            UserId = userAuthDetails.UserId;
            UserName = userAuthDetails.UserName;
            DisplayName = userAuthDetails.DisplayName;
            FirstName = userAuthDetails.FirstName;
            LastName = userAuthDetails.LastName;
            Email = userAuthDetails.Email;
            RequestToken = userAuthDetails.RequestToken;
            RequestTokenSecret = userAuthDetails.RequestTokenSecret;
            Items = userAuthDetails.Items;
            AccessToken = userAuthDetails.AccessToken;
            AccessTokenSecret = userAuthDetails.AccessTokenSecret;
            CreatedDate = userAuthDetails.CreatedDate;
            ModifiedDate = userAuthDetails.ModifiedDate;
        }

        public virtual IDictionary<string, string> Items1
        {
            get { return Items; }
            set { Items = new Dictionary<string, string>(value); }
        }
    }

}