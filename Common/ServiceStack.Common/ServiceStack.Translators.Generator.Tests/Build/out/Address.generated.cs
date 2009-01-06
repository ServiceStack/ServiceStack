namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class Address
    {
        
        public virtual ServiceStack.Translators.Generator.Tests.Support.Model.Address ToModel()
        {
			var model = new ServiceStack.Translators.Generator.Tests.Support.Model.Address {
				Line1 = this.Line1,
				Line2 = this.Line2,
			};
			return model;
        }
        
        public virtual Address Parse(ServiceStack.Translators.Generator.Tests.Support.Model.Address from)
        {
			var to = new Address {
				Line1 = from.Line1,
				Line2 = from.Line2,
			};
			return to;
        }
        
        public static List<Address> ParseAll(IEnumerable<ServiceStack.Translators.Generator.Tests.Support.Model.Address> from)
        {
			var to = new List<Address>();
			foreach (var item in from)
			{
				to.Add(new Address().Parse(item));
			}
			return to;
        }
    }
}
