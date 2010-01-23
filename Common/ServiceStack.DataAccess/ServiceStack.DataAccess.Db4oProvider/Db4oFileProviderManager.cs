using System;
using System.Collections.Generic;
using Db4objects.Db4o;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4OFileProviderManager : IPersistenceProviderManager
	{
		private readonly ILog log = LogManager.GetLogger(typeof(Db4OFileProviderManager));

		private IPersistenceProvider provider;
		public string ConnectionString { get; private set; }

		public Db4OFileProviderManager(string filePath)
		{
			ConnectionString = filePath;
		}

		/// <summary>
		/// When accessing a db4o embedded database we should use the same instance.
		/// </summary>
		/// <returns></returns>
		public IPersistenceProvider GetProvider()
		{
			if (this.provider == null)
			{
				var db = Db4oFactory.OpenFile(this.ConnectionString);
				this.provider = new Db4OPersistenceProvider(db, this);
			}
			return this.provider;
		}

		internal void ProviderDispose(IPersistenceProvider dispose)
		{
			//end of using statement, do nothing for a db4o embedded db
		}

		~Db4OFileProviderManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			if (this.provider == null) return;
			
			log.DebugFormat("Disposing Db4oPersistenceProvider...");
			try
			{
				//((Db4OPersistenceProvider)this.provider).ObjectContainer.Close();
				((Db4OPersistenceProvider)this.provider).ObjectContainer.Dispose();
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Error disposing: Db4OFileProviderManager.Dispose(): ", ex);
			}
			provider = null;
		}
	}
}