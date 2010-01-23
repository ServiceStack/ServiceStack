using System;
using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite
{
	public static class OrmLiteConfig
	{
		static OrmLiteConfig()
		{
#if NO_EXPRESSIONS
			PropertyInvoker = ReflectionPropertyInvoker.Instance;
#else
			PropertyInvoker = ExpressionPropertyInvoker.Instance;
#endif
		}

		private static IOrmLiteDialectProvider dialectProvider;
		public static IOrmLiteDialectProvider DialectProvider
		{
			get
			{
				if (dialectProvider == null)
				{
					throw new ArgumentNullException("DialectProvider",
						"You must set the singleton 'OrmLiteWriteExtensions.DialectProvider' to use the OrmLiteWriteExtensions");
				}
				return dialectProvider;
			}
			set
			{
				dialectProvider = value;
				PropertyInvoker.ConvertValueFn = dialectProvider.ConvertDbValue;
			}
		}

		public static IPropertyInvoker PropertyInvoker { get; set; }

		public static IDbConnection ToDbConnection(this string dbConnectionStringOrFilePath)
		{
			return DialectProvider.CreateConnection(dbConnectionStringOrFilePath, null);
		}

		public static IDbConnection OpenDbConnection(this string dbConnectionStringOrFilePath)
		{
			try
			{
				var sqlConn = dbConnectionStringOrFilePath.ToDbConnection();
				sqlConn.Open();
				return sqlConn;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static IDbConnection OpenReadOnlyDbConnection(this string dbConnectionStringOrFilePath)
		{
			try
			{
				var options = new Dictionary<string, string> { { "Read Only", "True" } };

				var sqlConn = DialectProvider.CreateConnection(dbConnectionStringOrFilePath, options);
				sqlConn.Open();
				return sqlConn;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static void ClearCache()
		{
			OrmLiteConfigExtensions.ClearCache();
		}
	}
}