using FluentNHibernate.Mapping;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.NHibernate
{
    public class UserOAuthProviderMap : ClassMap<UserAuthDetails>
    {
        public UserOAuthProviderMap()
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

            HasMany(x => x.Items)
                .AsMap<string>(
                    index => index.Column("`Key`").Type<string>(),
                    element => element.Column("Value").Type<string>())
                .KeyColumn("UserOAuthProviderID")
                .Table("UserOAuthProvider_Items")
                .Not.LazyLoad()
                .Cascade.All();

        }
    }

}