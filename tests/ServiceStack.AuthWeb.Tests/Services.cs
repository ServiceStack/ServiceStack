//#define HTTP_LISTENER

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

#if HTTP_LISTENER
namespace ServiceStack.Auth.Tests
#else
namespace ServiceStack.AuthWeb.Tests
#endif
{
    [Route("/profile")]
    public class GetUserProfile { }

    public class UserProfile
    {
        public int Id { get; set; }

        public UserAuth UserAuth { get; set; }
        public AuthUserSession Session { get; set; }
        public List<UserAuthDetails> UserAuthDetails { get; set; }
    }

    public class UserProfileResponse
    {
        public UserProfile Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/lockallusers")]
    public class LockAllUsers {}
    public class LockServices : Service
    {
        public object Any(LockAllUsers request)
        {
            Db.UpdateOnly(new UserAuth { LockedDate = DateTime.UtcNow },
                onlyFields: x => new { x.LockedDate },
                where: x => x.LockedDate == null);

            return HttpResult.Redirect("/");
        }
    }

    [Authenticate]
    public class UserProfileService : Service
    {
        public UserProfile Get(GetUserProfile request)
        {
            var session = base.SessionAs<CustomUserSession>();

            var userAuthId = session.UserAuthId.ToInt();
            var userProfile = new UserProfile
            {
                Id = userAuthId,
                Session = session,
                UserAuth = Db.SingleById<UserAuth>(userAuthId),
                UserAuthDetails = Db.Select<UserAuthDetails>(x => x.UserAuthId == userAuthId),
            };

            return userProfile;
        }
    }

    [Route("/reset-userauth")]
    public class ResetUserAuth {}
    public class ResetUserAuthService : Service
    {
        public object Get(ResetUserAuth request)
        {
            this.Cache.Remove(SessionFeature.GetSessionKey(Request));

            Db.DeleteAll<UserAuth>();
            Db.DeleteAll<UserAuthDetails>();

            var referrer = Request.UrlReferrer != null
                ? Request.UrlReferrer.AbsoluteUri
                : HttpHandlerFactory.GetBaseUrl();

            return HttpResult.Redirect(referrer);
        }
    }


    [Route("/rockstars")]
    [Route("/rockstars/aged/{Age}")]
    [Route("/rockstars/delete/{Delete}")]
    [Route("/rockstars/{Id}")]
    public class Rockstars
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string Delete { get; set; }
    }

    [DataContract]
    public class RockstarsResponse
    {
        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int? Aged { get; set; }

        [DataMember]
        public List<Rockstar> Results { get; set; }
    }

    public class Rockstar
    {
        public static Rockstar[] SeedData = new[] {
            new Rockstar(1, "Jimi", "Hendrix", 27), 
            new Rockstar(2, "Janis", "Joplin", 27), 
            new Rockstar(3, "Jim", "Morrisson", 27), 
            new Rockstar(4, "Kurt", "Cobain", 27),              
            new Rockstar(5, "Elvis", "Presley", 42), 
            new Rockstar(6, "Michael", "Jackson", 50), 
        };

        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public bool Alive { get; set; }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }

    [DefaultRequest(typeof(Rockstars))]
    public class RockstarsService : Service
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public object Get(Rockstars request)
        {
            if (request.Delete == "reset")
            {
                Db.DeleteAll<Rockstar>();
                Db.Insert(Rockstar.SeedData);
            }
            else if (request.Delete.IsInt())
            {
                Db.DeleteById<Rockstar>(request.Delete.ToInt());
            }

            return new RockstarsResponse
            {
                Aged = request.Age,
                Total = Db.Scalar<int>("select count(*) from Rockstar"),
                Results = request.Id != default(int) ?
                    Db.Select<Rockstar>(q => q.Id == request.Id)
                      : request.Age.HasValue ?
                    Db.Select<Rockstar>(q => q.Age == request.Age.Value)
                      : Db.Select<Rockstar>()
            };
        }

        public object Post(Rockstars request)
        {
            Db.Insert(request.ConvertTo<Rockstar>());
            return Get(new Rockstars());
        }
    }

    [Route("/viewmodel/{Id}")]
    public class ViewThatUsesLayoutAndModel
    {
        public string Id { get; set; }
    }

    public class ViewThatUsesLayoutAndModelResponse
    {
        public string Name { get; set; }
        public List<string> Results { get; set; }
    }

    public class ViewService : Service
    {
        public object Any(ViewThatUsesLayoutAndModel request)
        {
            return new ViewThatUsesLayoutAndModelResponse
            {
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

    [Route("/has-permission")]
    public class HasPermission : IReturn<CustomUserSession> { }

    [RequiredPermission("ThePermission")]
    public class RequiresPermissionService : Service
    {
        public object Any(HasPermission request)
        {
            return SessionAs<CustomUserSession>();
        }
    }

    [Route("/has-role")]
    public class HasRole : IReturn<CustomUserSession> { }

    [RequiredRole("TheRole")]
    public class RequiresRoleService : Service
    {
        public object Any(HasRole request)
        {
            return SessionAs<CustomUserSession>();
        }
    }
}
