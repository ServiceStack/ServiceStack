//@cc_on
//@set @debug(off)

import System;
import System.Collections.Generic;

package ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	
	public class Address
	{
		
		public function ToModel() : ServiceStack.Translators.Generator.Tests.Support.Model.Address
		{
			return this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Address());
		}
		
		public function UpdateModel(model : ServiceStack.Translators.Generator.Tests.Support.Model.Address) : ServiceStack.Translators.Generator.Tests.Support.Model.Address
		{
			model.Line1 = Line1;
			model.Line2 = Line2;
			return model;
		}
		
		public static function Parse(from : ServiceStack.Translators.Generator.Tests.Support.Model.Address) : ServiceStack.Translators.Generator.Tests.Support.DataContract.Address
		{
			var to : ServiceStack.Translators.Generator.Tests.Support.DataContract.Address = new ServiceStack.Translators.Generator.Tests.Support.DataContract.Address();
			to.Line1 = from.Line1;
			to.Line2 = from.Line2;
			return to;
		}
		
		public static function ParseAll(from : System.Collections.Generic.IEnumerable`1) : System.Collections.Generic.List`1
		{
			var to : System.Collections.Generic.List`1 = new System.Collections.Generic.List`1();
			for (var iter : System.Collections.Generic.IEnumerator`1 = from.GetEnumerator();
			; iter.MoveNext(); 
			)
			{
				var item : ServiceStack.Translators.Generator.Tests.Support.Model.Address = iter.Current;
				to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(item));
			}
			return to;
		}
	}
}
