namespace Sakila.ServiceModel.Version100.Types
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class Customer
    {
        
        public virtual Sakila.DomainModel.Customer ToModel()
        {
			var model = new Sakila.DomainModel.Customer {
				Id = this.Id,
				StoreId = this.StoreId,
				FirstName = this.FirstName,
				LastName = this.LastName,
				Email = this.Email,
				Address = this.Address.ToModel(),
			};
			return model;
        }
        
        public virtual Customer Parse(Sakila.DomainModel.Customer from)
        {
			var to = new Customer {
				Id = from.Id,
				StoreId = from.StoreId,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Email = from.Email,
				Address = new Address().Parse(from.Address),
			};
			return to;
        }
        
        public static List<Customer> ParseAll(IEnumerable<Sakila.DomainModel.Customer> from)
        {
			var to = new List<Customer>();
			foreach (var item in from)
			{
				to.Add(new Customer().Parse(item));
			}
			return to;
        }
    }
}
