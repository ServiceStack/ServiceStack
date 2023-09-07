using System;

using ServiceStack.OrmLite.SqlServer.Converters;

namespace ServiceStack.OrmLite.SqlServer;

public class SqlServerOrmLiteDialectProviderVersions : SqlServer2014OrmLiteDialectProvider
{
	public SqlServerOrmLiteDialectProviderVersions() : base()
	{
		base.RegisterConverter<String>(new SqlServerJsonStringConverter());
	}

	public new static SqlServerOrmLiteDialectProviderVersions Instance = new();

	public override SqlExpression<T> SqlExpression<T>() => new SqlServer2016Expression<T>(this);
}

public class SqlServer2017OrmLiteDialectProvider : SqlServerOrmLiteDialectProviderVersions
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
