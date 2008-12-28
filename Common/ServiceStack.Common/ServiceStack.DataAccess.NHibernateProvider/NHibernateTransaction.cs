/*
// $Id: NHibernateTransaction.cs 514 2008-12-15 10:51:34Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 514 $
// Modified Date : $LastChangedDate: 2008-12-15 10:51:34 +0000 (Mon, 15 Dec 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using ServiceStack.Logging;
using NHibernate;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateTransaction : ITransactionContext
	{
		public ITransaction Transaction { get; private set; }

		private NHibernatePersistenceProvider Provider { get; set; }

		private readonly ILog log = LogManager.GetLogger(typeof(NHibernateTransaction));

		public NHibernateTransaction(ITransaction transaction, NHibernatePersistenceProvider provider)
		{
			this.Transaction = transaction;
			this.Provider = provider;
		}

		public bool Commit()
		{
			var result = HasOpenTransaction;
			if (result)
			{
				try
				{
					Transaction.Commit();
					Transaction = null;
				}
				catch (HibernateException)
				{
					if (Transaction != null)
					{
						Transaction.Rollback();
						Transaction = null;
					}
					throw;
				}
			}
			return result;
		}

		public bool Rollback()
		{
			var result = HasOpenTransaction;
			if (result)
			{
				this.Transaction.Rollback();
				this.Transaction.Dispose();
				this.Transaction = null;

				// Start a new session in the provider
				this.Provider.StartNewSession();
			}
			return result;
		}

		public bool HasOpenTransaction
		{
			get
			{
				return (this.Transaction != null && !this.Transaction.WasCommitted && !this.Transaction.WasRolledBack);
			}
		}

		~NHibernateTransaction()
		{
			Dispose(true);
		}

		public void Dispose()
		{
			Dispose(false);
		}

		private void Dispose(bool finalizing)
		{
			if (HasOpenTransaction)
			{
				log.Warn("Open Transaction that was not committed was rollbacked");
				Rollback();
			}
			if (!finalizing)
			{
				GC.SuppressFinalize(this);
			}
		}
	}
}
