namespace Sakila.ServiceModel.Version100.Types
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class Address
    {
        
        public virtual Sakila.DomainModel.Address ToModel()
        {
			var model = new Sakila.DomainModel.Address {
				Id = this.Id,
				Line1 = this.Line1,
				Line2 = this.Line2,
				Town = this.Town,
				City = this.City.ToModel(),
				PostCode = this.PostCode,
			};
			return model;
        }
        
        public static Address Parse(Sakila.DomainModel.Address from)
        {
			var to = new Address {
				Id = from.Id,
				Line1 = from.Line1,
				Line2 = from.Line2,
				Town = from.Town,
				City = City.Parse(from.City),
				PostCode = from.PostCode,
			};
			return to;
        }
        
        public static List<Address> ParseAll(IEnumerable<Sakila.DomainModel.Address> from)
        {
			var to = new List<Address>();
			foreach (var item in from)
			{
				to.Add(Parse(item));
			}
			return to;
        }
    }
}
