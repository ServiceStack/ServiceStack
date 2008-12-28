using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IFilmManager : IManagerBase<Film, ushort>
    {
		// Get Methods
		IList<Film> GetBylanguage_id(System.Byte languageId);
		IList<Film> GetByoriginal_language_id(System.Byte originalLanguageId);
		IList<Film> GetBytitle(System.String title);
    }

    partial class FilmManager : ManagerBase<Film, ushort>, IFilmManager
    {
		#region Constructors
		
		public FilmManager() : base()
        {
        }
        public FilmManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Film> GetBylanguage_id(System.Byte languageId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Film));
			
			ICriteria languageCriteria = criteria.CreateCriteria("LanguageMember");
            languageCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", languageId));
			
			return criteria.List<Film>();
        }
		
		public IList<Film> GetByoriginal_language_id(System.Byte originalLanguageId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Film));
			
			ICriteria languageCriteria = criteria.CreateCriteria("LanguageMember");
            languageCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", originalLanguageId));
			
			return criteria.List<Film>();
        }
		
		public IList<Film> GetBytitle(System.String title)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Film));
			
			criteria.Add(NHibernate.Criterion.Expression.Eq("Title", title));
			
			return criteria.List<Film>();
        }
		
		#endregion
    }
}