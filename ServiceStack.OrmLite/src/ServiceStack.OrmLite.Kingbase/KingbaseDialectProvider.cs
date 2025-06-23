using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.PostgreSQL.Converters;
using ServiceStack.Text;

// ReSharper disable ConvertToPrimaryConstructor

namespace ServiceStack.OrmLite.Kingbase;

public class KingbaseDialectProvider : OrmLiteDialectProviderBase<KingbaseDialectProvider>
{
    public static KingbaseDialectProvider
        InstanceForMySqlConnector = new KingbaseDialectProvider(MySqlConnectorDialect.Instance);

    private readonly PostgreSqlDialectProvider innerProvider;
    private readonly IOrmLiteDialectProvider flavorProvider;

    public KingbaseDialectProvider(IOrmLiteDialectProvider flavorProvider)
    {
        innerProvider = new PostgreSqlDialectProvider();

        this.flavorProvider = flavorProvider ?? throw new ArgumentNullException(nameof(flavorProvider));

        //copy from PostgreSqlDialectProvider
        AutoIncrementDefinition = "";
        ParamString = ":";
        base.SelectIdentitySql = flavorProvider.SelectIdentitySql;
        innerProvider.NamingStrategy = NamingStrategy = flavorProvider.NamingStrategy;
        StringSerializer = new JsonStringSerializer();

        InitColumnTypeMap();

        RowVersionConverter = new PostgreSqlRowVersionConverter();

        RegisterConverter<string>(new PostgreSqlStringConverter());
        RegisterConverter<char[]>(new PostgreSqlCharArrayConverter());

        RegisterConverter<bool>(new PostgreSqlBoolConverter());
        RegisterConverter<Guid>(new PostgreSqlGuidConverter());

        RegisterConverter<DateTime>(new PostgreSqlDateTimeConverter());
        RegisterConverter<DateTimeOffset>(new PostgreSqlDateTimeOffsetConverter());


        RegisterConverter<sbyte>(new PostrgreSqlSByteConverter());
        RegisterConverter<ushort>(new PostrgreSqlUInt16Converter());
        RegisterConverter<uint>(new PostrgreSqlUInt32Converter());
        RegisterConverter<ulong>(new PostrgreSqlUInt64Converter());

        RegisterConverter<float>(new PostrgreSqlFloatConverter());
        RegisterConverter<double>(new PostrgreSqlDoubleConverter());
        RegisterConverter<decimal>(new PostrgreSqlDecimalConverter());

        RegisterConverter<byte[]>(new PostrgreSqlByteArrayConverter());

        //TODO provide support for pgsql native data structures:
        RegisterConverter<string[]>(new PostgreSqlStringArrayConverter());
        RegisterConverter<short[]>(new PostgreSqlShortArrayConverter());
        RegisterConverter<int[]>(new PostgreSqlIntArrayConverter());
        RegisterConverter<long[]>(new PostgreSqlLongArrayConverter());
        RegisterConverter<float[]>(new PostgreSqlFloatArrayConverter());
        RegisterConverter<double[]>(new PostgreSqlDoubleArrayConverter());
        RegisterConverter<decimal[]>(new PostgreSqlDecimalArrayConverter());
        RegisterConverter<DateTime[]>(new PostgreSqlDateTimeTimeStampArrayConverter());
        RegisterConverter<DateTimeOffset[]>(new PostgreSqlDateTimeOffsetTimeStampTzArrayConverter());

        RegisterConverter<XmlValue>(new PostgreSqlXmlConverter());

#if NET6_0_OR_GREATER
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.EnableLegacyCaseInsensitiveDbParameters", true);
        RegisterConverter<DateOnly>(new PostgreSqlDateOnlyConverter());
#endif

#if NET472
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#endif

        Variables = flavorProvider.Variables;

        //this.ExecFilter = new PostgreSqlExecFilter {
        //    OnCommand = cmd => cmd.AllResultTypesAreUnknown = true
        //};
    }


    public override string ToCreateSchemaStatement(string schemaName)
    {
        return innerProvider.ToCreateSchemaStatement(schemaName);
    }

    public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
    {
        return innerProvider.DoesSchemaExist(dbCmd, schemaName);
    }

    public override IDbDataParameter CreateParam()
    {
        return innerProvider.CreateParam();
    }

    public override IDbConnection CreateConnection(string filePath, Dictionary<string, string> options)
    {
        return innerProvider.CreateConnection(filePath, options);
    }

    public override bool DoesTableExist(IDbConnection db, string tableName, string schema = null)
    {
        return innerProvider.DoesTableExist(db, tableName, schema);
    }

    public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
    {
        return innerProvider.DoesTableExist(dbCmd, tableName, schema);
    }

    public override string ToCreateTableStatement(Type tableType)
    {
        return innerProvider.ToCreateTableStatement(tableType);
    }
}