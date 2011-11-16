using System;
using System.Configuration;
using ServiceStack.Common;
using ServiceStack.Common.Web;
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

	public class RegistrationService : RestServiceBase<Registration>
	{
		public IUserAuthRepository UserAuthRepo { get; set; }
		public static ValidateFn ValidateFn { get; set; }

		public static void Init(IAppHost appHost)
		{
			appHost.RegisterService<RegistrationService>();
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

			request.Password.ThrowIfNullOrEmpty("Password");

			if (request.UserName.IsNullOrEmpty() && request.Email.IsNullOrEmpty())
				throw new ArgumentNullException("UserName or Email required");

			if (!request.UserName.IsNullOrEmpty()
				&& UserAuthRepo.GetUserAuthByUserName(request.UserName) != null)
				throw HttpError.Conflict("UserName already exists");

			if (!request.Email.IsNullOrEmpty()
				&& UserAuthRepo.GetUserAuthByUserName(request.Email) != null)
				throw HttpError.Conflict("Email already exists");

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

			if (request.UserName.IsNullOrEmpty() && request.Email.IsNullOrEmpty())
				throw new ArgumentNullException("UserName or Email required");

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