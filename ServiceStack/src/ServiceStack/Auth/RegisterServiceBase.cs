using System;
using System.Globalization;
using System.Threading.Tasks;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

[IgnoreServices]
public class FullRegistrationValidator : RegistrationValidator
{
    public FullRegistrationValidator() { RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty()); }
}

[IgnoreServices]
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
                        await using (authRepo as IAsyncDisposable)
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
                        await using (authRepo as IAsyncDisposable)
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

/// <summary>
/// Register Base class for IAuthRepository / IUserAuth users
/// </summary>
public abstract class RegisterUserAuthServiceBase : RegisterServiceBase
{
    protected virtual IUserAuth ToUser(Register request)
    {
        var to = AuthRepositoryAsync is ICustomUserAuth customUserAuth
            ? customUserAuth.CreateUserAuth()
            : new UserAuth();

        to.PopulateWithNonDefaultValues(request);
        to.PrimaryEmail = request.Email;
        return to;
    }

    protected virtual async Task<bool> UserExistsAsync(IAuthSession session) => 
        session.IsAuthenticated && await AuthRepositoryAsync.GetUserAuthAsync(session, null).ConfigAwait() != null;

    private IValidator<Register> registrationValidator;
    public IValidator<Register> RegistrationValidator
    {
        get => registrationValidator ?? Request.TryResolve<IValidator<Register>>();
        set => registrationValidator = value;
    }

    protected virtual async Task ValidateAndThrowAsync(Register request)
    {
        var validator = RegistrationValidator ?? new RegistrationValidator();
        if (validator is IRequiresRequest requiresRequest)
            requiresRequest.Request ??= Request;
        await validator.ValidateAndThrowAsync(request, ApplyTo.Post).ConfigAwait();
    }

    protected virtual async Task RegisterNewUserAsync(IAuthSession session, IUserAuth user)
    {
        var authEvents = Request.TryResolve<IAuthEvents>();
        session.UserAuthId = user.Id.ToString(CultureInfo.InvariantCulture);
        session.PopulateSession(user);
        session.OnRegistered(Request, session, this);
        if (session is IAuthSessionExtended sessionExt)
            await sessionExt.OnRegisteredAsync(Request, session, this).ConfigAwait();
        authEvents?.OnRegistered(this.Request, session, this);
        if (authEvents is IAuthEventsAsync asyncEvents)
            await asyncEvents.OnRegisteredAsync(this.Request, session, this).ConfigAwait();
    }
}
    
/// <summary>
/// Register base class containing common functionality for generic
/// </summary>
public abstract class RegisterServiceBase : Service
{
    protected virtual async Task<object> CreateRegisterResponse(IAuthSession session, string userName, string password, bool? autoLogin=null)
    {
        var authFeature = GetPlugin<AuthFeature>();
        var returnUrl = Request.GetReturnUrl();
        if (!string.IsNullOrEmpty(returnUrl))
            authFeature?.ValidateRedirectLinks(Request, returnUrl);

        RegisterResponse response = null;
        if (autoLogin.GetValueOrDefault())
        {
#if !NETFRAMEWORK || NET472
            await using var authService = base.ResolveService<AuthenticateService>();
#else
                using var authService = base.ResolveService<AuthenticateService>();
#endif
            var authResponse = await authService.PostAsync(
                new Authenticate {
                    provider = AuthenticateService.CredentialsProvider,
                    UserName = userName,
                    Password = password,
                });

            if (authResponse is IHttpError)
                throw (Exception) authResponse;

            if (authResponse is IHttpResult { Response: AuthenticateResponse dto } httpResult)
            {
                // Return response inside original HttpResult Cookies
                httpResult.Response = ToRegisterResponse(dto, session.UserAuthId);
                return httpResult;
            }

            if (authResponse is AuthenticateResponse typedResponse)
            {
                response = ToRegisterResponse(typedResponse, session.UserAuthId);

                if (authFeature?.RegisterResponseDecorator != null)
                {
                    var ctx = new RegisterFilterContext {
                        RegisterService = this,
                        Register = Request.Dto as Register,
                        RegisterResponse = response,
                        ReferrerUrl = returnUrl,
                        Session = session,
                    };
                    var result = authFeature.RegisterResponseDecorator(ctx);
                    return result;
                }
            }
        }

        if (response == null)
        {
            response = new RegisterResponse {
                UserId = session.UserAuthId,
                ReferrerUrl = Request.GetReturnUrl(),
                UserName = session.UserName,
            };
        }

        var isHtml = Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
        if (isHtml)
        {
            if (string.IsNullOrEmpty(returnUrl))
                return response;

            return new HttpResult(response) {
                Location = returnUrl
            };
        }

        return response;
    }

    RegisterResponse ToRegisterResponse(AuthenticateResponse typedResponse, string userId) => new()
    {
        SessionId = typedResponse.SessionId,
        UserName = typedResponse.UserName,
        ReferrerUrl = typedResponse.ReferrerUrl,
        UserId = userId,
        BearerToken = typedResponse.BearerToken,
        RefreshToken = typedResponse.RefreshToken,
        RefreshTokenExpiry = typedResponse.RefreshTokenExpiry,
        Roles = typedResponse.Roles,
        Permissions = typedResponse.Permissions,
        Meta = typedResponse.Meta,
    };
}