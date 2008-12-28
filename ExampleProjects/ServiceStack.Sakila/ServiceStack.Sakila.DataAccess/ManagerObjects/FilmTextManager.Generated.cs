using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IFilmTextManager : IManagerBase<FilmText, short>
    {
		// Get Methods
		IList<FilmText> GetBytitledescription(System.String title, System.String description);
    }

    partial class FilmTextManager : ManagerBase<FilmText, short>, IFilmTextManager
    {
		#region Constructors
		
		public FilmTextManager() : base()
        {
        }
        public FilmTextManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<FilmText> GetBytitledescription(System.String title, System.String description)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(FilmText));
			
			criteria.Add(NHibernate.Criterion.Expression.Eq("Title", title));
			criteria.Add(NHibernate.Criterion.Expression.Eq("Description", description));
			
			return criteria.List<FilmText>();
        }
		
		#endregion
    }
}