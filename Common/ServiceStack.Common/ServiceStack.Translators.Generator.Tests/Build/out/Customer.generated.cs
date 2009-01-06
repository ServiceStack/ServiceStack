namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class Customer
    {
        
        public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Customer ToModel()
        {
			var model = new ServiceStack.Translators.Generator.Tests.Support.Model.Customer {
				Id = this.Id,
				Name = this.Name,
				BillingAddress = this.BillingAddress.ToModel(),
			};
			return model;
        }
        
        public virtual Customer Parse(ServiceStack.Translators.Generator.Tests.Support.Model.Customer from)
        {
			var to = new Customer {
				Id = from.Id,
				Name = from.Name,
				BillingAddress = new Address().Parse(from.BillingAddress),
			};
			return to;
        }
        
        public static List<Customer> ParseAll(IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> from)
        {
			var to = new List<Customer>();
			foreach (var item in from)
			{
				to.Add(new Customer().Parse(item));
			}
			return to;
        }
    }
}
