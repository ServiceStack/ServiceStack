using System;

namespace ServiceStack.Service
{
	public class PortAttribute : Attribute
	{
		public Type OperationType { get; set; }
		public PortRestriction Restrictions { get; set; }
		public int? Version { get; set; }

		public PortAttribute()
		{ }

		public PortAttribute(Type operationType)
		{
			this.OperationType = operationType;
		}

		public PortAttribute(Type operationType, int version)
			: this(operationType)
		{
			this.Version = version;
		}

		public PortAttribute(Type operationType, PortRestriction restrictions)
			: this(operationType)
		{
			this.OperationType = operationType;
			this.Restrictions = restrictions;
		}

		public PortAttribute(Type operationType, PortRestriction restrictions, int version)
			: this(operationType, restrictions)
		{
			this.Version = version;
		}
	}
}