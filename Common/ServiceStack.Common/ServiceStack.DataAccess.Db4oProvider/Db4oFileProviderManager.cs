using System;
using System.Collections.Generic;
using Db4objects.Db4o;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4oFileProviderManager : IPersistenceProviderManager
	{
		private readonly ILog log = LogManager.GetLogger(typeof(Db4oFileProviderManager));

		private IPersistenceProvider provider;
		public string ConnectionString { get; private set; }

		public Db4oFileProviderManager(string filePath)
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
				this.provider = new Db4oPersistenceProvider(db);
			}
			return this.provider;
		}

		~Db4oFileProviderManager()
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

			try
			{
				log.DebugFormat("Disposing Db4oFileProviderManager...");
				provider.Dispose();
			}
			catch (Exception ex)
			{
				log.Error("Error disposing of Db4o provider", ex);
			}
			provider = null;
		}
	}
}