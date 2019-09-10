﻿using System;
using System.Linq;
using System.Collections.Generic;
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
                        .Must(x =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
                            using (authRepo as IDisposable)
                            {
                                return authRepo.GetUserAuthByUserName(x) == null;
                            }
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.UsernameAlreadyExists.Localize(base.Request))
                        .When(x => !x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.Email)
                        .Must(x =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
                            using (authRepo as IDisposable)
                            {
                                return x.IsNullOrEmpty() || authRepo.GetUserAuthByUserName(x) == null;
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
        public object Put(Register request)
        {
            return Post(request);
        }

        /// <summary>
        ///     Create new Registration
        /// </summary>
        public object Post(Register request)
        {
            var authFeature = GetPlugin<AuthFeature>();
            if (authFeature != null)
            {
                if (authFeature.SaveUserNamesInLowerCase == true)
                {
                    if (request.UserName != null)
                        request.UserName = request.UserName.ToLower();
                    if (request.Email != null)
                        request.Email = request.Email.ToLower();
                }

                if (!string.IsNullOrEmpty(request.Continue))
                    authFeature.ValidateRedirectLinks(Request, request.Continue);
            }
            
            var validateResponse = ValidateFn?.Invoke(this, HttpMethods.Post, request);
            if (validateResponse != null)
                return validateResponse;

            RegisterResponse response = null;
            var session = this.GetSession();
            var newUserAuth = ToUserAuth(AuthRepository, request);

            var existingUser = session.IsAuthenticated ? AuthRepository.GetUserAuth(session, null) : null;
            var registerNewUser = existingUser == null;

            if (!registerNewUser && !AllowUpdates)
                throw new NotSupportedException(ErrorMessages.RegisterUpdatesDisabled.Localize(Request));
            
            if (!HostContext.AppHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync)) //Already gets run
                RegistrationValidator?.ValidateAndThrow(request, registerNewUser ? ApplyTo.Post : ApplyTo.Put);
            
            var user = registerNewUser
                ? AuthRepository.CreateUserAuth(newUserAuth, request.Password)
                : AuthRepository.UpdateUserAuth(existingUser, newUserAuth, request.Password);

            if (registerNewUser)
            {
                session.PopulateSession(user);
                session.OnRegistered(Request, session, this);
                AuthEvents?.OnRegistered(this.Request, session, this);
            }

            if (request.AutoLogin.GetValueOrDefault())
            {
                using (var authService = base.ResolveService<AuthenticateService>())
                {
                    var authResponse = authService.Post(
                        new Authenticate {
                            provider = CredentialsAuthProvider.Name,
                            UserName = request.UserName ?? request.Email,
                            Password = request.Password,
                            Continue = request.Continue ?? base.Request.GetQueryStringOrForm(Keywords.ReturnUrl)
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
            }

            if (response == null)
            {
                response = new RegisterResponse
                {
                    UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    ReferrerUrl = request.Continue,
                    UserName = session.UserName,
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

        public IUserAuth ToUserAuth(IAuthRepository authRepo, Register request)
        {
            var to = authRepo is ICustomUserAuth customUserAuth
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

                var newUserAuth = ToUserAuth(authRepo, request);
                authRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

                return new RegisterResponse
                {
                    UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
                };
            }
        }
    }
}