using System;

namespace ServiceStack.Service
{
	public class PortAttribute : Attribute
	{
		public Type OperationType { get; set; }
		public EndpointAttributes PortRestrictions { get; set; }
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

		public PortAttribute(Type operationType, EndpointAttributes endpointAttributes)
			: this(operationType)
		{
			this.OperationType = operationType;
			this.PortRestrictions = endpointAttributes;
		}

		public PortAttribute(Type operationType, EndpointAttributes endpointAttributes, int version)
			: this(operationType, endpointAttributes)
		{
			this.Version = version;
		}
	}
}