using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ICountryManager : IManagerBase<Country, ushort>
    {
		// Get Methods
    }

    partial class CountryManager : ManagerBase<Country, ushort>, ICountryManager
    {
		#region Constructors
		
		public CountryManager() : base()
        {
        }
        public CountryManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		#endregion
    }
}