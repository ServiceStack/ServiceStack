using System;

namespace ServiceStack.Service
{
	public class PortAttribute : Attribute
	{
		public Type OperationType { get; set; }
		public PortRestriction Restrictions { get; set; }
		public int? MinVersion { get; set; }
		public int? MaxVersion { get; set; }

		public PortAttribute()
		{}

		public PortAttribute(Type operationType)
		{
			this.OperationType = operationType;
		}

		public PortAttribute(Type operationType, PortRestriction restrictions)
		{
			this.OperationType = operationType;
			this.Restrictions = restrictions;
		}
	}
}