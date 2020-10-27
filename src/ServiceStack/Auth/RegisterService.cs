using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class FullRegistrationValidator : RegistrationValidator
    {
        public FullRegistrationValidator() { RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty()); }
    }

    public class RegistrationValidator : AbstractValidator<Register>
    {
        public RegistrationValidator()
        {
            RuleSet(
                ApplyTo.Post,
                () =>
                {
                    RuleFor(x => x.Password).NotEmpty();
                    RuleFor(x => x.ConfirmPassword)
                        .Equal(x => x.Password)
                        .When(x => x.ConfirmPassword != null)
                        .WithErrorCode(nameof(ErrorMessages.PasswordsShouldMatch))
                        .WithMessage(ErrorMessages.PasswordsShouldMatch.Localize(base.Request));
                    RuleFor(x => x.UserName).NotEmpty().When(x => x.Email.IsNullOrEmpty());
                    RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.UserName)
                        .MustAsync(async (x,token) =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(base.Request);
#if NET472 || NETSTANDARD2_0
                            await using (authRepo as IAsyncDisposable)
#else
                            using (authRepo as IDisposable)
#endif
                            {
                                return await authRepo.GetUserAuthByUserNameAsync(x).ConfigAwait() == null;
                            }
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.UsernameAlreadyExists.Localize(base.Request))
                        .When(x => !x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.Email)
                        .MustAsync(async (x,token) =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(base.Request);
#if NET472 || NETSTANDARD2_0
                            await using (authRepo as IAsyncDisposable)
#else
                            using (authRepo as IDisposable)
#endif
                            {
                                return x.IsNullOrEmpty() || await authRepo.GetUserAuthByUserNameAsync(x).ConfigAwait() == null;
                            }
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.EmailAlreadyExists.Localize(base.Request))
                        .When(x => !x.Email.IsNullOrEmpty());
                });
            RuleSet(
                ApplyTo.Put,
                () =>
                {
                    RuleFor(x => x.UserName).NotEmpty().When(x => x.Email.IsNullOrEmpty());
                    RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => x.UserName.IsNullOrEmpty());
                });
        }
    }

    [ErrorView(nameof(Register.ErrorView))]
    [DefaultRequest(typeof(Register))]
    public class RegisterService : Service
    {
        public static ValidateFn ValidateFn { get; set; }
        
        public static bool AllowUpdates { get; set; }

        public IValidator<Register> RegistrationValidator { get; set; }

        public IAuthEvents AuthEvents { get; set; }

        /// <summary>
        /// Update an existing registration
        /// </summary>
        [Obsolete("Use PostAsync")]
        public Task<object> PutAsync(Register request)
        {
            return PostAsync(request);
        }

        /// <summary>
        /// Create new Registration
        /// </summary>
        [Obsolete("Use PostAsync")]
        public object Post(Register request)
        {
            try
            {
                var task = PostAsync(request);
                var response = task.GetResult();
                return response;
            }
            catch (Exception e)
            {
                throw e.UnwrapIfSingleException();
            }
        }
        
        /// <summary>
        /// Create new Registration
        /// </summary>
        public async Task<object> PostAsync(Register request)
        {
            var returnUrl = Request.GetReturnUrl();

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
                if (!string.IsNullOrEmpty(returnUrl))
                    authFeature.ValidateRedirectLinks(Request, returnUrl);
            }
            
            var validateResponse = ValidateFn?.Invoke(this, HttpMethods.Post, request);
            if (validateResponse != null)
                return validateResponse;

            RegisterResponse response = null;
            var session = await this.GetSessionAsync();
            var newUserAuth = ToUserAuth(AuthRepositoryAsync as ICustomUserAuth, request);

            var existingUser = session.IsAuthenticated 
                ? await AuthRepositoryAsync.GetUserAuthAsync(session, null).ConfigAwait() 
                : null;
            var registerNewUser = existingUser == null;

            if (!registerNewUser && !AllowUpdates)
                throw new NotSupportedException(ErrorMessages.RegisterUpdatesDisabled.Localize(Request));

            if (!HostContext.AppHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync) //Already gets run
                && RegistrationValidator != null)
            {
                await RegistrationValidator.ValidateAndThrowAsync(request, registerNewUser ? ApplyTo.Post : ApplyTo.Put).ConfigAwait();
            }
            
            var user = registerNewUser
                ? await AuthRepositoryAsync.CreateUserAuthAsync(newUserAuth, request.Password).ConfigAwait()
                : await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUserAuth, request.Password).ConfigAwait();

            if (registerNewUser)
            {
                session.PopulateSession(user);
                session.OnRegistered(Request, session, this);
                if (session is IAuthSessionExtended sessionExt)
                    await sessionExt.OnRegisteredAsync(Request, session, this).ConfigAwait();
                AuthEvents?.OnRegistered(this.Request, session, this);
                if (AuthEvents is IAuthEventsAsync asyncEvents)
                    await asyncEvents.OnRegisteredAsync(this.Request, session, this).ConfigAwait();
            }

            if (request.AutoLogin.GetValueOrDefault())
            {
#if NETSTANDARD2_0 || NET472
                await using var authService = base.ResolveService<AuthenticateService>();
#else
                using var authService = base.ResolveService<AuthenticateService>();
#endif                
                var authResponse = await authService.PostAsync(
                    new Authenticate {
                        provider = CredentialsAuthProvider.Name,
                        UserName = request.UserName ?? request.Email,
                        Password = request.Password,
                    });

                if (authResponse is IHttpError)
                    throw (Exception)authResponse;

                if (authResponse is AuthenticateResponse typedResponse)
                {
                    response = new RegisterResponse
                    {
                        SessionId = typedResponse.SessionId,
                        UserName = typedResponse.UserName,
                        ReferrerUrl = typedResponse.ReferrerUrl,
                        UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                        BearerToken = typedResponse.BearerToken,
                        RefreshToken = typedResponse.RefreshToken,
                    };
                }
            }

            if (response == null)
            {
                response = new RegisterResponse
                {
                    UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    ReferrerUrl = Request.GetReturnUrl(),
                    UserName = session.UserName,
                };
            }

            var isHtml = Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            if (isHtml)
            {
                if (string.IsNullOrEmpty(returnUrl))
                    return response;

                return new HttpResult(response)
                {
                    Location = returnUrl
                };
            }

            return response;
        }

        public static IUserAuth ToUserAuth(ICustomUserAuth customUserAuth, Register request)
        {
            var to = customUserAuth != null
                ? customUserAuth.CreateUserAuth()
                : new UserAuth();

            to.PopulateWithNonDefaultValues(request);
            to.PrimaryEmail = request.Email;
            return to;
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
        /// </summary>
        public object UpdateUserAuth(Register request)
        {
            if (!HostContext.AppHost.GlobalRequestFiltersAsyncArray.Contains(ValidationFilters.RequestFilterAsync)) //Already gets run
            {
                RegistrationValidator.ValidateAndThrow(request, ApplyTo.Put);
            }

            var response = ValidateFn?.Invoke(this, HttpMethods.Put, request);
            if (response != null)
                return response;

            var session = this.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
            using (authRepo as IDisposable)
            {
                var existingUser = authRepo.GetUserAuth(session, null);
                if (existingUser == null)
                    throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

                var newUserAuth = ToUserAuth(authRepo as ICustomUserAuth, request);
                authRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

                return new RegisterResponse
                {
                    UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
                };
            }
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
        /// </summary>
        public async Task<object> UpdateUserAuthAsync(Register request)
        {
            if (!HostContext.AppHost.GlobalRequestFiltersAsyncArray.Contains(ValidationFilters.RequestFilterAsync)) //Already gets run
            {
                await RegistrationValidator.ValidateAndThrowAsync(request, ApplyTo.Put).ConfigAwait();
            }

            var response = ValidateFn?.Invoke(this, HttpMethods.Put, request);
            if (response != null)
                return response;

            var session = await this.GetSessionAsync().ConfigAwait();

            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(base.Request);
#if NET472 || NETSTANDARD2_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                var existingUser = await authRepo.GetUserAuthAsync(session, null).ConfigAwait();
                if (existingUser == null)
                    throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));

                var newUserAuth = ToUserAuth(authRepo as ICustomUserAuth, request);
                await authRepo.UpdateUserAuthAsync(existingUser, newUserAuth, request.Password).ConfigAwait();

                return new RegisterResponse
                {
                    UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
                };
            }
        }
    }
}