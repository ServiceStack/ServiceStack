using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ICustomerManager : IManagerBase<Customer, ushort>
    {
		// Get Methods
		IList<Customer> GetByaddress_id(System.UInt16 addressId);
		IList<Customer> GetBystore_id(System.Byte storeId);
		IList<Customer> GetBylast_name(System.String lastName);
    }

    partial class CustomerManager : ManagerBase<Customer, ushort>, ICustomerManager
    {
		#region Constructors
		
		public CustomerManager() : base()
        {
        }
        public CustomerManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Customer> GetByaddress_id(System.UInt16 addressId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Customer));
			
			ICriteria addressCriteria = criteria.CreateCriteria("addressMember");
            addressCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", addressId));
			
			return criteria.List<Customer>();
        }
		
		public IList<Customer> GetBystore_id(System.Byte storeId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Customer));
			
			ICriteria storeCriteria = criteria.CreateCriteria("StoreMember");
            storeCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", storeId));
			
			return criteria.List<Customer>();
        }
		
		public IList<Customer> GetBylast_name(System.String lastName)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Customer));
			
			criteria.Add(NHibernate.Criterion.Expression.Eq("LastName", lastName));
			
			return criteria.List<Customer>();
        }
		
		#endregion
    }
}