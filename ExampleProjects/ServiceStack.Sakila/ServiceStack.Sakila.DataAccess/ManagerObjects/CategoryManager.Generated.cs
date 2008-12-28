using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ICategoryManager : IManagerBase<Category, byte>
    {
		// Get Methods
    }

    partial class CategoryManager : ManagerBase<Category, byte>, ICategoryManager
    {
		#region Constructors
		
		public CategoryManager() : base()
        {
        }
        public CategoryManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		#endregion
    }
}