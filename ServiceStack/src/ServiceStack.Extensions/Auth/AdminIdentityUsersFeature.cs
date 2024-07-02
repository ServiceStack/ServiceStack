#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IIdentityAdminUsersFeature
{
    string AdminRole { get; set; }
    List<string> QueryIdentityUserProperties { get; }
    List<string> HiddenIdentityUserProperties { get; }

    public Task<object?> ValidateCreateUser(IRequest service, AdminCreateUser requestDto);
    public Task<object?> ValidateUpdateUser(IRequest service, AdminUpdateUser requestDto);
    
    public Task BeforeCreateUserAsync(IRequest request, object user);
    public Task AfterCreateUserAsync(IRequest request, object user);
    public Task BeforeUpdateUserAsync(IRequest request, object user);
    public Task AfterUpdateUserAsync(IRequest request, object user);
    public Task BeforeDeleteUserAsync(IRequest request, string userId);
    public Task AfterDeleteUserAsync(IRequest request, string userId);

    object NewUser();
    Task<IdentityResult> UpdateUserAsync(IRequest request, object user);
    Task<IdentityResult> LockUserAsync(IRequest request, object user);
    Task<IdentityResult> UnlockUserAsync(IRequest request, object user);
    Task<IdentityResult> ChangePasswordAsync(IRequest request, object user, string password);
    Task<IdentityResult> CreateUserAsync(IRequest request, object user, string password, List<string>? roles=null);
    Task<IdentityResult> AddRolesAsync(IRequest request, object user, IEnumerable<string> roles);
    Task<IdentityResult> RemoveRolesAsync(IRequest request, object user, IEnumerable<string> roles);

    Task<IEnumerable<object>> SearchUsersAsync(IRequest request, string query, string? orderBy = null, int? skip = null, int? take = null);
    public Task<object?> FindUserByIdAsync(IRequest request, string userId);
    Task<(object, List<string>)> GetUserAndRolesByIdAsync(IRequest request, string userId);
    Task DeleteUserByIdAsync(string requestId);
}

public static class IdentityAdminUsers
{
    public static Task<object?> ValidateCreateUserAsync(IRequest service, AdminCreateUser createUser)
    {
        if (string.IsNullOrEmpty(createUser.UserName))
            throw new ArgumentNullException(nameof(AdminUserBase.UserName));
        if (string.IsNullOrEmpty(createUser.Email))
            throw new ArgumentNullException(nameof(AdminUserBase.Email));
        if (string.IsNullOrEmpty(createUser.Password))
            throw new ArgumentNullException(nameof(AdminUserBase.Password));
        return Task.FromResult(null as object);
    }

    public static Task<object?> ValidateUpdateUserAsync(IRequest service, AdminUpdateUser updateUser) =>
        Task.FromResult(null as object);
}

