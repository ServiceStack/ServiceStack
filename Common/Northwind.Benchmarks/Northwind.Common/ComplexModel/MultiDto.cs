using System;
using Northwind.Common.ServiceModel;

namespace Northwind.Common.ComplexModel
{
	public class MultiDto
	{
		public Guid Id { get; set; }
		public CustomerDto Customer { get; set; }
		public SupplierDto Supplier { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as MultiDto;
			if (other == null) return false;

			return this.Id == other.Id
			       && this.Customer.Equals(other.Customer)
			       && this.Supplier.Equals(other.Supplier);
		}
	}
}