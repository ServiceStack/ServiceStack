namespace ServiceStack.Translators.Generator.Tests.Support
{
	using System;
	using System.Collections.Generic;
	
	
	public static partial class CustomerServiceModelTranslator
	{
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Customer ToModel(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer from)
		{
			return CustomerServiceModelTranslator.UpdateModel(from, new ServiceStack.Translators.Generator.Tests.Support.Model.Customer());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> ToModelList(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> from)
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
					to.Add(item.ToModel());
				}
			}
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Customer UpdateModel(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer fromModel, ServiceStack.Translators.Generator.Tests.Support.Model.Customer model)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer from = fromModel;
			model.Id = from.Id;
			model.Name = from.Name;
			if ((from.BillingAddress != null))
			{
				model.BillingAddress = from.BillingAddress.ToModel();
			}
			model.PhoneNumbers = ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber.ToModelList(from.PhoneNumbers);
			// Skipping property 'model.ModelReadOnly' because 'model.ModelReadOnly' is read-only
			model.ModelWriteOnly = from.ModelWriteOnly;
			model.DtoReadOnly = from.DtoReadOnly;
			// Skipping property 'model.DtoWriteOnly' because 'this.DtoWriteOnly' is write-only
			return model;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer Parse(this ServiceStack.Translators.Generator.Tests.Support.Model.Customer from)
		{
			if ((from == null))
			{
				return null;
			}
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer();
			to.Id = from.Id;
			to.Name = from.Name;
			to.BillingAddress = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(from.BillingAddress);
			to.PhoneNumbers = ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber.ParseAll(from.PhoneNumbers);
			to.ModelReadOnly = from.ModelReadOnly;
			// Skipping property 'to.ModelWriteOnly' because 'model.ModelWriteOnly' is write-only
			// Skipping property 'to.DtoReadOnly' because 'to.DtoReadOnly' is read-only
			to.DtoWriteOnly = from.DtoWriteOnly;
			return to;
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer> ParseAll(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Customer> from)
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
				to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer.Parse(item));
			}
			return to;
		}
	}
}
