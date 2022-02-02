using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.NativeTypes;
using ServiceStack.Text;

namespace ServiceStack.Admin
{
    public class AdminUsersFeature : IPlugin, Model.IHasStringId, IPreInitPlugin, IAfterInitAppHost
    {
        public string Id { get; set; } = Plugins.AdminUsers;
        public string AdminRole { get; set; } = RoleNames.Admin;

        /// <summary>
        /// Return only specified UserAuth Properties in AdminQueryUsers
        /// </summary>
        public List<string> QueryUserAuthProperties { get; set; } = new() {
            nameof(UserAuth.Id),
            nameof(UserAuth.UserName),
            nameof(UserAuth.Email),
            nameof(UserAuth.DisplayName),
            nameof(UserAuth.FirstName),
            nameof(UserAuth.LastName),
            nameof(UserAuth.Company),
            nameof(UserAuth.State),
            nameof(UserAuth.Country),
            nameof(UserAuth.ModifiedDate),
        };

        /// <summary>
        /// Specify different size media rules when a property should be visible, e.g:
        /// MediaRules.ExtraSmall.Show&lt;UserAuth&gt;(x => new { x.Id, x.Email, x.DisplayName })
        /// </summary>
        public List<MediaRule> QueryMediaRules { get; set; } = new() {
            MediaRules.ExtraSmall.Show<UserAuth>(x => new { x.Id, x.Email, x.DisplayName }),
            MediaRules.Small.Show<UserAuth>(x => new { x.Company, x.CreatedDate }),
        };

        /// <summary>
        /// Which User fields can be updated
        /// </summary>
        public List<List<InputInfo>> UserFormLayout { get; set; } = new()
        {
            new(){ Input.For<UserAuth>(x => x.Email, x => x.Type = Input.Types.Email) },
            new(){ Input.For<UserAuth>(x => x.UserName) },
            new() {
                Input.For<UserAuth>(x => x.FirstName),
                Input.For<UserAuth>(x => x.LastName),
            },
            new(){ Input.For<UserAuth>(x => x.DisplayName) },
            new(){ Input.For<UserAuth>(x => x.Company) },
            new(){ Input.For<UserAuth>(x => x.Address) },
            new(){ Input.For<UserAuth>(x => x.Address2) },
            new() {
                Input.For<UserAuth>(x => x.City),
                Input.For<UserAuth>(x => x.State),
            },
            new() {
                Input.For<UserAuth>(x => x.Country),
                Input.For<UserAuth>(x => x.PostalCode),
            },
            new(){ Input.For<UserAuth>(x => x.PhoneNumber, x => x.Type = Input.Types.Tel) },
        };

        /// <summary>
        /// Which UserAuth fields cannot be updated using UserAuthProperties dictionary
        /// </summary>
        public List<string> RestrictedUserAuthProperties { get; set; } = new() {
            nameof(UserAuth.Id),
            nameof(UserAuth.Roles),
            nameof(UserAuth.Permissions),
            nameof(UserAuth.CreatedDate),
            nameof(UserAuth.ModifiedDate),
            nameof(UserAuth.PasswordHash),
            nameof(UserAuth.Salt),
            nameof(UserAuth.DigestHa1Hash),
        };

        public HtmlModule HtmlModule { get; set; } = new("/modules/admin-ui", "/admin-ui");

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

        public AdminUsersFeature EachUserFormGroup(Action<List<InputInfo>, int> filter)
        {
            for (var i = 0; i < UserFormLayout.Count; i++)
            {
                var row = UserFormLayout[i];
                filter(row, i);
            }
            return this;
        }

        public AdminUsersFeature EachUserFormField(Action<InputInfo> filter)
        {
            UserFormLayout.SelectMany(row => row.ToArray()).Each(filter);
            return this;
        }

        public List<T> MapUserFormField<T>(Func<InputInfo,T> filter) => 
            UserFormLayout.SelectMany(row => row.ToArray()).Map(filter);

        public AdminUsersFeature RemoveFromUserForm(Predicate<InputInfo> match)
        {
            UserFormLayout.ForEach(row => row.RemoveAll(match));
            return this;
        }

        public AdminUsersFeature RemoveFromUserForm(params string[] fieldNames) =>
            RemoveFromUserForm(input => fieldNames.Contains(input.Name));
        
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
            appHost.ConfigurePlugin<UiFeature>(feature =>
            {
                if (HtmlModule != null)
                {
                    feature.HtmlModules.Add(HtmlModule);
                    feature.Info.AdminLinks.Add(new LinkInfo
                    {
                        Id = "users",
                        Label = "Manage Users",
                        Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Users)),
                    });
                }
            });
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService(typeof(AdminUsersService));
           
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
                        ?? new NativeTypesMetadata(HostContext.AppHost.Metadata, new MetadataTypesConfig());
                    var metaGen = nativeTypesMeta.GetGenerator();

                    var plugin = meta.Plugins.AdminUsers = new AdminUsersInfo {
                        AccessRole = AdminRole,
                        Enabled = new List<string>(),
                        UserAuth = metaGen.ToFlattenedType(userAuth.GetType()),
                        AllRoles = HostContext.Metadata.GetAllRoles(),
                        AllPermissions = HostContext.Metadata.GetAllPermissions(),
                        QueryUserAuthProperties = QueryUserAuthProperties,
                        QueryMediaRules = QueryMediaRules, 
                        UserFormLayout = UserFormLayout, 
                    };
                    if (authRepo is IQueryUserAuth)
                        plugin.Enabled.Add("query");
                    if (authRepo is ICustomUserAuth)
                        plugin.Enabled.Add("custom");
                    if (authRepo is IManageRoles)
                        plugin.Enabled.Add("manageRoles");

                    if (UserFormLayout != null)
                    {
                        var formPropNames = MapUserFormField(input => input.Id).ToSet();
                        plugin.UserAuth.Properties.RemoveAll(x => !formPropNames.Contains(x.Name));
                    }

                    if (meta.Plugins.Auth == null)
                        throw new Exception(nameof(AdminUsersFeature) + " requires " + nameof(AuthFeature));

                    if (HtmlModule != null)
                    {
                        meta.Plugins.Auth.RoleLinks[RoleNames.Admin] = new List<LinkInfo>
                        {
                            new() { Href = "../admin-ui", Label = "Dashboard", Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Home)) },
                            new() { Href = "../admin-ui/users", Label = "Manage Users", Icon = Svg.ImageSvg(Svg.GetImage(Svg.Icons.Users, "currentColor")) },
                        };
                    }
                }
            });
        }

        public void AfterInit(IAppHost appHost)
        {
            var authRepo = ((ServiceStackHost)appHost).GetAuthRepository();
            using (authRepo as IDisposable)
            {
                if (authRepo == null)
                    throw new Exception("UserAuth Repository is required to use " + nameof(AdminUsersFeature));
            }
        }
    }
        
/* Allow metadata discovery & code-gen in *.Source.csproj builds */    
#if !SOURCE
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public partial class AdminUsersService {}
#endif

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
                var tuple = await manageRoles.GetRolesAndPermissionsAsync( user.Id.ToString());
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
}
