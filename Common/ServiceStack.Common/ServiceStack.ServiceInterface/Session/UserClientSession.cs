using System;

namespace ServiceStack.ServiceInterface.Session
{
	[Serializable]
	public class UserClientSession
	{
		private const int VALID_FOR_TWO_WEEKS = 14;
		private readonly UserSession userSession;
		public string IpAddress { get; private set; }
		public DateTime ExpiryDate { get; private set; }

		public UserClientSession(UserSession userSession, Guid id, string ipAddress, string base64ClientModulus)
		{
			this.userSession = userSession;
			this.Id = id;
			this.IpAddress = ipAddress;
			this.Base64ClientModulus = base64ClientModulus;
			this.ExpiryDate = DateTime.Now.AddDays(VALID_FOR_TWO_WEEKS);
		}

		public Guid Id { get; set; }
		public long UserId { get { return userSession.UserId; } }
		public string Base64ClientModulus { get; set; }
	}
}