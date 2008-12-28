/*
// $Id: AppConfig.cs 362 2008-12-05 21:29:40Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 362 $
// Modified Date : $LastChangedDate: 2008-12-05 21:29:40 +0000 (Fri, 05 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Configuration;

namespace @ServiceNamespace@.Host.WebService.AppSupport
{
	public class AppConfig
	{
		public string ConnectionString
		{
			get { return ConfigurationManager.AppSettings["ConnectionString"]; }
		}

		public string ServerPrivateKey
		{
			get { return ConfigurationManager.AppSettings["ServerPrivateKey"]; }
		}

		public string UsageExamplesBaseUri
		{
			get { return ConfigurationManager.AppSettings["UsageExamplesBaseUri"]; }
		}

		public static string StringResourcesFile
		{
			get { return ConfigurationManager.AppSettings["StringResourcesFile"]; }
		}
	}
}