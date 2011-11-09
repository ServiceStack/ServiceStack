using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.ServiceInterface.Auth
{
	public class Login
	{
		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class LoginResponse
	{
		public LoginResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string UserId { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}
}