using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ICityManager : IManagerBase<City, ushort>
    {
		// Get Methods
		IList<City> GetBycountry_id(System.UInt16 countryId);
    }

    partial class CityManager : ManagerBase<City, ushort>, ICityManager
    {
		#region Constructors
		
		public CityManager() : base()
        {
        }
        public CityManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<City> GetBycountry_id(System.UInt16 countryId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(City));
			
			ICriteria countryCriteria = criteria.CreateCriteria("CountryMember");
            countryCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", countryId));
			
			return criteria.List<City>();
        }
		
		#endregion
    }
}