namespace Sakila.ServiceModel.Version100.Types
{
    using System;
    using System.Collections.Generic;
    
    
    public partial class City
    {
        
        public virtual Sakila.DomainModel.City ToModel()
        {
			var model = new Sakila.DomainModel.City {
				Id = this.Id,
				Name = this.Name,
				Country = this.Country.ToModel(),
			};
			return model;
        }
        
        public virtual City Parse(Sakila.DomainModel.City from)
        {
			var to = new City {
				Id = from.Id,
				Name = from.Name,
				Country = new Country().Parse(from.Country),
			};
			return to;
        }
        
        public static List<City> ParseAll(IEnumerable<Sakila.DomainModel.City> from)
        {
			var to = new List<City>();
			foreach (var item in from)
			{
				to.Add(new City().Parse(item));
			}
			return to;
        }
    }
}
