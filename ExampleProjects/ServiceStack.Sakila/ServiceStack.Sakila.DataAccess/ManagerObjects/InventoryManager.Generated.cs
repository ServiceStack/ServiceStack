using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IInventoryManager : IManagerBase<Inventory, uint>
    {
		// Get Methods
		IList<Inventory> GetByfilm_id(System.UInt16 filmId);
		IList<Inventory> GetBystore_id(System.Byte storeId);
		IList<Inventory> GetBystore_idfilm_id(System.Byte storeId, System.UInt16 filmId);
    }

    partial class InventoryManager : ManagerBase<Inventory, uint>, IInventoryManager
    {
		#region Constructors
		
		public InventoryManager() : base()
        {
        }
        public InventoryManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Inventory> GetByfilm_id(System.UInt16 filmId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Inventory));
			
			ICriteria filmCriteria = criteria.CreateCriteria("FilmMember");
            filmCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", filmId));
			
			return criteria.List<Inventory>();
        }
		
		public IList<Inventory> GetBystore_id(System.Byte storeId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Inventory));
			
			ICriteria storeCriteria = criteria.CreateCriteria("StoreMember");
            storeCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", storeId));
			
			return criteria.List<Inventory>();
        }
		
		public IList<Inventory> GetBystore_idfilm_id(System.Byte storeId, System.UInt16 filmId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Inventory));
			
			ICriteria storeCriteria = criteria.CreateCriteria("StoreMember");
            storeCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", storeId));
			ICriteria filmCriteria = criteria.CreateCriteria("FilmMember");
            filmCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", filmId));
			
			return criteria.List<Inventory>();
        }
		
		#endregion
    }
}