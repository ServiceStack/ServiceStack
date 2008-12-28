/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate $ 
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Configuration;
using Ddn.CacheAccess.Memcached;
using Ddn.Common.Services.CacheAccess;
using Ddn.Common.Testing;
using Ddn.Logging;
using Ddn.Logging.Log4Net;
using Enyim.Caching;

namespace @ServiceNamespace@.Tests.Support
{
	/// <summary>
	/// 
	/// </summary>
	public class TestParameters : ITestParameters
	{
		private readonly ILogFactory logFactory = new Log4NetFactory(true);

		public TestParameters()
			: this(DatabaseUsage.DisposableUnitTest)
		{
		}

		public TestParameters(DatabaseUsage databaseUsage)
		{
			this.DatabaseUsage = databaseUsage;
		}

		public DatabaseUsage DatabaseUsage { get; private set; }

		public string LocalConnectionString
		{
			get { return ConfigurationManager.AppSettings["LocalConnectionString"]; }
		}

		public string UnitTestConnectionString
		{
			get { return ConfigurationManager.AppSettings["TestConnectionString"]; }
		}

		public string DatabaseName
		{
			get { return ConfigurationManager.AppSettings["DatabaseName"]; }
		}

		public string CreateSchemaScript
		{
			get { return ConfigurationManager.AppSettings["CreateSchemaScript"]; }
		}

		public string MappingAssemblyName
		{
			get { return "@ServiceNamespace@.DataAccess"; }
		}

		public string ServerPrivateKeyXml
		{
			get { return ConfigurationManager.AppSettings["ServerPrivateKey"]; }
		}

		public string StringResourcesFilePath
		{
			get { return ConfigurationManager.AppSettings["StringResourcesFile"]; }
		}

		public ILogFactory LogFactory
		{
			get { return logFactory; }
		}

		/// <summary>
		/// Gets the cache. Configures iteslf from App.config
		/// </summary>
		/// <value>The cache.</value>
		public ICacheClient Cache
		{
			get { return new DdnMemcachedClient(new MemcachedClient()); }
		}
	}
}