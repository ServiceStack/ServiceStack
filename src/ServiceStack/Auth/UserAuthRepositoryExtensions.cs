using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ServiceStack.Auth
{
    public static class UserAuthRepositoryExtensions
    {
        /// <summary>
        /// Creates the required missing tables or DB schema 
        /// </summary>
        public static void InitSchema(this IAuthRepository authRepo)
        {
            var requiresSchema = authRepo as IRequiresSchema;
            if (requiresSchema != null)
            {
                requiresSchema.InitSchema();
            }
        }

        public static void AssignRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var managesRoles = UserAuthRepo as IManageRoles;
            if (managesRoles != null)
            {
                managesRoles.AssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
            {
                if (!roles.IsEmpty())
                {
                    foreach (var missingRole in roles.Where(x => !userAuth.Roles.Contains(x)))
                    {
                        userAuth.Roles.Add(missingRole);
                    }
                }

                if (!permissions.IsEmpty())
                {
                    foreach (var missingPermission in permissions.Where(x => !userAuth.Permissions.Contains(x)))
                    {
                        userAuth.Permissions.Add(missingPermission);
                    }
                }

                UserAuthRepo.SaveUserAuth(userAuth);
            }
        }

        public static void UnAssignRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var managesRoles = UserAuthRepo as IManageRoles;
            if (managesRoles != null)
            {
                managesRoles.UnAssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    UserAuthRepo.SaveUserAuth(userAuth);
                }
            }
        }

        public static ICollection<string> GetRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth)
        {
            var managesRoles = UserAuthRepo as IManageRoles;
            if (managesRoles != null)
            {
                return managesRoles.GetRoles(userAuth.Id.ToString());
            }
            else
            {
                return userAuth.Roles;
            }
        }

        public static ICollection<string> GetPermissions(this IAuthRepository UserAuthRepo, IUserAuth userAuth)
        {
            var managesRoles = UserAuthRepo as IManageRoles;
            if (managesRoles != null)
            {
                return managesRoles.GetPermissions(userAuth.Id.ToString());
            }
            else
            {
                return userAuth.Permissions;
            }
        }

        public static void PopulateSession(this IAuthSession session, IUserAuth userAuth, List<IAuthTokens> authTokens)
        {
            if (userAuth == null)
                return;

            var originalId = session.Id;
            session.PopulateWith(userAuth);
            session.Id = originalId;
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = authTokens;
        }
    }
}