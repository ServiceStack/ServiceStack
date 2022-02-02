using System;

using ServiceStack.OrmLite.SqlServer.Converters;

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServer2016OrmLiteDialectProvider : SqlServer2014OrmLiteDialectProvider
    {
		public SqlServer2016OrmLiteDialectProvider() : base()
		{
			base.RegisterConverter<String>(new SqlServerJsonStringConverter());
		}

        public new static SqlServer2016OrmLiteDialectProvider Instance = new SqlServer2016OrmLiteDialectProvider();

        public override SqlExpression<T> SqlExpression<T>() => new SqlServer2016Expression<T>(this);
    }
}