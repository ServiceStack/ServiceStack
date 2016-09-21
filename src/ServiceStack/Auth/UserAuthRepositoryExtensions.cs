using System;
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
            return managesRoles != null 
                ? managesRoles.GetRoles(userAuth.Id.ToString()) 
                : userAuth.Roles;
        }

        public static ICollection<string> GetPermissions(this IAuthRepository UserAuthRepo, IUserAuth userAuth)
        {
            var managesRoles = UserAuthRepo as IManageRoles;
            return managesRoles != null 
                ? managesRoles.GetPermissions(userAuth.Id.ToString()) 
                : userAuth.Permissions;
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

        public static List<IUserAuthDetails> GetUserAuthDetails(this IAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuthDetails(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static IUserAuth GetUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static void DeleteUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            authRepo.DeleteUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static ICollection<string> GetRoles(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetRoles(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static ICollection<string> GetPermissions(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetPermissions(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static bool HasRole(this IManageRoles manageRoles, int userAuthId, string role)
        {
            return manageRoles.HasRole(userAuthId.ToString(CultureInfo.InvariantCulture), role);
        }

        public static bool HasPermission(this IManageRoles manageRoles, int userAuthId, string permission)
        {
            return manageRoles.HasPermission(userAuthId.ToString(CultureInfo.InvariantCulture), permission);
        }

        public static void AssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.AssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
        }

        public static void UnAssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.UnAssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
        }

        static IUserAuthRepository AssertUserAuthRepository(this IAuthRepository repo)
        {
            var userRepo = repo as IUserAuthRepository;
            if (userRepo == null)
                throw new NotSupportedException("This operation requires a IUserAuthRepository");

            return userRepo;
        }

        public static IUserAuth CreateUserAuth(this IAuthRepository authRepo, IUserAuth newUser, string password)
        {
            return authRepo.AssertUserAuthRepository().CreateUserAuth(newUser, password);
        }

        public static IUserAuth UpdateUserAuth(this IAuthRepository authRepo, IUserAuth existingUser, IUserAuth newUser, string password)
        {
            return authRepo.AssertUserAuthRepository().UpdateUserAuth(existingUser, newUser, password);
        }

        public static IUserAuth GetUserAuth(this IAuthRepository authRepo, string userAuthId)
        {
            return authRepo.AssertUserAuthRepository().GetUserAuth(userAuthId);
        }

        public static void DeleteUserAuth(this IAuthRepository authRepo, string userAuthId)
        {
            authRepo.AssertUserAuthRepository().DeleteUserAuth(userAuthId);
        }

        public static void ValidateNewUser(this IUserAuth newUser)
        {
            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException(ErrorMessages.UsernameOrEmailRequired);

            if (!newUser.UserName.IsNullOrEmpty() && !HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
        }

        public static void ValidateNewUser(this IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException(ErrorMessages.UsernameOrEmailRequired);

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                    throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
            }
        }
    }
}
