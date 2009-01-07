
namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
    #nowarn "49" // uppercase argument names
    #nowarn "67" // this type test or downcast will always hold
    #nowarn "66" // this upcast is unnecessary - the types are identical
    #nowarn "58" // possible incorrect indentation..
    #nowarn "57" // do not use create_DelegateEvent
    #nowarn "51" // address-of operator can occur in the code
    open System
    open System.Collections.Generic
    
    exception ReturnExceptionc92cf7c6f8854045ac16b20665406291 of obj
    exception ReturnNoneExceptionc92cf7c6f8854045ac16b20665406291
    
    type
        
        (* partial *)Address = class
            static member ParseAll  (from:System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Address>) =
                let mutable from = from
                let mutable (_to:System.Collections.Generic.List<Address>) = new System.Collections.Generic.List<Address>()
                let mutable (iter:System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.Address>) = from.GetEnumerator()
                while iter.MoveNext() do
                    let mutable (item:ServiceStack.Translators.Generator.Tests.Support.Model.Address) = iter.Current
                    _to.Add(Address.Parse(item)) |> ignore
                ((_to :> obj) :?> System.Collections.Generic.List<Address>)
            
            static member Parse  (from:ServiceStack.Translators.Generator.Tests.Support.Model.Address) =
                let mutable from = from
                let mutable (_to:Address) = new Address()
                _to.Line1 <- from.Line1
                _to.Line2 <- from.Line2
                ((_to :> obj) :?> Address)
            
            abstract UpdateModel : ServiceStack.Translators.Generator.Tests.Support.Model.Address -> ServiceStack.Translators.Generator.Tests.Support.Model.Address
            default this.UpdateModel  (model:ServiceStack.Translators.Generator.Tests.Support.Model.Address) =
                let mutable model = model
                model.Line1 <- Line1
                model.Line2 <- Line2
                ((model :> obj) :?> ServiceStack.Translators.Generator.Tests.Support.Model.Address)
            
            abstract ToModel : unit -> ServiceStack.Translators.Generator.Tests.Support.Model.Address
            default this.ToModel  () =
                ((this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Address()) :> obj) :?> ServiceStack.Translators.Generator.Tests.Support.Model.Address)
        end