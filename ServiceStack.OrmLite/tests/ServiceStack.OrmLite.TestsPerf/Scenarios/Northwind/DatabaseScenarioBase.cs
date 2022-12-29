using System;
using System.Data;
using ServiceStack.OrmLite;

namespace Northwind.Perf
{
	public abstract class DatabaseScenarioBase
		: ScenarioBase, IDisposable
	{
		public string ConnectionString { get; set; }

		protected int Iteration;
		public bool IsFirstRun
		{
			get
			{
				return this.Iteration == 0;
			}
		}

        private IDbConnection db;
        protected IDbConnection Db
		{
			get
			{
				if (db == null)
				{
				    var connStr = ConnectionString;
                    db = connStr.OpenDbConnection();
				}
				return db;
			}
		}

		public override void Run()
		{
			Run(Db);
			this.Iteration++;
		}

        protected abstract void Run(IDbConnection db);

		public void Dispose()
		{
			if (this.db == null) return;

			try
			{
				this.db.Dispose();
				this.db = null;
			}
			finally
			{
			}
		}
	}
}