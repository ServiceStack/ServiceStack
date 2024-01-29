using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.NativeTypes;

namespace ServiceStack;

public class AdminUsersFeature : IPlugin, IConfigureServices, Model.IHasStringId, IPreInitPlugin, IAfterInitAppHost
{
    public string Id { get; set; } = Plugins.AdminUsers;
    public string AdminRole { get; set; } = RoleNames.Admin;

    /// <summary>
    /// Return only specified UserAuth Properties in AdminQueryUsers
    /// </summary>
    public List<string> QueryUserAuthProperties { get; set; } =
    [
        nameof(UserAuth.Id),
        nameof(UserAuth.UserName),
        nameof(UserAuth.Email),
        nameof(UserAuth.DisplayName),
        nameof(UserAuth.FirstName),
        nameof(UserAuth.LastName),
        nameof(UserAuth.Company),
        nameof(UserAuth.State),
        nameof(UserAuth.Country),
        nameof(UserAuth.ModifiedDate)
    ];

    /// <summary>
    /// Specify different size media rules when a property should be visible, e.g:
    /// MediaRules.ExtraSmall.Show&lt;UserAuth&gt;(x => new { x.Id, x.Email, x.DisplayName })
    /// </summary>
    public List<MediaRule> QueryMediaRules { get; set; } =
    [
        MediaRules.ExtraSmall.Show<UserAuth>(x => new { x.Id, x.Email, x.DisplayName }),
        MediaRules.Small.Show<UserAuth>(x => new { x.Company, x.CreatedDate })
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
        Input.For<UserAuth>(x => x.Email, x => x.Type = Input.Types.Email),
        Input.For<UserAuth>(x => x.UserName),
        Input.For<UserAuth>(x => x.FirstName, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.LastName, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.DisplayName),
        Input.For<UserAuth>(x => x.Company),
        Input.For<UserAuth>(x => x.Address),
        Input.For<UserAuth>(x => x.Address2),
        Input.For<UserAuth>(x => x.City, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.State, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.Country, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.PostalCode, c => c.FieldsPerRow(2)),
        Input.For<UserAuth>(x => x.PhoneNumber, x => x.Type = Input.Types.Tel)
    ];

    /// <summary>
    /// Which UserAuth fields cannot be updated using UserAuthProperties dictionary
    /// </summary>
    public List<string> RestrictedUserAuthProperties { get; set; } =
    [
        nameof(UserAuth.Id),
        nameof(UserAuth.Roles),
        nameof(UserAuth.Permissions),
        nameof(UserAuth.CreatedDate),
        nameof(UserAuth.ModifiedDate),
        nameof(UserAuth.PasswordHash),
        nameof(UserAuth.Salt),
        nameof(UserAuth.DigestHa1Hash)
    ];

    /// <summary>
    /// Invoked before user is created or updated.
    /// A non-null return (e.g. HttpResult/HttpError) invalidates the request and is used as the API Response instead
    /// </summary>
    public ValidateAsyncFn ValidateFn { get; set; }

    /// <summary>
    /// Invoked before a User is created
    /// </summary>
    public Func<IUserAuth, Service, Task> OnBeforeCreateUser { get; set; }

    /// <summary>
    /// Invoked after a User is created
    /// </summary>
    public Func<IUserAuth, Service, Task> OnAfterCreateUser { get; set; }

    /// <summary>
    /// Invoked before a User is updated. (NewUser, ExistingUser, Service)
    /// </summary>
    public Func<IUserAuth, IUserAuth, Service, Task> OnBeforeUpdateUser { get; set; }

    /// <summary>
    /// Invoked after a User is updated. (NewUser, ExistingUser, Service)
    /// </summary>
    public Func<IUserAuth, IUserAuth, Service, Task> OnAfterUpdateUser { get; set; }

    /// <summary>
    /// Invoked before a User is deleted
    /// </summary>
    public Func<string, Service, Task> OnBeforeDeleteUser { get; set; }

    /// <summary>
    /// Invoked after a User is deleted
    /// </summary>
    public Func<string, Service, Task> OnAfterDeleteUser { get; set; }

    /// <summary>
    /// Whether to execute OnRegistered Events for Users created through Admin UI (default true).
    /// </summary>
    public bool ExecuteOnRegisteredEventsForCreatedUsers { get; set; } = true;

    public AdminUsersFeature RemoveFromUserForm(params string[] fieldNames) =>
        RemoveFromUserForm(input => fieldNames.Contains(input.Name));
    public AdminUsersFeature RemoveFromUserForm(Predicate<InputInfo> match)
    {
        FormLayout.RemoveAll(match);
        return this;
    }
        
    public AdminUsersFeature RemoveFromQueryResults(params string[] fieldNames)
    {
        QueryUserAuthProperties.RemoveAll(fieldNames.Contains);
        return this;
    }
        
    public AdminUsersFeature RemoveFields(params string[] fieldNames)
    {
        RemoveFromQueryResults(fieldNames);
        RemoveFromUserForm(fieldNames);
        return this;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Users, new LinkInfo {
                Id = "users",
                Label = "Users",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Users)),
                Show = $"role:{AdminRole}",
            });
        });
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminUsersService));
    }

    public void Register(IAppHost appHost)
    {
        appHost.AddToAppMetadata(meta => {
            var host = (ServiceStackHost) appHost;
            var authRepo = host.GetAuthRepository();
            if (authRepo == null)
                return;
                
            using (authRepo as IDisposable)
            {
                var userAuth = authRepo is ICustomUserAuth customUserAuth
                    ? customUserAuth.CreateUserAuth()
                    : new UserAuth();

                var nativeTypesMeta = appHost.TryResolve<INativeTypesMetadata>() as NativeTypesMetadata 
                    ?? new NativeTypesMetadata(appHost.Metadata, new MetadataTypesConfig());
                var metaGen = nativeTypesMeta.GetGenerator();

                var plugin = meta.Plugins.AdminUsers = new AdminUsersInfo {
                    AccessRole = AdminRole,
                    Enabled = [],
                    UserAuth = metaGen.ToFlattenedType(userAuth.GetType()),
                    AllRoles = appHost.Metadata.GetAllRoles(),
                    AllPermissions = appHost.Metadata.GetAllPermissions(),
                    QueryUserAuthProperties = QueryUserAuthProperties,
                    QueryMediaRules = QueryMediaRules, 
                    FormLayout = FormLayout,
                };
                if (authRepo is IQueryUserAuth)
                    plugin.Enabled.Add("query");
                if (authRepo is ICustomUserAuth)
                    plugin.Enabled.Add("custom");
                if (authRepo is IManageRoles)
                    plugin.Enabled.Add("manageRoles");

                if (FormLayout != null)
                {
                    var formPropNames = FormLayout.Select(input => input.Id).ToSet();
                    plugin.UserAuth.Properties.RemoveAll(x => !formPropNames.Contains(x.Name));
                }

                if (meta.Plugins.Auth == null)
                    throw new Exception(nameof(AdminUsersFeature) + " requires " + nameof(AuthFeature));
            }
        });
    }

    public void AfterInit(IAppHost appHost)
    {
        var authRepo = ((ServiceStackHost)appHost).GetAuthRepositoryAsync();
        using (authRepo as IDisposable)
        {
            if (authRepo == null)
                throw new Exception("UserAuth Repository is required to use " + nameof(AdminUsersFeature));
        }
    }
}
