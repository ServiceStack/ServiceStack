namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class Customer
	{
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Customer ToModel()
		{
			return this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Customer());
		}
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Customer UpdateModel(ServiceStack.Translators.Generator.Tests.Support.Model.Customer model)
		{
			model.Id = Id;
			model.Name = Name;
			model.BillingAddress = this.BillingAddress.ToModel();
			return model;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer Parse(ServiceStack.Translators.Generator.Tests.Support.Model.Customer from)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer();
			to.Id = from.Id;
			to.Name = from.Name;
			to.BillingAddress = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(from.BillingAddress);
			return to;
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> ParseAll(System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> from)
		{
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.Model.Customer item = iter.Current;
				to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer.Parse(item));
			}
			return to;
		}
	}
}
