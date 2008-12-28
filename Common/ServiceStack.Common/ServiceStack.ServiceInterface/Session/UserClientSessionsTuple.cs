using ServiceStack.ServiceInterface.Session;

namespace ServiceStack.ServiceInterface.Session
{
	/// <summary>
	/// Holds a 'Secure' and 'Unsecure' client session for the user.
	/// 
	/// //TODO: think of a better name
	/// </summary>
	public class UserClientSessionsTuple
	{
		public UserClientSessionsTuple(UserClientSession unsecureClientSession, UserClientSession secureClientSession)
		{
			this.UnsecureClientSession = unsecureClientSession;
			this.SecureClientSession = secureClientSession;
		}

		public UserClientSession UnsecureClientSession { get; private set; }
		public UserClientSession SecureClientSession { get; private set; }
	}
}