public class IdentityAdminUsersFeature<TUser, TKey> : IIdentityAdminUsersFeature, IPlugin, IConfigureServices,
    Model.IHasStringId, IPreInitPlugin
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    public string Id { get; set; } = Plugins.AdminIdentityUsers;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public Func<IQueryable<TUser>, string, IQueryable<TUser>> SearchUsersFilter { get; set; } = DefaultSearchUsersFilter;
    
    public string? DefaultOrderBy { get; set; }
    
    public static IQueryable<TUser> DefaultSearchUsersFilter(IQueryable<TUser> q, string query)
    {
        var queryUpper = query.ToUpper();
        q = typeof(TKey) == typeof(string)
            ? q.Where(x => x.NormalizedEmail.Contains(queryUpper) || x.NormalizedUserName.Contains(queryUpper) || x.Id.ToString().Contains(query))
            : q.Where(x => x.NormalizedEmail.Contains(queryUpper) || x.NormalizedUserName.Contains(queryUpper));
        return q;
    }

    public IdentityAuthContextManager<TUser, TKey> Manager => IdentityAuth.Manager as IdentityAuthContextManager<TUser, TKey>
        ?? throw new NotSupportedException("IdentityAuth is not configured");

    public async Task<object?> FindUserByIdAsync(IRequest request, string userId)
    {
        return await Manager.FindUserByIdAsync(userId, request).ConfigAwait();
    }

    public async Task<(object, List<string>)> GetUserAndRolesByIdAsync(IRequest request, string userId)
    {
        return await Manager.GetUserAndRolesByIdAsync(userId, request).ConfigAwait();
    }

    public async Task DeleteUserByIdAsync(string requestId)
    {
        await Manager.DeleteUserByIdAsync(requestId);
    }

    public Func<TUser> CreateUser { get; set; } = () => new TUser { EmailConfirmed = true }; 

    public object NewUser() => CreateUser();
    
    public async Task<IdentityResult> UpdateUserAsync(IRequest request, object user)
    {
        return await Manager.UpdateUserAsync((TUser)user, request).ConfigAwait();
    }

    public async Task<IdentityResult> LockUserAsync(IRequest request, object user)
    {
        var typedUser = (TUser)user;
        return await Manager.LockUserAsync(typedUser, ResolveLockoutDate(typedUser), request).ConfigAwait();
    }

    public async Task<IdentityResult> UnlockUserAsync(IRequest request, object user)
    {
        return await Manager.UnlockUserAsync((TUser)user, request).ConfigAwait();
    }

    public async Task<IdentityResult> ChangePasswordAsync(IRequest request, object user, string password)
    {
        return await Manager.ChangePasswordAsync((TUser)user, password, request).ConfigAwait();
    }

    public async Task<IdentityResult> AddRolesAsync(IRequest request, object user, IEnumerable<string> roles)
    {
        return await Manager.AddRolesAsync((TUser)user, roles, request).ConfigAwait();
    }

    public async Task<IdentityResult> RemoveRolesAsync(IRequest request, object user, IEnumerable<string> roles)
    {
        return await Manager.RemoveRolesAsync((TUser)user, roles, request).ConfigAwait();
    }

    public async Task<IdentityResult> CreateUserAsync(IRequest request, object user, string password, List<string>? roles=null)
    {
        var typedUser = (TUser)user;
        var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
        var result = await userManager.CreateAsync(typedUser, password).ConfigAwait();
        if (result.Succeeded == false)
            return result;
        if (roles is { Count: > 0 })
            await userManager.AddToRolesAsync(typedUser, roles).ConfigAwait();
        
        return result;
    }

    public async Task<IEnumerable<object>> SearchUsersAsync(IRequest request, string query, string? orderBy = null, int? skip = null, int? take = null)
    {
        return await Manager.SearchUsersAsync(query, orderBy, skip, take, request).ConfigAwait();
    }

    /// <summary>
    /// Return only specified UserAuth Properties in AdminQueryUsers
    /// </summary>
    public List<string> QueryIdentityUserProperties { get; set; } =
    [
        nameof(IdentityUser<TKey>.Id),
        nameof(IdentityUser<TKey>.UserName),
        nameof(IdentityUser<TKey>.Email),
        nameof(IdentityUser<TKey>.PhoneNumber),
        nameof(IdentityUser<TKey>.LockoutEnd),
    ];

    /// <summary>
    /// Specify different size media rules when a property should be visible, e.g:
    /// MediaRules.ExtraSmall.Show&lt;UserAuth&gt;(x => new { x.Id, x.Email, x.DisplayName })
    /// </summary>
    public List<MediaRule> QueryMediaRules { get; set; } =
    [
        MediaRules.ExtraSmall.Show<TUser>(x => new { x.Id, x.Email, x.UserName }),
        MediaRules.Small.Show<TUser>(x => new { x.PhoneNumber, x.LockoutEnd })
    ];

    public List<List<InputInfo>> UserFormLayout
    {
        set => FormLayout = Input.FromGridLayout(value);
    }

    /// <summary>
    /// Which User fields can be updated
    /// </summary>
    public List<InputInfo> FormLayout { get; set; } =
    [
        Input.For<TUser>(x => x.UserName, c => c.FieldsPerRow(2)),
        Input.For<TUser>(x => x.Email, c => { 
            c.Type = Input.Types.Email;
            c.FieldsPerRow(2); 
        }),
        Input.For<TUser>(x => x.PhoneNumber, c =>
        {
            c.Type = Input.Types.Tel;
            c.FieldsPerRow(2); 
        })
    ];

    /// <summary>
    /// Which IdentityUser fields that are not returned 
    /// </summary>
    public List<string> HiddenIdentityUserProperties { get; set; } =
    [
        nameof(IdentityUser<TKey>.PasswordHash),
        nameof(IdentityUser<TKey>.SecurityStamp),
        nameof(IdentityUser<TKey>.ConcurrencyStamp),
        nameof(IdentityUser<TKey>.NormalizedUserName),
        nameof(IdentityUser<TKey>.NormalizedEmail),
    ];

    /// <summary>
    /// Which IdentityUser fields that are not modifiable 
    /// </summary>
    public List<string> ReadOnlyIdentityUserProperties { get; set; } =
    [
        nameof(IdentityUser<TKey>.Id),
        nameof(IdentityUser<TKey>.PasswordHash),
        nameof(IdentityUser<TKey>.SecurityStamp),
        nameof(IdentityUser<TKey>.ConcurrencyStamp),
        nameof(IdentityUser<TKey>.NormalizedUserName),
        nameof(IdentityUser<TKey>.NormalizedEmail),
    ];

    /// <summary>
    /// Invoked before user is created
    /// A non-null return (e.g. HttpResult/HttpError) invalidates the request and is used as the API Response instead
    /// </summary>
    public Func<IRequest,AdminCreateUser,Task<object?>> CreateUserValidation { get; set; } = IdentityAdminUsers.ValidateCreateUserAsync;

    /// <summary>
    /// Invoked before user is updated
    /// A non-null return (e.g. HttpResult/HttpError) invalidates the request and is used as the API Response instead
    /// </summary>
    public Func<IRequest,AdminUpdateUser,Task<object?>>  UpdateUserValidation { get; } = IdentityAdminUsers.ValidateUpdateUserAsync;

    /// <summary>
    /// What Locking Date to use when Locking a User (default DateTimeOffset.MaxValue)
    /// </summary>
    public Func<TUser, DateTimeOffset> ResolveLockoutDate { get; set; } = DefaultResolveLockoutDate;
        
    public static DateTimeOffset DefaultResolveLockoutDate(TUser user) => DateTimeOffset.MaxValue; 

    public Task<object?> ValidateCreateUser(IRequest service, AdminCreateUser requestDto)
    {
        return CreateUserValidation(service, requestDto);
    }

    public Task<object?> ValidateUpdateUser(IRequest service, AdminUpdateUser requestDto)
    {
        return UpdateUserValidation(service, requestDto);
    }
    
    /// <summary>
    /// Invoked before a User is created
    /// </summary>
    public Func<IRequest, TUser, Task>? OnBeforeCreateUser { get; set; }

    /// <summary>
    /// Invoked after a User is created
    /// </summary>
    public Func<IRequest, TUser, Task>? OnAfterCreateUser { get; set; }

    /// <summary>
    /// Invoked before a User is updated. (NewUser, ExistingUser, Service)
    /// </summary>
    public Func<IRequest, TUser, Task>? OnBeforeUpdateUser { get; set; }

    /// <summary>
    /// Invoked after a User is updated. (NewUser, ExistingUser, Service)
    /// </summary>
    public Func<IRequest, TUser, Task>? OnAfterUpdateUser { get; set; }

    /// <summary>
    /// Invoked before a User is deleted
    /// </summary>
    public Func<IRequest, string, Task>? OnBeforeDeleteUser { get; set; }

    /// <summary>
    /// Invoked after a User is deleted
    /// </summary>
    public Func<IRequest, string, Task>? OnAfterDeleteUser { get; set; }

    public Task BeforeCreateUserAsync(IRequest request, object user) => 
        OnBeforeCreateUser?.Invoke(request, (TUser)user) ?? Task.CompletedTask;
    public Task AfterCreateUserAsync(IRequest request, object user) => 
        OnAfterCreateUser?.Invoke(request, (TUser)user) ?? Task.CompletedTask;

    public Task BeforeUpdateUserAsync(IRequest request, object user) => 
        OnBeforeUpdateUser?.Invoke(request, (TUser)user) ?? Task.CompletedTask;
    public Task AfterUpdateUserAsync(IRequest request, object user) => 
        OnAfterUpdateUser?.Invoke(request, (TUser)user) ?? Task.CompletedTask;

    public Task BeforeDeleteUserAsync(IRequest request, string userId) =>
        OnBeforeDeleteUser?.Invoke(request, userId) ?? Task.CompletedTask;
    public Task AfterDeleteUserAsync(IRequest request, string userId) =>
        OnAfterDeleteUser?.Invoke(request, userId) ?? Task.CompletedTask;

    public IdentityAdminUsersFeature<TUser, TKey> RemoveFromUserForm(params string[] fieldNames) =>
        RemoveFromUserForm(input => fieldNames.Contains(input.Name));

    public IdentityAdminUsersFeature<TUser, TKey> RemoveFromUserForm(Predicate<InputInfo> match)
    {
        FormLayout.RemoveAll(match);
        return this;
    }

    public IdentityAdminUsersFeature<TUser, TKey> RemoveFromQueryResults(params string[] fieldNames)
    {
        QueryIdentityUserProperties.RemoveAll(fieldNames.Contains);
        return this;
    }

    public IdentityAdminUsersFeature<TUser, TKey> RemoveFields(params string[] fieldNames)
    {
        RemoveFromQueryResults(fieldNames);
        RemoveFromUserForm(fieldNames);
        return this;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature =>
        {
            feature.AddAdminLink(AdminUiFeature.Users, new LinkInfo
            {
                Id = "users",
                Label = "Users",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Users)),
                Show = $"role:{AdminRole}",
            });
        });
    }

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IIdentityAdminUsersFeature>(this);
        services.RegisterService<AdminIdentityUsersService>();
    }

    public void Register(IAppHost appHost)
    {
        if (appHost.GetPlugin<AuthFeature>() == null)
            throw new Exception("IdentityAdminUsersFeature requires " + nameof(AuthFeature));

        appHost.AddToAppMetadata(meta =>
        {
            var nativeTypesMeta = appHost.TryResolve<INativeTypesMetadata>() as NativeTypesMetadata
                ?? new NativeTypesMetadata(HostContext.AppHost.Metadata, new MetadataTypesConfig());
            var metaGen = nativeTypesMeta.GetGenerator();

            var plugin = meta.Plugins.AdminIdentityUsers = new AdminIdentityUsersInfo
            {
                AccessRole = AdminRole,
                Enabled = [],
                IdentityUser = metaGen.ToFlattenedType(typeof(TUser)),
                AllRoles = appHost.Metadata.GetAllRoles(),
                AllPermissions = appHost.Metadata.GetAllPermissions(),
                QueryIdentityUserProperties = QueryIdentityUserProperties,
                QueryMediaRules = QueryMediaRules,
                FormLayout = FormLayout,
            };

            var formPropNames = FormLayout.Select(input => input.Id).ToSet();
            plugin.IdentityUser.Properties.RemoveAll(x => !formPropNames.Contains(x.Name));
        });
    }
}

