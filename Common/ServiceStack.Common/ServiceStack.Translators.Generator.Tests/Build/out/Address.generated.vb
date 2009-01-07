Imports System
Imports System.Collections.Generic

Namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
	
	Partial Public Class Address
		
		Public Overridable Function ToModel() As ServiceStack.Translators.Generator.Tests.Support.Model.Address
			Return Me.UpdateModel(New ServiceStack.Translators.Generator.Tests.Support.Model.Address)
		End Function
		
		Public Overridable Function UpdateModel(ByVal model As ServiceStack.Translators.Generator.Tests.Support.Model.Address) As ServiceStack.Translators.Generator.Tests.Support.Model.Address
			model.Line1 = Line1
			model.Line2 = Line2
			Return model
		End Function
		
		Public Shared Function Parse(ByVal from As ServiceStack.Translators.Generator.Tests.Support.Model.Address) As ServiceStack.Translators.Generator.Tests.Support.DataContract.Address
			Dim [to] As ServiceStack.Translators.Generator.Tests.Support.DataContract.Address = New ServiceStack.Translators.Generator.Tests.Support.DataContract.Address
			[to].Line1 = from.Line1
			[to].Line2 = from.Line2
			Return [to]
		End Function
		
		Public Shared Function ParseAll(ByVal from As System.Collections.Generic.IEnumerable(Of ServiceStack.Translators.Generator.Tests.Support.Model.Address)) As System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address)
			Dim [to] As System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address) = New System.Collections.Generic.List(Of ServiceStack.Translators.Generator.Tests.Support.DataContract.Address)
			Dim iter As System.Collections.Generic.IEnumerator(Of ServiceStack.Translators.Generator.Tests.Support.Model.Address) = from.GetEnumerator
			Do While iter.MoveNext
				Dim item As ServiceStack.Translators.Generator.Tests.Support.Model.Address = iter.Current
				[to].Add(ServiceStack.Translators.Generator.Tests.Support.DataContract.Address.Parse(item))

			Loop
			Return [to]
		End Function
	End Class
End Namespace
