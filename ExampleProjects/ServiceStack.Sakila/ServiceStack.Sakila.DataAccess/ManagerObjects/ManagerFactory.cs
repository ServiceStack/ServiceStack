using System;
using System.Collections.Generic;
using System.Text;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public interface IManagerFactory
    {
		// Get Methods
		IActorManager GetActorManager();
		IActorManager GetActorManager(INHibernateSession session);
		IaddressManager GetaddressManager();
		IaddressManager GetaddressManager(INHibernateSession session);
		ICategoryManager GetCategoryManager();
		ICategoryManager GetCategoryManager(INHibernateSession session);
		ICityManager GetCityManager();
		ICityManager GetCityManager(INHibernateSession session);
		ICountryManager GetCountryManager();
		ICountryManager GetCountryManager(INHibernateSession session);
		ICustomerManager GetCustomerManager();
		ICustomerManager GetCustomerManager(INHibernateSession session);
		IFilmManager GetFilmManager();
		IFilmManager GetFilmManager(INHibernateSession session);
		IFilmTextManager GetFilmTextManager();
		IFilmTextManager GetFilmTextManager(INHibernateSession session);
		IInventoryManager GetInventoryManager();
		IInventoryManager GetInventoryManager(INHibernateSession session);
		ILanguageManager GetLanguageManager();
		ILanguageManager GetLanguageManager(INHibernateSession session);
		IPaymentManager GetPaymentManager();
		IPaymentManager GetPaymentManager(INHibernateSession session);
		IRentalManager GetRentalManager();
		IRentalManager GetRentalManager(INHibernateSession session);
		IStaffManager GetStaffManager();
		IStaffManager GetStaffManager(INHibernateSession session);
		IStoreManager GetStoreManager();
		IStoreManager GetStoreManager(INHibernateSession session);
    }

    public class ManagerFactory : IManagerFactory
    {
        #region Constructors

        public ManagerFactory()
        {
        }

        #endregion

        #region Get Methods

		public IActorManager GetActorManager()
        {
            return new ActorManager();
        }
		public IActorManager GetActorManager(INHibernateSession session)
        {
            return new ActorManager(session);
        }
		public IaddressManager GetaddressManager()
        {
            return new addressManager();
        }
		public IaddressManager GetaddressManager(INHibernateSession session)
        {
            return new addressManager(session);
        }
		public ICategoryManager GetCategoryManager()
        {
            return new CategoryManager();
        }
		public ICategoryManager GetCategoryManager(INHibernateSession session)
        {
            return new CategoryManager(session);
        }
		public ICityManager GetCityManager()
        {
            return new CityManager();
        }
		public ICityManager GetCityManager(INHibernateSession session)
        {
            return new CityManager(session);
        }
		public ICountryManager GetCountryManager()
        {
            return new CountryManager();
        }
		public ICountryManager GetCountryManager(INHibernateSession session)
        {
            return new CountryManager(session);
        }
		public ICustomerManager GetCustomerManager()
        {
            return new CustomerManager();
        }
		public ICustomerManager GetCustomerManager(INHibernateSession session)
        {
            return new CustomerManager(session);
        }
		public IFilmManager GetFilmManager()
        {
            return new FilmManager();
        }
		public IFilmManager GetFilmManager(INHibernateSession session)
        {
            return new FilmManager(session);
        }
		public IFilmTextManager GetFilmTextManager()
        {
            return new FilmTextManager();
        }
		public IFilmTextManager GetFilmTextManager(INHibernateSession session)
        {
            return new FilmTextManager(session);
        }
		public IInventoryManager GetInventoryManager()
        {
            return new InventoryManager();
        }
		public IInventoryManager GetInventoryManager(INHibernateSession session)
        {
            return new InventoryManager(session);
        }
		public ILanguageManager GetLanguageManager()
        {
            return new LanguageManager();
        }
		public ILanguageManager GetLanguageManager(INHibernateSession session)
        {
            return new LanguageManager(session);
        }
		public IPaymentManager GetPaymentManager()
        {
            return new PaymentManager();
        }
		public IPaymentManager GetPaymentManager(INHibernateSession session)
        {
            return new PaymentManager(session);
        }
		public IRentalManager GetRentalManager()
        {
            return new RentalManager();
        }
		public IRentalManager GetRentalManager(INHibernateSession session)
        {
            return new RentalManager(session);
        }
		public IStaffManager GetStaffManager()
        {
            return new StaffManager();
        }
		public IStaffManager GetStaffManager(INHibernateSession session)
        {
            return new StaffManager(session);
        }
		public IStoreManager GetStoreManager()
        {
            return new StoreManager();
        }
		public IStoreManager GetStoreManager(INHibernateSession session)
        {
            return new StoreManager(session);
        }
        
        #endregion
    }
}
