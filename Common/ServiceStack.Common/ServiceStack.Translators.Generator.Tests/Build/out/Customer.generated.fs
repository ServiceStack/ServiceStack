
namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
    #nowarn "49" // uppercase argument names
    #nowarn "67" // this type test or downcast will always hold
    #nowarn "66" // this upcast is unnecessary - the types are identical
    #nowarn "58" // possible incorrect indentation..
    #nowarn "57" // do not use create_DelegateEvent
    #nowarn "51" // address-of operator can occur in the code
    open System
    open System.Collections.Generic
    
    exception ReturnExceptiona2c85b7b15c94b3ba1a02f3a62886423 of obj
    exception ReturnNoneExceptiona2c85b7b15c94b3ba1a02f3a62886423
    
    type
        
        (* partial *)Customer = class
            static member ParseAll  (from:System.Collections.Generic.IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Customer>) =
                let mutable from = from
                let mutable (_to:System.Collections.Generic.List<Customer>) = new System.Collections.Generic.List<Customer>()
                let mutable (iter:System.Collections.Generic.IEnumerator<ServiceStack.Translators.Generator.Tests.Support.Model.Customer>) = from.GetEnumerator()
                while iter.MoveNext() do
                    let mutable (item:ServiceStack.Translators.Generator.Tests.Support.Model.Customer) = iter.Current
                    _to.Add(Customer.Parse(item)) |> ignore
                ((_to :> obj) :?> System.Collections.Generic.List<Customer>)
            
            static member Parse  (from:ServiceStack.Translators.Generator.Tests.Support.Model.Customer) =
                let mutable from = from
                let mutable (_to:Customer) = new Customer()
                _to.Id <- from.Id
                _to.Name <- from.Name
                _to.BillingAddress <- Address.Parse(from.BillingAddress)
                ((_to :> obj) :?> Customer)
            
            abstract UpdateModel : ServiceStack.Translators.Generator.Tests.Support.Model.Customer -> ServiceStack.Translators.Generator.Tests.Support.Model.Customer
            default this.UpdateModel  (model:ServiceStack.Translators.Generator.Tests.Support.Model.Customer) =
                let mutable model = model
                model.Id <- Id
                model.Name <- Name
                model.BillingAddress <- this.BillingAddress.ToModel()
                ((model :> obj) :?> ServiceStack.Translators.Generator.Tests.Support.Model.Customer)
            
            abstract ToModel : unit -> ServiceStack.Translators.Generator.Tests.Support.Model.Customer
            default this.ToModel  () =
                ((this.UpdateModel(new ServiceStack.Translators.Generator.Tests.Support.Model.Customer()) :> obj) :?> ServiceStack.Translators.Generator.Tests.Support.Model.Customer)
        end