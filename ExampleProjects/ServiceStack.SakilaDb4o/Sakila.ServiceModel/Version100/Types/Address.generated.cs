namespace Sakila.ServiceModel.Version100.Types
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class Address
	{
		
		public virtual Sakila.DomainModel.Address ToModel()
		{
			return this.UpdateModel(new Sakila.DomainModel.Address());
		}
		
		public virtual Sakila.DomainModel.Address UpdateModel(Sakila.DomainModel.Address model)
		{
			model.Id = Id;
			model.Line1 = Line1;
			model.Line2 = Line2;
			model.Town = Town;
			model.City = this.City.ToModel();
			model.PostCode = PostCode;
			return model;
		}
		
		public static Sakila.ServiceModel.Version100.Types.Address Parse(Sakila.DomainModel.Address from)
		{
			Sakila.ServiceModel.Version100.Types.Address to = new Sakila.ServiceModel.Version100.Types.Address();
			to.Id = from.Id;
			to.Line1 = from.Line1;
			to.Line2 = from.Line2;
			to.Town = from.Town;
			to.City = Sakila.ServiceModel.Version100.Types.City.Parse(from.City);
			to.PostCode = from.PostCode;
			return to;
		}
		
		public static System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Address> ParseAll(System.Collections.Generic.IEnumerable<Sakila.DomainModel.Address> from)
		{
			System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Address> to = new System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Address>();
			for (System.Collections.Generic.IEnumerator<Sakila.DomainModel.Address> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				Sakila.DomainModel.Address item = iter.Current;
				to.Add(Sakila.ServiceModel.Version100.Types.Address.Parse(item));
			}
			return to;
		}
	}
}
