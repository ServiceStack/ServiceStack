namespace Sakila.ServiceModel.Version100.Types
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class City
	{
		
		public virtual Sakila.DomainModel.City ToModel()
		{
			return this.UpdateModel(new Sakila.DomainModel.City());
		}
		
		public virtual Sakila.DomainModel.City UpdateModel(Sakila.DomainModel.City model)
		{
			model.Id = Id;
			model.Name = Name;
			model.Country = this.Country.ToModel();
			return model;
		}
		
		public static Sakila.ServiceModel.Version100.Types.City Parse(Sakila.DomainModel.City from)
		{
			Sakila.ServiceModel.Version100.Types.City to = new Sakila.ServiceModel.Version100.Types.City();
			to.Id = from.Id;
			to.Name = from.Name;
			to.Country = Sakila.ServiceModel.Version100.Types.Country.Parse(from.Country);
			return to;
		}
		
		public static System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.City> ParseAll(System.Collections.Generic.IEnumerable<Sakila.DomainModel.City> from)
		{
			System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.City> to = new System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.City>();
			for (System.Collections.Generic.IEnumerator<Sakila.DomainModel.City> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				Sakila.DomainModel.City item = iter.Current;
				to.Add(Sakila.ServiceModel.Version100.Types.City.Parse(item));
			}
			return to;
		}
	}
}
