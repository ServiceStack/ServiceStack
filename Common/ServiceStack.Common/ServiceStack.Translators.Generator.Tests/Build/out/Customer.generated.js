//@cc_on
//@set @debug(off)

import System;
import System.Collections.Generic;

package ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	
	public class Customer
	{
		
		public function ToModel() : ServiceStack.Translators.Generator.Tests.Support.Model.Customer
		{
			return this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Customer());
		}
		
		public function UpdateModel(model : ServiceStack.Translators.Generator.Tests.Support.Model.Customer) : ServiceStack.Translators.Generator.Tests.Support.Model.Customer
		{
			model.Id = Id;
			model.Name = Name;
			model.BillingAddress = this.BillingAddress.ToModel();
			return model;
		}
		
		public static function Parse(from : ServiceStack.Translators.Generator.Tests.Support.Model.Customer) : ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer
		{
			if ((from == undefined))
			{
				return undefined;
			}
			var to : ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer();
			to.Id = from.Id;
			to.Name = from.Name;
			to.BillingAddress = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(from.BillingAddress);
			return to;
		}
		
		public static function ParseAll(from : System.Collections.Generic.IEnumerable`1) : System.Collections.Generic.List`1
		{
			var to : System.Collections.Generic.List`1 = new System.Collections.Generic.List`1();
			for (var iter : System.Collections.Generic.IEnumerator`1 = from.GetEnumerator();
			; iter.MoveNext(); 
			)
			{
				var item : ServiceStack.Translators.Generator.Tests.Support.Model.Customer = iter.Current;
				to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer.Parse(item));
			}
			return to;
		}
	}
}
