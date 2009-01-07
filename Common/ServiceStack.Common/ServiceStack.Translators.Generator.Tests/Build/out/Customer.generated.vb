Imports System
Imports System.Collections.Generic

Namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
	
	Partial Public Class Customer
		
		Public Overridable Function ToModel() As ServiceStack.Translators.Generator.Tests.Support.Model.Customer
			Return Me.UpdateModel(New ServiceStack.Translators.Generator.Tests.Support.Model.Customer)
		End Function
		
		Public Overridable Function UpdateModel(ByVal model As ServiceStack.Translators.Generator.Tests.Support.Model.Customer) As ServiceStack.Translators.Generator.Tests.Support.Model.Customer
			model.Id = Id
			model.Name = Name
			model.BillingAddress = Me.BillingAddress.ToModel
			Return model
		End Function
		
		Public Shared Function Parse(ByVal from As ServiceStack.Translators.Generator.Tests.Support.Model.Customer) As ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer
			Dim [to] As ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer = New ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer
			[to].Id = from.Id
			[to].Name = from.Name
			[to].BillingAddress = ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(from.BillingAddress)
			Return [to]
		End Function
		
		Public Shared Function ParseAll(ByVal from As System.Collections.Generic.IEnumerable(Of ServiceStack.Translators.Generator.Tests.Support.Model.Customer)) As System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer)
			Dim [to] As System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer) = New System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer)
			Dim iter As System.Collections.Generic.IEnumerator(Of ServiceStack.Translators.Generator.Tests.Support.Model.Customer) = from.GetEnumerator
			Do While iter.MoveNext
				Dim item As ServiceStack.Translators.Generator.Tests.Support.Model.Customer = iter.Current
				[to].Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Customer.Parse(item))

			Loop
			Return [to]
		End Function
	End Class
End Namespace
