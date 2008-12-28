using System;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Once implemented (in the ServiceController area) this will ensure that the ports
	/// are only executed in the specified restrictions.
	/// </summary>
	public class MessagingRestrictionAttribute : Attribute
	{
		public MessagingRestriction Restrictions { get; private set; }

		public MessagingRestrictionAttribute(MessagingRestriction restrictions)
		{
			this.Restrictions = restrictions;
		}
	}
}