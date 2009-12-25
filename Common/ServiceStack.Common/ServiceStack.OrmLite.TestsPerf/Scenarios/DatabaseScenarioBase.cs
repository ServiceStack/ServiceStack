using System;
using System.Data;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios
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

		private IDbCommand dbCmd;
		protected IDbCommand DbCmd
		{
			get
			{
				if (dbCmd == null)
				{
					dbCmd = ConnectionString.OpenDbConnection().CreateCommand();
				}
				return dbCmd;
			}
		}

		public override void Run()
		{
			Run(DbCmd);
			this.Iteration++;
		}

		protected abstract void Run(IDbCommand dbCmd);

		public void Dispose()
		{
			if (this.dbCmd == null) return;

			var dbConn = this.dbCmd.Connection;
			try
			{
				this.dbCmd.Dispose();
				this.dbCmd = null;
			}
			finally
			{
				if (dbConn != null)
				{
					dbConn.Dispose();
				}
			}
		}
	}
}