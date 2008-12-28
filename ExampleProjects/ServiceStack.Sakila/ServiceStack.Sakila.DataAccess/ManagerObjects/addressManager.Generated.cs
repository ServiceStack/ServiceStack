using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IaddressManager : IManagerBase<address, ushort>
    {
		// Get Methods
		IList<address> GetBycity_id(System.UInt16 cityId);
    }

    partial class addressManager : ManagerBase<address, ushort>, IaddressManager
    {
		#region Constructors
		
		public addressManager() : base()
        {
        }
        public addressManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<address> GetBycity_id(System.UInt16 cityId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(address));
			
			ICriteria cityCriteria = criteria.CreateCriteria("CityMember");
            cityCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", cityId));
			
			return criteria.List<address>();
        }
		
		#endregion
    }
}