using System;

using ServiceStack.OrmLite.SqlServer.Converters;

namespace ServiceStack.OrmLite.SqlServer;

public class SqlServer2016OrmLiteDialectProvider : SqlServer2014OrmLiteDialectProvider
{
	public SqlServer2016OrmLiteDialectProvider() : base()
	{
		base.RegisterConverter<string>(new SqlServerJsonStringConverter());
	}

	public new static SqlServer2016OrmLiteDialectProvider Instance = new();

	public override SqlExpression<T> SqlExpression<T>() => new SqlServer2016Expression<T>(this);
}

public class SqlServer2017OrmLiteDialectProvider : SqlServer2016OrmLiteDialectProvider
{
	public new static SqlServer2017OrmLiteDialectProvider Instance = new();
}

public class SqlServer2019OrmLiteDialectProvider : SqlServer2017OrmLiteDialectProvider
{
	public new static SqlServer2019OrmLiteDialectProvider Instance = new();
}

public class SqlServer2022OrmLiteDialectProvider : SqlServer2019OrmLiteDialectProvider
{
	public new static SqlServer2022OrmLiteDialectProvider Instance = new();
}
