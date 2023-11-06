//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite
{
    public static class OrmLiteConfig
    {
        public const string IdField = "Id";

        private const int DefaultCommandTimeout = 30;
        private static int? commandTimeout;

        public static int CommandTimeout
        {
            get => commandTimeout ?? DefaultCommandTimeout;
            set => commandTimeout = value;
        }

        private static IOrmLiteDialectProvider dialectProvider;
        public static IOrmLiteDialectProvider DialectProvider
        {
            get
            {
                if (dialectProvider == null)
                {
                    throw new ArgumentNullException(nameof(DialectProvider),
                        "You must set the singleton 'OrmLiteConfig.DialectProvider' to use the OrmLiteWriteExtensions");
                }
                return dialectProvider;
            }
            set => dialectProvider = value;
        }

        public static IOrmLiteDialectProvider GetDialectProvider(this IDbCommand dbCmd) =>
            dbCmd is IHasDialectProvider hasDialectProvider 
                ? hasDialectProvider.DialectProvider
                : DialectProvider;

        public static IOrmLiteDialectProvider Dialect(this IDbCommand dbCmd) =>
            dbCmd is IHasDialectProvider hasDialectProvider 
                ? hasDialectProvider.DialectProvider
                : DialectProvider;

        public static IOrmLiteDialectProvider GetDialectProvider(this IDbConnection db) =>
            db is IHasDialectProvider hasDialectProvider
                ? hasDialectProvider.DialectProvider
                : DialectProvider;

        public static INamingStrategy GetNamingStrategy(this IDbConnection db) =>
            db.GetDialectProvider().NamingStrategy;

        public static IOrmLiteDialectProvider Dialect(this IDbConnection db) =>
            db is IHasDialectProvider hasDialectProvider
                ? hasDialectProvider.DialectProvider
                : DialectProvider;

        public static IOrmLiteExecFilter GetExecFilter(this IOrmLiteDialectProvider dialectProvider) {
            return dialectProvider != null
                ? dialectProvider.ExecFilter ?? ExecFilter
                : ExecFilter;
        }

        public static IOrmLiteExecFilter GetExecFilter(this IDbCommand dbCmd) {
            var dialect = dbCmd is IHasDialectProvider hasDialectProvider
                ? hasDialectProvider.DialectProvider
                : DialectProvider;
            return dialect.GetExecFilter();
        }

        public static IOrmLiteExecFilter GetExecFilter(this IDbConnection db) {
            var dialect = db is IHasDialectProvider hasDialectProvider
                ? hasDialectProvider.DialectProvider
                : DialectProvider;
            return dialect.GetExecFilter();
        }

        public static void SetLastCommandText(this IDbConnection db, string sql)
        {
            if (db is OrmLiteConnection ormLiteConn)
            {
                ormLiteConn.LastCommandText = sql;
            }
        }

        private const string RequiresOrmLiteConnection = "{0} can only be set on a OrmLiteConnectionFactory connection, not a plain IDbConnection";

        public static void SetCommandTimeout(this IDbConnection db, int? commandTimeout)
        {
            if (!(db is OrmLiteConnection ormLiteConn))
                throw new NotImplementedException(string.Format(RequiresOrmLiteConnection,"CommandTimeout"));

            ormLiteConn.CommandTimeout = commandTimeout;
        }

        public static IDbConnection ToDbConnection(this string dbConnectionStringOrFilePath)
        {
            return dbConnectionStringOrFilePath.ToDbConnection(DialectProvider);
        }

        public static IDbConnection OpenDbConnection(this string dbConnectionStringOrFilePath)
        {
            var sqlConn = dbConnectionStringOrFilePath.ToDbConnection(DialectProvider);
            sqlConn.Open();
            return sqlConn;
        }

        public static IDbConnection OpenReadOnlyDbConnection(this string dbConnectionStringOrFilePath)
        {
            var options = new Dictionary<string, string> { { "Read Only", "True" } };

            var dbConn = DialectProvider.CreateConnection(dbConnectionStringOrFilePath, options);
            dbConn.Open();
            return dbConn;
        }

        public static void ClearCache()
        {
            OrmLiteConfigExtensions.ClearCache();
        }

        public static ModelDefinition GetModelMetadata(this Type modelType)
        {
            return modelType.GetModelDefinition();
        }

        public static IDbConnection ToDbConnection(this string dbConnectionStringOrFilePath, IOrmLiteDialectProvider dialectProvider)
        {
            var dbConn = dialectProvider.CreateConnection(dbConnectionStringOrFilePath, options: null);
            return dbConn;
        }

        public static void ResetLogFactory(ILogFactory logFactory=null)
        {
            logFactory ??= LogManager.LogFactory;
            LogManager.LogFactory = logFactory;
            OrmLiteResultsFilterExtensions.Log = logFactory.GetLogger(typeof(OrmLiteResultsFilterExtensions));
            OrmLiteWriteCommandExtensions.Log = logFactory.GetLogger(typeof(OrmLiteWriteCommandExtensions));
            OrmLiteReadCommandExtensions.Log = logFactory.GetLogger(typeof(OrmLiteReadCommandExtensions));
            OrmLiteResultsFilterExtensions.Log = logFactory.GetLogger(typeof(OrmLiteResultsFilterExtensions));
            OrmLiteUtils.Log = logFactory.GetLogger(typeof(OrmLiteUtils));
            OrmLiteWriteCommandExtensionsAsync.Log = logFactory.GetLogger(typeof(OrmLiteWriteCommandExtensionsAsync));
            OrmLiteReadCommandExtensionsAsync.Log = logFactory.GetLogger(typeof(OrmLiteReadCommandExtensionsAsync));
            OrmLiteResultsFilterExtensionsAsync.Log = logFactory.GetLogger(typeof(OrmLiteResultsFilterExtensionsAsync));
            OrmLiteConverter.Log = logFactory.GetLogger(typeof(OrmLiteConverter));
        }
        
        public static bool DisableColumnGuessFallback { get; set; }
        public static bool StripUpperInLike { get; set; } 
#if NETCORE
            = true;
#endif

        public static IOrmLiteResultsFilter ResultsFilter
        {
            get
            {
                var state = OrmLiteContext.OrmLiteState;
                return state?.ResultsFilter;
            }
            set => OrmLiteContext.GetOrCreateState().ResultsFilter = value;
        }

        private static IOrmLiteExecFilter execFilter;
        public static IOrmLiteExecFilter ExecFilter
        {
            get 
            {
                if (execFilter == null)
                    execFilter = new OrmLiteExecFilter();

                return dialectProvider != null 
                    ? dialectProvider.ExecFilter ?? execFilter 
                    : execFilter; 
            }
            set => execFilter = value;
        }

        public static Action<IDbCommand> BeforeExecFilter { get; set; }
        public static Action<IDbCommand> AfterExecFilter { get; set; }

        public static Action<IDbCommand, object> InsertFilter { get; set; }
        public static Action<IDbCommand, object> UpdateFilter { get; set; }
        public static Action<IUntypedSqlExpression> SqlExpressionSelectFilter { get; set; }
        public static Func<Type, string, string> LoadReferenceSelectFilter { get; set; }


        public static Func<string, string> StringFilter { get; set; }

        public static Func<FieldDefinition, object> OnDbNullFilter { get; set; }

        public static Action<object> PopulatedObjectFilter { get; set; }

        public static Action<IDbCommand, Exception> ExceptionFilter { get; set; }

        public static bool ThrowOnError { get; set; } = true;

        public static Func<string, string> SanitizeFieldNameForParamNameFn = fieldName =>
            (fieldName ?? "").Replace(" ", "");

        public static bool IsCaseInsensitive { get; set; }

        public static bool DeoptimizeReader { get; set; }

        public static bool SkipForeignKeys { get; set; }

        public static bool IncludeTablePrefixes { get; set; }
        
        public static Action<IUntypedSqlExpression> SqlExpressionInitFilter { get; set; }

        public static Func<string, string> ParamNameFilter { get; set; }
        
        public static Action<ModelDefinition> OnModelDefinitionInit { get; set; }
    }
}