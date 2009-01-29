namespace ServiceStack.Translators.Generator.Tests.Support
{
	using System;
	using System.Collections.Generic;
	
	
	public static partial class PhoneNumberExplicitTranslator
	{
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber ToDomainModelPhoneNumber(this ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber from)
		{
			return PhoneNumberExplicitTranslator.UpdatePhoneNumber(from, new ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> ToDomainModelPhoneNumbers(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber item = iter.Current;
				if ((item != null))
				{
					to.Add(item.ToDomainModelPhoneNumber());
				}
			}
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber UpdatePhoneNumber(this ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber fromParam, ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber to)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber from = fromParam;
			to.Type = ServiceStack.Common.Utils.StringConverterUtils.Parse<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumberType>(from.Type);
			to.Number = from.Number;
			return to;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber ToDtoPhoneNumber(this ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber from)
		{
			if ((from == null))
			{
				return null;
			}
			ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber to = new ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber();
			if ((from.Type != null))
			{
				to.Type = from.Type.ToString();
			}
			to.Number = from.Number;
			return to;
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> ToDtoPhoneNumbers(this System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> from)
		{
			if ((from == null))
			{
				return null;
			}
			System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> to = new System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber>();
			for (System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber item = iter.Current;
				to.Add(item.ToDtoPhoneNumber());
			}
			return to;
		}
	}
}
