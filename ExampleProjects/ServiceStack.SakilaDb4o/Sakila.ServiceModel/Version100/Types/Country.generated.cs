namespace Sakila.ServiceModel.Version100.Types
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class Country
    {
        
        public virtual Sakila.DomainModel.Country ToModel()
        {
			var model = new Sakila.DomainModel.Country {
				Id = this.Id,
				Name = this.Name,
			};
			return model;
        }
        
        public virtual Country Parse(Sakila.DomainModel.Country from)
        {
			var to = new Country {
				Id = from.Id,
				Name = from.Name,
			};
			return to;
        }
        
        public static List<Country> ParseAll(IEnumerable<Sakila.DomainModel.Country> from)
        {
			var to = new List<Country>();
			foreach (var item in from)
			{
				to.Add(new Country().Parse(item));
			}
			return to;
        }
    }
}
