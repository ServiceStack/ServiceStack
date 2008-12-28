/*
// $Id: IPersistenceProviderManager.cs 258 2008-11-28 17:02:44Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 258 $
// Modified Date : $LastChangedDate: 2008-11-28 17:02:44 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

namespace ServiceStack.DataAccess
{
	/// <summary>
	/// Manages a connection to a persistance provider
	/// </summary>
	public interface IPersistenceProviderManager
	{
		string ConnectionString { get; }
		IPersistenceProvider CreateProvider();
	}
}