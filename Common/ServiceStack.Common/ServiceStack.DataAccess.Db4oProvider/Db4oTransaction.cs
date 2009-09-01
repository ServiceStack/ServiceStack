using System;
using Db4objects.Db4o;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4OTransaction : ITransactionContext
	{
		private IObjectContainer Provider { get; set; }

		private readonly ILog log = LogManager.GetLogger(typeof(Db4OTransaction));

		public Db4OTransaction(IObjectContainer provider)
		{
			this.Provider = provider;
			this.HasOpenTransaction = true;
		}

		public bool Commit()
		{
			var result = HasOpenTransaction;
			if (result)
			{
				try
				{
					this.Provider.Commit();
				}
				catch (Exception ex)
				{
					if (this.Provider != null)
					{
						this.Provider.Rollback();
					}
					throw;
				}
				finally
				{
					this.HasOpenTransaction = false;
				}
			}
			return result;
		}

		public bool Rollback()
		{
			var result = HasOpenTransaction;
			if (result)
			{
				this.Provider.Rollback();
			}
			return result;
		}

		public bool HasOpenTransaction { get; set; }

		~Db4OTransaction()
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