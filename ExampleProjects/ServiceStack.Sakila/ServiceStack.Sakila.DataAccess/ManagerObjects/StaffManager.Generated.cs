using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IStaffManager : IManagerBase<Staff, byte>
    {
		// Get Methods
		IList<Staff> GetByaddress_id(System.UInt16 addressId);
		IList<Staff> GetBystore_id(System.Byte storeId);
    }

    partial class StaffManager : ManagerBase<Staff, byte>, IStaffManager
    {
		#region Constructors
		
		public StaffManager() : base()
        {
        }
        public StaffManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Staff> GetByaddress_id(System.UInt16 addressId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Staff));
			
			ICriteria addressCriteria = criteria.CreateCriteria("addressMember");
            addressCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", addressId));
			
			return criteria.List<Staff>();
        }
		
		public IList<Staff> GetBystore_id(System.Byte storeId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Staff));
			
			ICriteria storeCriteria = criteria.CreateCriteria("StoreMember");
            storeCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", storeId));
			
			return criteria.List<Staff>();
        }
		
		#endregion
    }
}