/*
// $Id: IPersistenceProviderManagerFactory.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

namespace ServiceStack.DataAccess
{
	public interface IPersistenceProviderManagerFactory
	{
		IPersistenceProviderManager CreateProviderManager(string connectionString);
	}
}