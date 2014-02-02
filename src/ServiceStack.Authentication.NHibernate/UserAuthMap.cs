using System.Collections.Generic;
using FluentNHibernate.Mapping;
using ServiceStack.Auth;

namespace ServiceStack.Authentication.NHibernate
{
    public class UserAuthMap : ClassMap<UserAuthNHibernate>
    {
        public UserAuthMap()
        {
            Table("UserAuth");
            Id(x => x.Id)
                .GeneratedBy.Native();

            Map(x => x.CreatedDate);
            Map(x => x.DisplayName);
            Map(x => x.Email);
            Map(x => x.FirstName);
            Map(x => x.LastName);
            Map(x => x.ModifiedDate);
            Map(x => x.PasswordHash);
            Map(x => x.PrimaryEmail);
            Map(x => x.Salt);
            Map(x => x.UserName);

            HasManyToMany(x => x.Permissions1)
                .Table("UserAuth_Permissions")
                .ParentKeyColumn("UserAuthID")
                .Element("Permission");

            HasManyToMany(x => x.Roles1)
                .Table("UserAuth_Roles")
                .ParentKeyColumn("UserAuthID")
                .Element("Role");

        }
    }

    public class UserAuthNHibernate : UserAuth
    {
        public UserAuthNHibernate()
            : base()
        { }

        public UserAuthNHibernate(IUserAuth userAuth)
        {
            Id = userAuth.Id;
            UserName = userAuth.UserName;
            Email = userAuth.Email;
            PrimaryEmail = userAuth.PrimaryEmail;
            FirstName = userAuth.FirstName;
            LastName = userAuth.LastName;
            DisplayName = userAuth.DisplayName;
            Salt = userAuth.Salt;
            PasswordHash = userAuth.PasswordHash;
            Roles = userAuth.Roles;
            Permissions = userAuth.Permissions;
            CreatedDate = userAuth.CreatedDate;
            ModifiedDate = userAuth.ModifiedDate;
        }

        public virtual IList<string> Permissions1
        {
            get { return Permissions; }
            set { Permissions = new List<string>(value); }
        }

        public virtual IList<string> Roles1
        {
            get { return Roles; }
            set { Roles = new List<string>(value); }
        }
    }

}