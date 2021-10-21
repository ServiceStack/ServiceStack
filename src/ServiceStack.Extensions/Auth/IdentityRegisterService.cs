using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class IdentityRegistrationValidator<TUser> : AbstractValidator<Register>
        where TUser : IdentityUser
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
                        .MustAsync(async (x,token) => {
                            var userManager = Request.TryResolve<UserManager<TUser>>();
                            return await userManager.FindByEmailAsync(x).ConfigAwait() == null;
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.UsernameAlreadyExists.Localize(base.Request))
                        .When(x => !x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.Email)
                        .MustAsync(async (x,token) => {
                            var userManager = Request.TryResolve<UserManager<TUser>>();
                            return await userManager.FindByEmailAsync(x).ConfigAwait() == null;
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.EmailAlreadyExists.Localize(base.Request))
                        .When(x => !x.Email.IsNullOrEmpty());
                });
        }
    }

    /// <summary>
    /// Register Base class for IAuthRepository / IUserAuth users
    /// </summary>
    public abstract class IdentityRegisterServiceBase<TUser> : RegisterServiceBase
        where TUser : IdentityUser
    {
        public IValidator<Register> RegistrationValidator { get; set; }

        protected readonly UserManager<TUser> userManager;
        public IdentityRegisterServiceBase(UserManager<TUser> userManager)
        {
            this.userManager = userManager;
        }

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
            var validator = RegistrationValidator ?? new RegistrationValidator();
            await validator.ValidateAndThrowAsync(request, ApplyTo.Post).ConfigAwait();
        }

        protected async Task RegisterNewUserAsync(IAuthSession session, TUser user)
        {
            var authEvents = TryResolve<IAuthEvents>();
            session.UserAuthId = user.Id;
            session.OnRegistered(Request, session, this);
            if (session is IAuthSessionExtended sessionExt)
                await sessionExt.OnRegisteredAsync(Request, session, this).ConfigAwait();
            authEvents?.OnRegistered(this.Request, session, this);
            if (authEvents is IAuthEventsAsync asyncEvents)
                await asyncEvents.OnRegisteredAsync(this.Request, session, this).ConfigAwait();
        }
    }

    [DefaultRequest(typeof(Register))]
    public class IdentityRegisterService<TUser,TRole> : IdentityRegisterServiceBase<TUser> 
        where TUser : IdentityUser
        where TRole : IdentityRole
    {
        public IdentityRegisterService(UserManager<TUser> userManager) : base(userManager) { }

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
                session = ((IdentityAuthContext<TUser, TRole>)IdentityAuth.Config!)!.UserToSessionConverter(newUser);
                await RegisterNewUserAsync(session, newUser);
            
                var response = await CreateRegisterResponse(session, 
                    request.UserName ?? request.Email, request.Password, request.AutoLogin);
                return response;
            }

            var errorCode = HttpStatusCode.BadRequest.ToString();
            var errorResponse = new RegisterResponse {
                ResponseStatus = new ResponseStatus {
                    ErrorCode = errorCode,
                    Message = errorCode,
                    Errors = result.Errors.Map(x => new ResponseError {
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
}