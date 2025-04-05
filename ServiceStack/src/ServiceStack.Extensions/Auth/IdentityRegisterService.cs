using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.Auth;

[IgnoreServices]
public class IdentityRegistrationValidator<TUser,TKey> : AbstractValidator<Register>
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>
{
    public IdentityRegistrationValidator()
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
                    .MustAsync(async (x, token) =>
                    {
                        var userManager = Request.TryResolve<UserManager<TUser>>();
                        return await userManager.FindByEmailAsync(x).ConfigAwait() == null;
                    })
                    .WithErrorCode("AlreadyExists")
                    .WithMessage(ErrorMessages.UsernameAlreadyExists.Localize(base.Request))
                    .When(x => !x.UserName.IsNullOrEmpty());
                RuleFor(x => x.Email)
                    .MustAsync(async (x, token) =>
                    {
                        var userManager = Request.TryResolve<UserManager<TUser>>();
                        return await userManager.FindByEmailAsync(x).ConfigAwait() == null;
                    })
                    .WithErrorCode("AlreadyExists")
                    .WithMessage(ErrorMessages.EmailAlreadyExists.Localize(base.Request))
                    .When(x => !x.Email.IsNullOrEmpty());
            });
    }
}

public abstract class IdentityRegisterServiceBase<TUser>(UserManager<TUser> userManager)
    : IdentityRegisterServiceBase<TUser, IdentityRole<string>, string>(userManager)
    where TUser : IdentityUser<string>, new() {}

public abstract class IdentityRegisterServiceBase<TUser, TKey>(UserManager<TUser> userManager)
    : IdentityRegisterServiceBase<TUser, IdentityRole<TKey>, TKey>(userManager)
    where TUser : IdentityUser<TKey>, new()
    where TKey : IEquatable<TKey>  {}

/// <summary>
/// Register Base class for IAuthRepository / IUserAuth users
/// </summary>
public abstract class IdentityRegisterServiceBase<TUser, TRole, TKey>(UserManager<TUser> userManager) : RegisterServiceBase
    where TUser : IdentityUser<TKey>, new()
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
#if NET8_0_OR_GREATER
    [Microsoft.AspNetCore.Mvc.FromServices]
#endif
    public IValidator<Register>? RegistrationValidator { get; set; }

    public IdentityAuthContext<TUser, TRole, TKey> AuthContext => IdentityAuth.Instance<TUser, TRole, TKey>()
        ?? throw new Exception(nameof(IdentityAuth) + " not configured");

    protected UserManager<TUser> UserManager => userManager;

    protected TUser ToUser(Register request)
    {
        var to = request.ConvertTo<TUser>();
        to.UserName ??= to.Email;
        to.Email = request.Email;
        return to;
    }

    protected async Task<bool> UserExistsAsync(IAuthSession session) =>
        session.IsAuthenticated && await userManager.FindByEmailAsync(session.UserAuthName ?? session.Email).ConfigAwait() != null;

    protected virtual async Task ValidateAndThrowAsync(Register request)
    {
        var validator = RegistrationValidator 
            ?? ValidatorCache.GetValidator(Request, typeof(Register)) as IValidator<Register>
            ?? new IdentityRegistrationValidator<TUser, TKey>();
        if (validator is IRequiresRequest requiresRequest)
            requiresRequest.Request ??= Request;
        await validator.ValidateAndThrowAsync(request, ApplyTo.Post).ConfigAwait();
    }

    protected async Task RegisterNewUserAsync(IAuthSession session, TUser user)
    {
        var authEvents = TryResolve<IAuthEvents>();
        session.UserAuthId = user.Id.ToString();
        session.OnRegistered(Request, session, this);
        if (session is IAuthSessionExtended sessionExt)
            await sessionExt.OnRegisteredAsync(Request, session, this).ConfigAwait();
        authEvents?.OnRegistered(this.Request, session, this);
        if (authEvents is IAuthEventsAsync asyncEvents)
            await asyncEvents.OnRegisteredAsync(this.Request, session, this).ConfigAwait();
    }
}

public abstract class IdentityRegisterService<TUser, TKey>(UserManager<TUser> userManager)
    : IdentityRegisterService<TUser, IdentityRole<TKey>, TKey>(userManager)
    where TUser : IdentityUser<TKey>, new()
    where TKey : IEquatable<TKey>  {}


[DefaultRequest(typeof(Register))]
public class IdentityRegisterService<TUser, TRole, TKey>(UserManager<TUser> userManager)
    : IdentityRegisterServiceBase<TUser, TRole, TKey>(userManager)
    where TUser : IdentityUser<TKey>, new()
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    public async Task<object> PostAsync(Register request)
    {
        var session = await GetSessionAsync();
        if (await UserExistsAsync(session))
            throw new NotSupportedException(ErrorMessages.AlreadyRegistered);

        await ValidateAndThrowAsync(request);

        var newUser = ToUser(request);

        var result = await userManager.CreateAsync(newUser, request.Password);
        if (result.Succeeded)
        {
            session = AuthContext.UserToSessionConverter(newUser);
            await RegisterNewUserAsync(session, newUser);

            var response = await CreateRegisterResponse(session,
                request.UserName ?? request.Email, request.Password, request.AutoLogin);
            return response;
        }

        var errorCode = HttpStatusCode.BadRequest.ToString();
        var errorResponse = new RegisterResponse
        {
            ResponseStatus = new ResponseStatus
            {
                ErrorCode = errorCode,
                Message = errorCode,
                Errors = result.Errors.Map(x => new ResponseError
                {
                    ErrorCode = x.Code,
                    Message = x.Description,
                    FieldName = x.Code.StartsWith(nameof(Register.Password)) ? nameof(Register.Password) : null,
                })
            }
        };
        var status = errorResponse.ResponseStatus;
        var firstError = status.Errors.FirstOrDefault();
        if (firstError != null)
        {
            status.ErrorCode = firstError.ErrorCode;
            status.Message = firstError.Message;
        }
        return new HttpError(errorResponse, HttpStatusCode.BadRequest);
    }
}