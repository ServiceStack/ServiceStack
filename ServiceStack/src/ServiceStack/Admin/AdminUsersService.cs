using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.Admin;

public partial class AdminUsersService : Service
{
    private async Task<AdminUsersFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<AdminUsersFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        return feature;
    }

    private async Task<object> Validate(AdminUserBase request)
    {
        var feature = await AssertRequiredRole();

        var authFeature = GetPlugin<AuthFeature>();
        if (authFeature != null)
        {
            if (authFeature.SaveUserNamesInLowerCase)
            {
                if (request.UserName != null)
                    request.UserName = request.UserName.ToLower();
                if (request.Email != null)
                    request.Email = request.Email.ToLower();
            }
        }

        if (feature.ValidateFn == null)
            return null;

        var validateResponse = await feature.ValidateFn(this, HttpMethods.Post, request);
        return validateResponse;
    }

    public async Task<object> Get(AdminGetUser request)
    {
        await AssertRequiredRole();

        if (request.Id == null)
            throw new ArgumentNullException(nameof(request.Id));
            
        var existingUser = await AuthRepositoryAsync.GetUserAuthAsync(request.Id);
        var existingUserDetails = await AuthRepositoryAsync.GetUserAuthDetailsAsync(request.Id);
        return await CreateUserResponse(existingUser, existingUserDetails);
    }

    public async Task<object> Get(AdminQueryUsers request)
    {
        await AssertRequiredRole();

        // Do exact search by Username/Email if Auth Repo doesn't support querying
        if (!(AuthRepositoryAsync is IQueryUserAuthAsync) && !(AuthRepository is IQueryUserAuth))
        {
            var user = await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.Query);
            return new AdminUsersResponse {
                Results = new List<Dictionary<string, object>> { ToUserProps(user) }
            };
        }
            
        var users = !string.IsNullOrEmpty(request.Query)
            ? await AuthRepositoryAsync.SearchUserAuthsAsync(request.Query, request.OrderBy, request.Skip, request.Take)
            : await AuthRepositoryAsync.GetUserAuthsAsync(request.OrderBy, request.Skip, request.Take);

        var feature = AssertPlugin<AdminUsersFeature>();
        var userResults = FilterResults(users.Map(ToUserProps), feature.QueryUserAuthProperties);
        return new AdminUsersResponse {
            Results = userResults,
        };
    }

    private List<Dictionary<string, object>> FilterResults(List<Dictionary<string, object>> results, List<string> includeProps)
    {
        if (includeProps == null)
            return results;

        var to = new List<Dictionary<string, object>>();

        foreach (var result in results)
        {
            var row = new Dictionary<string, object>();
            foreach (var includeProp in includeProps)
            {
                row[includeProp] = result.TryGetValue(includeProp, out var value)
                    ? value
                    : null;
            }
            to.Add(row);
        }
            
        return to;
    }
        
    public async Task<object> Post(AdminCreateUser request)
    {
        var validateResponse = await Validate(request);
        if (validateResponse != null)
            return validateResponse;

        if (await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.UserName).ConfigAwait() != null)
            throw HttpError.Validation("AlreadyExists", ErrorMessages.UsernameAlreadyExists.Localize(base.Request), nameof(request.UserName));
        if (await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.Email).ConfigAwait() != null)
            throw HttpError.Validation("AlreadyExists", ErrorMessages.EmailAlreadyExists.Localize(base.Request), nameof(request.Email));

        var newUser = PopulateUserAuth(NewUserAuth(), request);

        var feature = AssertPlugin<AdminUsersFeature>();
        if (feature.OnBeforeCreateUser != null)
            await feature.OnBeforeCreateUser(newUser, this);
            
        IUserAuth user = await AuthRepositoryAsync.CreateUserAuthAsync(newUser, request.Password).ConfigAwait();
        if (!request.Roles.IsEmpty() || !request.Permissions.IsEmpty())
        {
            await AuthRepositoryAsync.AssignRolesAsync(user, request.Roles, request.Permissions);
        }

        if (feature.OnAfterCreateUser != null)
            await feature.OnAfterCreateUser(newUser, this);

        if (feature.ExecuteOnRegisteredEventsForCreatedUsers)
        {
            var session = user.CreateNewSession(Request);
            var authEvents = TryResolve<IAuthEvents>();
            if (authEvents != null && session != null)
                await authEvents.ExecuteOnRegisteredUserEventsAsync(session, this);
        }

        return await CreateUserResponse(user);
    }

    private IUserAuth NewUserAuth() => AuthRepositoryAsync is ICustomUserAuth customUserAuth ? customUserAuth.CreateUserAuth() : new UserAuth();

    public async Task<object> Put(AdminUpdateUser request)
    {
        if (request.Id == null)
            throw new ArgumentNullException(nameof(request.Id));
            
        var validateResponse = await Validate(request);
        if (validateResponse != null)
            return validateResponse;

        var existingUser = await AuthRepositoryAsync.GetUserAuthAsync(request.Id);
        if (existingUser == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

        var newUser = PopulateUserAuth(NewUserAuth().PopulateWith(existingUser), request);

        if (request.LockUser == true)
        {
            newUser.LockedDate = DateTime.UtcNow;
        }
        if (request.UnlockUser == true)
        {
            newUser.LockedDate = null;
            newUser.InvalidLoginAttempts = 0;
        }
            
        var feature = AssertPlugin<AdminUsersFeature>();
        if (feature.OnBeforeUpdateUser != null)
            await feature.OnBeforeUpdateUser(newUser, existingUser, this);

        if (!string.IsNullOrEmpty(request.Password))
            newUser = await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUser, request.Password);
        else
            newUser = await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUser);

        if (!request.AddRoles.IsEmpty() || !request.AddPermissions.IsEmpty())
            await AuthRepositoryAsync.AssignRolesAsync(newUser, request.AddRoles, request.AddPermissions);
        if (!request.RemoveRoles.IsEmpty() || !request.RemovePermissions.IsEmpty())
            await AuthRepositoryAsync.UnAssignRolesAsync(newUser, request.RemoveRoles, request.RemovePermissions);

        if (feature.OnAfterUpdateUser != null)
            await feature.OnAfterUpdateUser(newUser, existingUser, this);

        return await CreateUserResponse(newUser);
    }
        
    public async Task<object> Delete(AdminDeleteUser request)
    {
        var feature = await AssertRequiredRole();
        if (request.Id == null)
            throw new ArgumentNullException(nameof(request.Id));
            
        if (feature.OnBeforeDeleteUser != null)
            await feature.OnBeforeDeleteUser(request.Id, this);
            
        await AuthRepositoryAsync.DeleteUserAuthAsync(request.Id);

        if (feature.OnAfterDeleteUser != null)
            await feature.OnAfterDeleteUser(request.Id, this);

        return new AdminDeleteUserResponse {
            Id = request.Id,
        };
    }

    private async Task<object> CreateUserResponse(IUserAuth user, List<IUserAuthDetails> existingUserDetails = null)
    {
        if (user == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));
            
        var userProps = await GetUserPropsAndRoles(user);

        return new AdminUserResponse {
            Id = user.Id.ToString(),
            Result = userProps,
            Details = existingUserDetails?.Map(x => x.ToObjectDictionary()), 
        };
    }

    private async Task<Dictionary<string, object>> GetUserPropsAndRoles(IUserAuth user)
    {
        if (AuthRepositoryAsync is IManageRolesAsync manageRoles)
        {
            var tuple = await manageRoles.GetLocalRolesAndPermissionsAsync(user.Id.ToString());
            user.Roles = tuple.Item1.ToList();
            user.Permissions = tuple.Item2.ToList();
        }

        return ToUserProps(user);
    }

    private static Dictionary<string, object> ToUserProps(IUserAuth user)
    {
        var userProps = user.ToObjectDictionary();
        userProps.Remove(nameof(IUserAuth.PasswordHash));
        userProps.Remove(nameof(IUserAuth.Salt));

        if (userProps.TryGetValue(nameof(IUserAuth.Meta), out var meta) && meta is Dictionary<string,string> metaMap)
        {
            if (metaMap.TryGetValue(nameof(IAuthSession.ProfileUrl), out var profileUrl))
                userProps[nameof(IAuthSession.ProfileUrl)] = profileUrl;
        }

        return userProps;
    }

    private IUserAuth PopulateUserAuth(IUserAuth to, AdminUserBase request)
    {
        to.PopulateWithNonDefaultValues(request);
        if (!string.IsNullOrEmpty(request.Email))
            to.PrimaryEmail = request.Email;

        if (to.DisplayName == null && to.FirstName != null)
            to.DisplayName = to.FirstName + (to.LastName != null ? " " + to.LastName : "");

        var userAuthProps = request.UserAuthProperties;
        if (userAuthProps != null)
        {
            userAuthProps = new Dictionary<string, string>(request.UserAuthProperties, StringComparer.OrdinalIgnoreCase);
            var feature = AssertPlugin<AdminUsersFeature>();
            foreach (var restrictedProp in feature.RestrictedUserAuthProperties)
            {
                userAuthProps.RemoveKey(restrictedProp);
            }
        }

        userAuthProps.PopulateInstance(to);

        var hasProfileUrlProp = TypeProperties.Get(to.GetType()).PropertyMap.ContainsKey(nameof(IAuthSession.ProfileUrl));
        if (request.ProfileUrl != null && !hasProfileUrlProp)
        {
            to.Meta ??= new Dictionary<string, string>();
            to.Meta[nameof(IAuthSession.ProfileUrl)] = request.ProfileUrl;
        }
            
        return to;
    }
        
}