public class AdminIdentityUsersService(IIdentityAdminUsersFeature feature) : Service
{
    private async Task AssertRequiredRole()
    {
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
    }

    public async Task<object> Get(AdminQueryUsers request)
    {
        await AssertRequiredRole();

        var users = await feature.SearchUsersAsync(Request, request.Query, request.OrderBy, request.Skip, request.Take);

         var userResults = FilterResults(users.Map(ToUserProps), feature.QueryIdentityUserProperties);
        return new AdminUsersResponse {
            Results = userResults,
        };
    }

    private List<Dictionary<string, object?>> FilterResults(List<Dictionary<string, object?>> results, List<string>? includeProps)
    {
        if (includeProps == null)
            return results;

        var to = new List<Dictionary<string, object?>>();

        foreach (var result in results)
        {
            var row = new Dictionary<string, object?>();
            foreach (var includeProp in includeProps)
            {
                row[includeProp] = result.GetValueOrDefault(includeProp);
            }
            to.Add(row);
        }
            
        return to;
    }
        
    public async Task<object> Get(AdminGetUser request)
    {
        await AssertRequiredRole();

        if (request.Id == null)
            throw new ArgumentNullException(nameof(request.Id));

        var (user, roles) = await feature.GetUserAndRolesByIdAsync(Request, request.Id);
        return await CreateUserResponse(user, roles).ConfigAwait();
    }

