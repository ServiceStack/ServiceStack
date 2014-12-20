using System;
using System.Globalization;
using ServiceStack.FluentValidation;
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
        public IAuthRepository UserAuthRepo { get; set; }

        public RegistrationValidator()
        {
            RuleSet(
                ApplyTo.Post,
                () =>
                {
                    RuleFor(x => x.Password).NotEmpty();
                    RuleFor(x => x.UserName).NotEmpty().When(x => x.Email.IsNullOrEmpty());
                    RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.UserName)
                        .Must(x => UserAuthRepo.GetUserAuthByUserName(x) == null)
                        .WithErrorCode("AlreadyExists")
                        .WithMessage("UserName already exists")
                        .When(x => !x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.Email)
                        .Must(x => x.IsNullOrEmpty() || UserAuthRepo.GetUserAuthByUserName(x) == null)
                        .WithErrorCode("AlreadyExists")
                        .WithMessage("Email already exists")
                        .When(x => !x.Email.IsNullOrEmpty());
                });
            RuleSet(
                ApplyTo.Put,
                () =>
                {
                    RuleFor(x => x.UserName).NotEmpty();
                    RuleFor(x => x.Email).NotEmpty();
                });
        }
    }

    [DefaultRequest(typeof(Register))]
    public class RegisterService : RegisterService<UserAuth> { }

    [DefaultRequest(typeof(Register))]
    public class RegisterService<TUserAuth> : Service
        where TUserAuth : class, IUserAuth
    {
        public IAuthRepository AuthRepo { get; set; }

        public static ValidateFn ValidateFn { get; set; }

        public IValidator<Register> RegistrationValidator { get; set; }

        public IAuthEvents AuthEvents { get; set; }

        /// <summary>
        /// Update an existing registraiton
        /// </summary>
        public object Put(Register request)
        {
            return Post(request);
        }

        /// <summary>
        ///     Create new Registration
        /// </summary>
        public object Post(Register request)
        {
            if (HostContext.GlobalRequestFilters == null
                || !HostContext.GlobalRequestFilters.Contains(ValidationFilters.RequestFilter)) //Already gets run
            {
                if (RegistrationValidator != null)
                {
                    RegistrationValidator.ValidateAndThrow(request, ApplyTo.Post);
                }
            }

            var userAuthRepo = AuthRepo.AsUserAuthRepository(GetResolver());

            if (ValidateFn != null)
            {
                var validateResponse = ValidateFn(this, HttpMethods.Post, request);
                if (validateResponse != null)
                    return validateResponse;
            }

            RegisterResponse response = null;
            var session = this.GetSession();
            var newUserAuth = ToUserAuth(request);
            var existingUser = userAuthRepo.GetUserAuth(session, null);

            var registerNewUser = existingUser == null;
            var user = registerNewUser
                ? userAuthRepo.CreateUserAuth(newUserAuth, request.Password)
                : userAuthRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

            if (request.AutoLogin.GetValueOrDefault())
            {
                using (var authService = base.ResolveService<AuthenticateService>())
                {
                    var authResponse = authService.Post(
                        new Authenticate {
                            provider = CredentialsAuthProvider.Name,
                            UserName = request.UserName ?? request.Email,
                            Password = request.Password,
                            Continue = request.Continue
                        });

                    if (authResponse is IHttpError)
                        throw (Exception)authResponse;

                    var typedResponse = authResponse as AuthenticateResponse;
                    if (typedResponse != null)
                    {
                        response = new RegisterResponse
                        {
                            SessionId = typedResponse.SessionId,
                            UserName = typedResponse.UserName,
                            ReferrerUrl = typedResponse.ReferrerUrl,
                            UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                        };
                    }
                }
            }

            if (registerNewUser)
            {
                session = this.GetSession();
                session.OnRegistered(this.Request, session, this);
                if (AuthEvents != null)
                    AuthEvents.OnRegistered(this.Request, session, this);
            }

            if (response == null)
            {
                response = new RegisterResponse
                {
                    UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    ReferrerUrl = request.Continue
                };
            }

            var isHtml = Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            if (isHtml)
            {
                if (string.IsNullOrEmpty(request.Continue))
                    return response;

                return new HttpResult(response)
                {
                    Location = request.Continue
                };
            }

            return response;
        }

        public TUserAuth ToUserAuth(Register request)
        {
            var to = request.ConvertTo<TUserAuth>();
            to.PrimaryEmail = request.Email;
            return to;
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
        /// </summary>
        public object UpdateUserAuth(Register request)
        {
            if (HostContext.GlobalRequestFilters == null
                || !HostContext.GlobalRequestFilters.Contains(ValidationFilters.RequestFilter))
            {
                RegistrationValidator.ValidateAndThrow(request, ApplyTo.Put);
            }

            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Put, request);
                if (response != null)
                    return response;
            }

            var userAuthRepo = AuthRepo.AsUserAuthRepository(GetResolver());
            var session = this.GetSession();

            var existingUser = userAuthRepo.GetUserAuth(session, null);
            if (existingUser == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists);

            var newUserAuth = ToUserAuth(request);
            userAuthRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

            return new RegisterResponse
            {
                UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}