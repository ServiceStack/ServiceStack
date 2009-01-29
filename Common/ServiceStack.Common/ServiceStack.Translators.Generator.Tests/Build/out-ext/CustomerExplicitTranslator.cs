namespace ServiceStack.Translators.Generator.Tests.Support
{
	using System;
	using System.Collections.Generic;
	
	
	public static partial class CustomerExplicitTranslator
	{
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Customer ToDomainModelCustomer(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer from)
		{
			return CustomerExplicitTranslator.UpdateCustomer(from, new ServiceStack.Translators.Generator.Tests.Support.Model.Customer());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> ToDomainModelCustomers(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Customer>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer item = iter.Current;
				if ((item != null))
				{
					to.Add(item.ToDomainModelCustomer());
				}
			}
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Customer UpdateCustomer(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer fromParam, ServiceStack.Translators.Generator.Tests.Support.Model.Customer to)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer from = fromParam;
			to.Id = from.Id;
			to.Name = from.Name;
			if ((from.BillingAddress != null))
			{
				to.BillingAddress = from.BillingAddress.ToDomainModelAddress();
			}
			to.PhoneNumbers = from.PhoneNumbers.ToDomainModelPhoneNumbers();
			// Skipping property 'model.ModelReadOnly' because 'model.ModelReadOnly' is read-only
			to.ModelWriteOnly = from.ModelWriteOnly;
			to.DtoReadOnly = from.DtoReadOnly;
			// Skipping property 'model.DtoWriteOnly' because 'this.DtoWriteOnly' is write-only
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer ToDtoCustomer(this ServiceStack.Translators.Generator.Tests.Support.Model.Customer from)
		{
			if ((from == null))
			{
				return null;
			}
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer();
			to.Id = from.Id;
			to.Name = from.Name;
			to.BillingAddress = from.BillingAddress.ToDtoAddress();
			to.PhoneNumbers = from.PhoneNumbers.ToDtoPhoneNumbers();
			to.ModelReadOnly = from.ModelReadOnly;
			// Skipping property 'to.ModelWriteOnly' because 'model.ModelWriteOnly' is write-only
			// Skipping property 'to.DtoReadOnly' because 'to.DtoReadOnly' is read-only
			to.DtoWriteOnly = from.DtoWriteOnly;
			return to;
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> ToDtoCustomers(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.Model.Customer item = iter.Current;
				to.Add(item.ToDtoCustomer());
			}
			return to;
		}
	}
}
