using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;
using NHibernate;
using NHibernate.Criterion;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernatePersistenceProvider : IPersistenceProvider
	{
		public ISession Session { get; private set; }
		
		private bool disposed;
		
		private ISessionFactory SessionFactory { get; set; }

		private const string ID_FIELD = "Id";

		private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public NHibernatePersistenceProvider(ISessionFactory sessionFactory)
		{
			this.SessionFactory = sessionFactory;
			this.Session = this.SessionFactory.OpenSession();
		}

		public object LoadById(Type theType, object id)
		{
			return this.Session.Load(theType, id);
		}

		public void Revert(object obj)
		{
			this.Session.Evict(obj);
		}

		public ITransactionContext BeginTransaction()
		{
			this.Session.Transaction.Begin();
			return new NHibernateTransaction(Session.Transaction, this);
		}

		public void Flush()
		{
			this.Session.Flush();
		}

		public T FindByValue<T>(string name, object value) where T : class
		{
			var list = FindAllByValue<T>(name, value);
			return list.Count == 0 ? null : list[0];
		}

		public IList<T> FindAllByValue<T>(string name, object value) where T : class
		{
			var list = this.Session.CreateCriteria(typeof (T))
				.Add(Restrictions.Eq(name, value)).ToList<T>();

			return list;
		}

		public IList<T> FindByValues<T>(string name, object[] values) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .Add(Restrictions.Eq(name, values))
				 .ToList<T>();

			return list;
		}

		public IList<T> FindByValues<T>(string name, ICollection values) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .Add(Restrictions.In(name, values))
				 .ToList<T>();

			return list;
		}

		public IList<T> GetAll<T>(string orderBy) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .AddOrder(Order.Asc(orderBy))
				 .ToList<T>();
			return list;
		}

		public T GetById<T>(object id) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .Add(Restrictions.Eq(ID_FIELD, id))
				 .List();

			if (list.Count == 0)
			{
				return null;
			}

			return (T)list[0];
		}

		public IList<T> GetByIds<T>(object[] ids) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .Add(Restrictions.In(ID_FIELD, ids))
				 .ToList<T>();

			return list;
		}

		public IList<T> GetByIds<T>(ICollection ids) where T : class
		{
			var list = this.Session.CreateCriteria(typeof(T))
				 .Add(Restrictions.In(ID_FIELD, ids))
				 .ToList<T>();

			return list;
		}

		public IList<T> GetAll<T>() where T : class
		{
			return this.Session.CreateCriteria(typeof(T)).ToList<T>();
		}

		public IList<T> GetAllOrderedBy<T>(string fieldName, bool sortAsc) where T : class
		{
			var orderBy = sortAsc ? Order.Asc(fieldName) : Order.Desc(fieldName);
			return this.Session.CreateCriteria(typeof(T))
				 .AddOrder(orderBy)
				 .ToList<T>();
		}

		public T Store<T>(T obj) where T : class
		{
			this.Session.Save(obj);
			return obj;
		}

		public void Delete<T>(T obj) where T : class
		{
			this.Session.Delete(obj);
		}

		~NHibernatePersistenceProvider()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposed) return;

			Close();
			
			if (disposing)
			{
				GC.SuppressFinalize(this);
			}
			
			disposed = true;
		}

		public void Close()
		{
			if (!Session.IsOpen) return;
			try
			{
				Session.Flush();
			}
			catch (Exception ex)
			{
				//Says that we're trying to flush a disposed connection when Session.IsOpen == true.
				log.Error(ex.Message, ex);
			}
			try
			{
				Session.Close();
			}
			catch (Exception ex)
			{
				log.Error(ex.Message, ex);
			}
		}

		/// <summary>
		/// Called by NHibernateTransaction on Rollback
		/// </summary>
		internal void StartNewSession()
		{
			this.Session.Close();
			this.Session.Dispose();
			this.Session = this.SessionFactory.OpenSession();
		}

		public bool IsOpen
		{
			get { return this.Session.IsOpen; }
		}

	}
}
