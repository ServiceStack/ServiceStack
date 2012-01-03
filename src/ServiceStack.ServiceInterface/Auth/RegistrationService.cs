using System;
using System.Configuration;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface.ServiceModel;
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
	}

	public class RegistrationResponse
	{
		public RegistrationResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string UserId { get; set; }

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
				RuleFor(x => x.Email).NotEmpty().When(x => x.UserName.IsNullOrEmpty());
				RuleFor(x => x.UserName)
					.Must(x => UserAuthRepo.GetUserAuthByUserName(x) == null)
					.WithErrorCode("UserNameAlreadyExists")
					.WithMessage("UserName already exists")
					.When(x => !x.UserName.IsNullOrEmpty());
				RuleFor(x => x.Email)
					.Must(x => x.IsNullOrEmpty() || UserAuthRepo.GetUserAuthByUserName(x) == null)
					.WithErrorCode("EmailAlreadyExists")
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

		public static void Init(IAppHost appHost)
		{
			appHost.RegisterService<RegistrationService>();
			appHost.RegisterAs<RegistrationValidator, IValidator<Registration>>();
		}

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
			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Get, request);
				if (response != null) return response;
			}

			AssertUserAuthRepo();

			RegistrationValidator.ValidateAndThrow(request, ApplyTo.Post);

			var newUserAuth = request.TranslateTo<UserAuth>();
			var createdUser = this.UserAuthRepo.CreateUserAuth(newUserAuth, request.Password);

			return new RegistrationResponse {
				UserId = createdUser.Id.ToString(),
			};
		}

		/// <summary>
		/// Logic to update UserAuth from Registration info, not enabled on OnPut because of security.
		/// </summary>
		public object UpdateUserAuth(Registration request)
		{
			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Get, request);
				if (response != null) return response;
			}

			RegistrationValidator.ValidateAndThrow(request, ApplyTo.Put);

			var userName = request.UserName ?? request.Email;
			var existingUser = UserAuthRepo.GetUserAuthByUserName(userName);

			existingUser.PopulateWith(request);

			UserAuthRepo.SaveUserAuth(existingUser);

			return new RegistrationResponse {
				UserId = existingUser.Id.ToString(),
			};
		}
	}
}