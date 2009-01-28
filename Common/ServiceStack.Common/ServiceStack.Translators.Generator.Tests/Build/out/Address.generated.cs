namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class Address
	{
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Address ToModel()
		{
			return this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Address());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Address> ToModelList(System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Address> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Address>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.DataContract.Address item = iter.Current;
				if ((item != null))
				{
					to.Add(item.ToModel());
				}
			}
			return to;
		}
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Address UpdateModel(ServiceStack.Translators.Generator.Tests.Support.Model.Address model)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Address from = this;
			model.Line1 = from.Line1;
			model.Line2 = from.Line2;
			return model;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.Address Parse(ServiceStack.Translators.Generator.Tests.Support.Model.Address from)
		{
			if ((from == null))
			{
				return null;
			}
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Address to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Address();
			to.Line1 = from.Line1;
			to.Line2 = from.Line2;
			return to;
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> ParseAll(System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Address> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.Address> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.Model.Address item = iter.Current;
				to.Add(Parse(item));
			}
			return to;
		}
	}
}
