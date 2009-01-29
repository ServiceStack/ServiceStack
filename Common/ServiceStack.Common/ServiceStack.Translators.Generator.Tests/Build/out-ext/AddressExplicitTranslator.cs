namespace ServiceStack.Translators.Generator.Tests.Support
{
	using System;
	using System.Collections.Generic;
	
	
	public static partial class AddressExplicitTranslator
	{
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Address ToDomainModelAddress(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Address from)
		{
			return AddressExplicitTranslator.UpdateAddress(from, new ServiceStack.Translators.Generator.Tests.Support.Model.Address());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.Address> ToDomainModelAddresss(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> from)
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
					to.Add(item.ToDomainModelAddress());
				}
			}
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.Address UpdateAddress(this ServiceStack.Translators.Generator.Tests.Support.DataContract.Address fromParam, ServiceStack.Translators.Generator.Tests.Support.Model.Address to)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.Address from = fromParam;
			to.Line1 = from.Line1;
			to.Line2 = from.Line2;
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.Address ToDtoAddress(this ServiceStack.Translators.Generator.Tests.Support.Model.Address from)
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
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.Address> ToDtoAddresss(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Address> from)
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
				to.Add(item.ToDtoAddress());
			}
			return to;
		}
	}
}
