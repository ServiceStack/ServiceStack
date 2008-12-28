using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IActorManager : IManagerBase<Actor, ushort>
    {
		// Get Methods
		IList<Actor> GetBylast_name(System.String lastName);
    }

    partial class ActorManager : ManagerBase<Actor, ushort>, IActorManager
    {
		#region Constructors
		
		public ActorManager() : base()
        {
        }
        public ActorManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Actor> GetBylast_name(System.String lastName)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Actor));
			
			criteria.Add(NHibernate.Criterion.Expression.Eq("LastName", lastName));
			
			return criteria.List<Actor>();
        }
		
		#endregion
    }
}