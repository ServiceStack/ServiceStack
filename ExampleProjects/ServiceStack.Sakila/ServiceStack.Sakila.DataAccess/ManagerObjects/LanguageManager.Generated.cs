using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ILanguageManager : IManagerBase<Language, byte>
    {
		// Get Methods
    }

    partial class LanguageManager : ManagerBase<Language, byte>, ILanguageManager
    {
		#region Constructors
		
		public LanguageManager() : base()
        {
        }
        public LanguageManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		#endregion
    }
}