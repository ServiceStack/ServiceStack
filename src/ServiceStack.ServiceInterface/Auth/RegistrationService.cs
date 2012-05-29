using System;
using System.Configuration;
using System.Globalization;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface.Validation;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
{
    public class Registration
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool? AutoLogin { get; set; }
        public string Continue { get; set; }
    }

    public class RegistrationResponse
    {
        public RegistrationResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        public string UserId { get; set; }

        public string SessionId { get; set; }

        public string UserName { get; set; }

        public string ReferrerUrl { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class FullRegistrationValidator : RegistrationValidator
    {
        public FullRegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () => {
                RuleFor(x => x.DisplayName).NotEmpty();
            });
        }
    }

    public class RegistrationValidator : AbstractValidator<Registration>
    {
        public IUserAuthRepository UserAuthRepo { get; set; }

        public RegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () => {
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
            RuleSet(ApplyTo.Put, () => {
                RuleFor(x => x.UserName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty();
            });
        }
    }

    public class RegistrationService : RestServiceBase<Registration>
    {
        public IUserAuthRepository UserAuthRepo { get; set; }
        public static ValidateFn ValidateFn { get; set; }

        public IValidator<Registration> RegistrationValidator { get; set; }

        private void AssertUserAuthRepo()
        {
            if (UserAuthRepo == null)
                throw new ConfigurationException("No IUserAuthRepository has been registered in your AppHost.");
        }

        /// <summary>
        /// Create new Registration
        /// </summary>
        public override object OnPost(Registration request)
        {
            if (!ValidationFeature.Enabled)
                RegistrationValidator.ValidateAndThrow(request, ApplyTo.Post);

            AssertUserAuthRepo();

            if (ValidateFn != null)
            {
                var validateResponse = ValidateFn(this, HttpMethods.Post, request);
                if (validateResponse != null) return validateResponse;
            }

            RegistrationResponse response = null;
            var session = this.GetSession();
            var newUserAuth = ToUserAuth(request);
            var existingUser = UserAuthRepo.GetUserAuth(session, null);

            var user = existingUser != null
                ? this.UserAuthRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password)
                : this.UserAuthRepo.CreateUserAuth(newUserAuth, request.Password);

            if (request.AutoLogin.GetValueOrDefault())
            {
                var authService = base.ResolveService<AuthService>();
                var authResponse = authService.Post(new Auth {
                    UserName = request.UserName ?? request.Email,
                    Password = request.Password
                });

                if (authResponse is IHttpError)
                    throw (Exception)authResponse;

                var typedResponse = authResponse as AuthResponse;
                if (typedResponse != null)
                {
                    response = new RegistrationResponse {
                        SessionId = typedResponse.SessionId,
                        UserName = typedResponse.UserName,
                        ReferrerUrl = typedResponse.ReferrerUrl,
                        UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    };
                }
            }

            if (response == null)
            {
                response = new RegistrationResponse {
                    UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                };
            }

            if (request.Continue == null)
                return response;
            
            return new HttpResult(response) {
                Location = request.Continue
            };
        }

        public UserAuth ToUserAuth(Registration request)
        {
            var to = request.TranslateTo<UserAuth>();
            to.PrimaryEmail = request.Email;
            return to;
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on OnPut because of security.
        /// </summary>
        public object UpdateUserAuth(Registration request)
        {
            if (!ValidationFeature.Enabled)
                RegistrationValidator.ValidateAndThrow(request, ApplyTo.Put);

            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Put, request);
                if (response != null) return response;
            }

            var session = this.GetSession();
            var existingUser = UserAuthRepo.GetUserAuth(session, null);
            if (existingUser == null)
            {
                throw HttpError.NotFound("User does not exist");
            }

            var newUserAuth = ToUserAuth(request);
            UserAuthRepo.UpdateUserAuth(newUserAuth, existingUser, request.Password);

            return new RegistrationResponse {
                UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}