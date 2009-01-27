namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class PhoneNumber
	{
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber ToModel()
		{
			return this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber());
		}
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> ToModelList(System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> from)
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
					to.Add(item.ToModel());
				}
			}
			return to;
		}
		
		public virtual ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber UpdateModel(ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber model)
		{
			ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber from = this;
			model.Type = ServiceStack.Common.Utils.StringConverterUtils.Parse<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumberType>(from.Type);
			model.Number = from.Number;
			return model;
		}
		
		public static ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber Parse(ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber from)
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
		
		public static System.Collections.Generic.List<ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber> ParseAll(System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.PhoneNumber> from)
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
				to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.PhoneNumber.Parse(item));
			}
			return to;
		}
	}
}
