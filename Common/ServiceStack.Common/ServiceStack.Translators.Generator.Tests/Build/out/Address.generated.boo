namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
import System
import System.Collections.Generic


partial class Address():
	
	public virtual def ToModel() as ServiceStack.Translators.Generator.Tests.Support.Model.Address:
		return self.UpdateModel(ServiceStack.Translators.Generator.Tests.Support.Model.Address())
	
	public virtual def UpdateModel(model as ServiceStack.Translators.Generator.Tests.Support.Model.Address) as ServiceStack.Translators.Generator.Tests.Support.Model.Address:
		model.Line1 = Line1
		model.Line2 = Line2
		return model
	
	public static def Parse(from as ServiceStack.Translators.Generator.Tests.Support.Model.Address) as ServiceStack.Translators.Generator.Tests.Support.DataContract.Address:
		to as ServiceStack.Translators.Generator.Tests.Support.DataContract.Address = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address()
		to.Line1 = from.Line1
		to.Line2 = from.Line2
		return to
	
	public static def ParseAll(from as System.Collections.Generic.IEnumerable[of ServiceStack.Translators.Generator.Tests.Support.Model.Address]) as System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address]:
		to as System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address] = System.Collections.Generic.List[of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address]()
		iter as System.Collections.Generic.IEnumerator[of ServiceStack.Translators.Generator.Tests.Support.Model.Address] = from.GetEnumerator()
		while iter.MoveNext():
			item as ServiceStack.Translators.Generator.Tests.Support.Model.Address = iter.Current
			to.Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(item))

		return to
