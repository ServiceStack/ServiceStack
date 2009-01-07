namespace Sakila.ServiceModel.Version100.Types
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class Country
	{
		
		public virtual Sakila.DomainModel.Country ToModel()
		{
			return this.UpdateModel(new Sakila.DomainModel.Country());
		}
		
		public virtual Sakila.DomainModel.Country UpdateModel(Sakila.DomainModel.Country model)
		{
			model.Id = Id;
			model.Name = Name;
			return model;
		}
		
		public static Sakila.ServiceModel.Version100.Types.Country Parse(Sakila.DomainModel.Country from)
		{
			Sakila.ServiceModel.Version100.Types.Country to = new Sakila.ServiceModel.Version100.Types.Country();
			to.Id = from.Id;
			to.Name = from.Name;
			return to;
		}
		
		public static System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Country> ParseAll(System.Collections.Generic.IEnumerable<Sakila.DomainModel.Country> from)
		{
			System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Country> to = new System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Country>();
			for (System.Collections.Generic.IEnumerator<Sakila.DomainModel.Country> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				Sakila.DomainModel.Country item = iter.Current;
				to.Add(Sakila.ServiceModel.Version100.Types.Country.Parse(item));
			}
			return to;
		}
	}
}
