namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
import System
import System.Collections.Generic


partial class Customer():
	
	public virtual def ToModel() as ServiceStack.Translators.Generator.Tests.Support.Model.Customer:
		return self.UpdateModel(ServiceStack.Translators.Generator.Tests.Support.Model.Customer())
	
	public virtual def UpdateModel(model as ServiceStack.Translators.Generator.Tests.Support.Model.Customer) as ServiceStack.Translators.Generator.Tests.Support.Model.Customer:
		model.Id = Id
		model.Name = Name
		model.BillingAddress = self.BillingAddress.ToModel()
		return model
	
	public static def Parse(from as ServiceStack.Translators.Generator.Tests.Support.Model.Customer) as ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer:
		to as ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer = ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer()
		to.Id = from.Id
		to.Name = from.Name
		to.BillingAddress = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(from.BillingAddress)
		return to
	
	public static def ParseAll(from as System.Collections.Generic.IEnumerable[of ServiceStack.Translators.Generator.Tests.Support.Model.Customer]) as System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer]:
		to as System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer] = System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer]()
		iter as System.Collections.Generic.IEnumerator[of ServiceStack.Translators.Generator.Tests.Support.Model.Customer] = from.GetEnumerator()
		while iter.MoveNext():
			item as ServiceStack.Translators.Generator.Tests.Support.Model.Customer = iter.Current
			to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer.Parse(item))

		return to
