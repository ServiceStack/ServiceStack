using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IRentalManager : IManagerBase<Rental, int>
    {
		// Get Methods
		IList<Rental> GetBycustomer_id(System.UInt16 customerId);
		IList<Rental> GetByinventory_id(System.UInt32 inventoryId);
		IList<Rental> GetBystaff_id(System.Byte staffId);
		IList<Rental> GetByrental_dateinventory_idcustomer_id(System.DateTime rentalDate, System.UInt32 inventoryId, System.UInt16 customerId);
    }

    partial class RentalManager : ManagerBase<Rental, int>, IRentalManager
    {
		#region Constructors
		
		public RentalManager() : base()
        {
        }
        public RentalManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Rental> GetBycustomer_id(System.UInt16 customerId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Rental));
			
			ICriteria customerCriteria = criteria.CreateCriteria("CustomerMember");
            customerCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", customerId));
			
			return criteria.List<Rental>();
        }
		
		public IList<Rental> GetByinventory_id(System.UInt32 inventoryId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Rental));
			
			ICriteria inventoryCriteria = criteria.CreateCriteria("InventoryMember");
            inventoryCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", inventoryId));
			
			return criteria.List<Rental>();
        }
		
		public IList<Rental> GetBystaff_id(System.Byte staffId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Rental));
			
			ICriteria staffCriteria = criteria.CreateCriteria("StaffMember");
            staffCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", staffId));
			
			return criteria.List<Rental>();
        }
		
		public IList<Rental> GetByrental_dateinventory_idcustomer_id(System.DateTime rentalDate, System.UInt32 inventoryId, System.UInt16 customerId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Rental));
			
			criteria.Add(NHibernate.Criterion.Expression.Eq("RentalDate", rentalDate));
			ICriteria inventoryCriteria = criteria.CreateCriteria("InventoryMember");
            inventoryCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", inventoryId));
			ICriteria customerCriteria = criteria.CreateCriteria("CustomerMember");
            customerCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", customerId));
			
			return criteria.List<Rental>();
        }
		
		#endregion
    }
}