    public async Task<object> Post(AdminCreateUser request)
    {
        await AssertRequiredRole();
        await feature.ValidateCreateUser(Request!, request).ConfigAwait();
        
        var user = feature.NewUser();
        var props = request.UserAuthProperties ?? new();
        props[nameof(AdminUserBase.UserName)] = request.UserName;
        props[nameof(AdminUserBase.Email)] = request.Email;

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            props[nameof(AdminUserBase.PhoneNumber)] = request.PhoneNumber;
        if (!string.IsNullOrEmpty(request.Password))
            props[nameof(AdminUserBase.Password)] = request.Password;

        props.PopulateInstance(user);
        
        await feature.BeforeCreateUserAsync(Request, user).ConfigAwait();
        var result = await feature.CreateUserAsync(Request, user, request.Password, request.Roles ?? new());
        await feature.AfterCreateUserAsync(Request, user).ConfigAwait();

        result.AssertSucceeded();
        
        return new AdminUserResponse();
    }

    public async Task<object> Put(AdminUpdateUser request)
    {
        await AssertRequiredRole();
        await feature.ValidateUpdateUser(Request!, request).ConfigAwait();
        
        var existingUser = await feature.FindUserByIdAsync(Request, request.Id).ConfigAwait();
        if (existingUser == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

        if (request.LockUser == true)
        {
            var result = await feature.LockUserAsync(Request, existingUser).ConfigAwait();
            result.AssertSucceeded();
        }
        if (request.UnlockUser == true)
        {
            var result = await feature.UnlockUserAsync(Request, existingUser).ConfigAwait();
            result.AssertSucceeded();
        }
            
        if (!string.IsNullOrEmpty(request.Password))
        {
            var result = await feature.ChangePasswordAsync(Request, existingUser, request.Password).ConfigAwait();
            result.AssertSucceeded();
        }
        else
        {
            var updateProps = request.UserAuthProperties ?? new();
            if (!string.IsNullOrEmpty(request.UserName))
                updateProps[nameof(AdminUserBase.UserName)] = request.UserName;
            if (!string.IsNullOrEmpty(request.Email))
                updateProps[nameof(AdminUserBase.Email)] = request.Email;
            if (!string.IsNullOrEmpty(request.PhoneNumber))
                updateProps[nameof(AdminUserBase.PhoneNumber)] = request.PhoneNumber;

            if (updateProps.Count > 0)
            {
                updateProps.PopulateInstance(existingUser);
                await feature.BeforeUpdateUserAsync(Request, existingUser).ConfigAwait();
                var result = await feature.UpdateUserAsync(Request, existingUser);
                result.AssertSucceeded();
                await feature.AfterUpdateUserAsync(Request, existingUser).ConfigAwait();
            }
        }

        if (!request.AddRoles.IsEmpty())
        {
            var result = await feature.AddRolesAsync(Request, existingUser, request.AddRoles).ConfigAwait();
            result.AssertSucceeded();
        }
        if (!request.RemoveRoles.IsEmpty())
        {
            var result = await feature.RemoveRolesAsync(Request, existingUser, request.RemoveRoles).ConfigAwait();
            result.AssertSucceeded();
        }
        
        var (user, roles) = await feature.GetUserAndRolesByIdAsync(Request, request.Id);
        return await CreateUserResponse(user, roles).ConfigAwait();
    }

    public async Task<object> Delete(AdminDeleteUser request)
    {
        await AssertRequiredRole();

        if (request.Id == null)
            throw new ArgumentNullException(nameof(request.Id));
            
        await feature.BeforeDeleteUserAsync(Request, request.Id).ConfigAwait();
            
        await feature.DeleteUserByIdAsync(request.Id);

        await feature.AfterDeleteUserAsync(Request, request.Id).ConfigAwait();

        return new AdminDeleteUserResponse {
            Id = request.Id,
        };
    }

    private async Task<object> CreateUserResponse(object user, List<string> roles)
    {
        if (user == null)
            throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

        var userProps = ToUserProps(user);
        userProps["Roles"] = roles;
        
        return new AdminUserResponse {
            Id = userProps["Id"]!.ToString(),
            Result = userProps,
        };
    }

    private Dictionary<string, object?> ToUserProps(object user)
    {
        var userProps = user.ToObjectDictionary();
        foreach (var removeProp in feature.HiddenIdentityUserProperties)
        {
            userProps.Remove(removeProp);
        }
        return userProps;
    }
}

#endif