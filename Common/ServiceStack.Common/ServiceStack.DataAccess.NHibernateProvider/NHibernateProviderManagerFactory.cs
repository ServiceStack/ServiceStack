/*
// $Id: NHibernateProviderManagerFactory.cs 298 2008-12-03 13:41:40Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 298 $
// Modified Date : $LastChangedDate: 2008-12-03 13:41:40 +0000 (Wed, 03 Dec 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateProviderManagerFactory : IPersistenceProviderManagerFactory
	{
		private readonly ILog log;

		public IDictionary<string, string> StaticConfigPropertyTable { get; set; }
		public IList<string> XmlMappingAssemblyNames { get; set; }

		public NHibernateProviderManagerFactory(ILogFactory logFactory)
		{
			this.StaticConfigPropertyTable = new Dictionary<string, string>();
			this.XmlMappingAssemblyNames = new List<string>();
			this.log = logFactory.GetLogger(GetType());
		}

		public IPersistenceProviderManager CreateProviderManager(string connectionString)
		{
			// Create the NHibernate configuration programmatically
			var configuration = this.BuildNHibernateConfiguration(connectionString);

			// Create a provider manager wrapper around a session factory
			return new NHibernateProviderManager(configuration);
		}

		private NHibernate.Cfg.Configuration BuildNHibernateConfiguration(string connectionString)
		{
			IDictionary<string, string> configPropertyTable = new Dictionary<string, string>(this.StaticConfigPropertyTable);
			configPropertyTable[NHibernateProviderManager.ConnectionStringKey] = connectionString;

			NHibernate.Cfg.Configuration config = new NHibernate.Cfg.Configuration().SetProperties(configPropertyTable);

			foreach (var assemblyName in this.XmlMappingAssemblyNames)
			{
				config.AddAssembly(assemblyName);
			}

			return config;
		}
	}
}