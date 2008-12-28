using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IStoreManager : IManagerBase<Store, byte>
    {
		// Get Methods
		IList<Store> GetByaddress_id(System.UInt16 addressId);
		Store GetBymanager_staff_id(System.Byte managerStaffId);
    }

    partial class StoreManager : ManagerBase<Store, byte>, IStoreManager
    {
		#region Constructors
		
		public StoreManager() : base()
        {
        }
        public StoreManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Store> GetByaddress_id(System.UInt16 addressId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Store));
			
			ICriteria addressCriteria = criteria.CreateCriteria("addressMember");
            addressCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", addressId));
			
			return criteria.List<Store>();
        }
		
		public Store GetBymanager_staff_id(System.Byte managerStaffId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Store));
			
			ICriteria staffCriteria = criteria.CreateCriteria("StaffMember");
            staffCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", managerStaffId));
			
			IList<Store> result = criteria.List<Store>();
			return (result.Count > 0) ? result[0] : null;
        }
		
		#endregion
    }
}