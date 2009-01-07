namespace Sakila.ServiceModel.Version100.Types
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class Customer
	{
		
		public virtual Sakila.DomainModel.Customer ToModel()
		{
			return this.UpdateModel(new Sakila.DomainModel.Customer());
		}
		
		public virtual Sakila.DomainModel.Customer UpdateModel(Sakila.DomainModel.Customer model)
		{
			model.Id = Id;
			model.StoreId = StoreId;
			model.FirstName = FirstName;
			model.LastName = LastName;
			model.Email = Email;
			model.Address = this.Address.ToModel();
			return model;
		}
		
		public static Sakila.ServiceModel.Version100.Types.Customer Parse(Sakila.DomainModel.Customer from)
		{
			Sakila.ServiceModel.Version100.Types.Customer to = new Sakila.ServiceModel.Version100.Types.Customer();
			to.Id = from.Id;
			to.StoreId = from.StoreId;
			to.FirstName = from.FirstName;
			to.LastName = from.LastName;
			to.Email = from.Email;
			to.Address = Sakila.ServiceModel.Version100.Types.Address.Parse(from.Address);
			return to;
		}
		
		public static System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Customer> ParseAll(System.Collections.Generic.IEnumerable<Sakila.DomainModel.Customer> from)
		{
			System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Customer> to = new System.Collections.Generic.List<Sakila.ServiceModel.Version100.Types.Customer>();
			for (System.Collections.Generic.IEnumerator<Sakila.DomainModel.Customer> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				Sakila.DomainModel.Customer item = iter.Current;
				to.Add(Sakila.ServiceModel.Version100.Types.Customer.Parse(item));
			}
			return to;
		}
	}
}
