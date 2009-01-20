using System;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Session;
using ServiceStack.Validation.Validators;

namespace ServiceStack.Validation.Tests.Support
{
	/// <summary>
	/// Tests if the field is not null or not default(T) for value types
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidSessionAttribute : ValidationAttributeBase
	{
		private UserSessionManager userSessionManager;
		public UserSessionManager UserSessionManager
		{
			get
			{
				if (this.userSessionManager == null)
				{
					var sessionManager = ApplicationContext.Instance.Get<UserSessionManager>();
					if (sessionManager == null)
					{
						throw new ArgumentException("UserSessionManager does not exist. " + 
						                            "Either inject it in the attribute or register it in the ApplicationContext");
					}
					return sessionManager;
				}
				return this.userSessionManager;
			} 
			set { this.userSessionManager = value; }
		}

		public override string Validate(object oUserSessionId)
		{
			if (oUserSessionId == null) return ErrorCodes.FieldIsRequired.ToString();

			var userSessionId = oUserSessionId as UserSessionId;
			if (userSessionId == null)
			{
				throw new ArgumentException(
						"'[ValidateSession]' Attribute can only be applied to 'UserSessionId' properties");
			}

			var sessionManager = this.UserSessionManager;
			var clientSession = sessionManager.GetUserClientSession(userSessionId.UserId, userSessionId.SessionId);
			if (clientSession == null)
			{
				return ErrorCodes.InvalidOrExpiredSession.ToString();
			}
			return null;
		}
	}

	internal class UserSessionId
	{
		public int UserId { get; set; }
		public Guid SessionId { get; set; }

		public UserSessionId(int userId, Guid sessionId)
		{
			this.UserId = userId;
			this.SessionId = sessionId;
		}
	}
}