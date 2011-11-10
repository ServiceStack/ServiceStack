using ServiceStack.Common;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceInterface.Auth
{
	public class LoginService : RestServiceBase<Login>
	{
		public IUserAuthRepository UserAuthRepo { get; set; }

		public override object OnPost(Login request)
		{
			request.UserName.ThrowIfNullOrEmpty("UserName");
			request.Password.ThrowIfNullOrEmpty("Password");

			var existingUser = UserAuthRepo.GetUserAuthByUserName(request.UserName);
			if (existingUser != null)
				throw HttpError.Conflict("UserName already exists");

			var newUserAuth = request.TranslateTo<UserAuth>();
			var createdUser = this.UserAuthRepo.CreateUserAuth(newUserAuth, request.Password);

			return new LoginResponse {
				UserId = createdUser.Id.ToString(),
			};
		}
	}
}