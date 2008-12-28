/*
// $Id: IAction.cs 651 2008-12-22 11:32:03Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 651 $
// Modified Date : $LastChangedDate: 2008-12-22 11:32:03 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Ddn.Common.DesignPatterns.Command;
using Ddn.Common.Services.Service;
using @ServiceNamespace@.DataAccess;

namespace @ServiceNamespace@.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		@ServiceName@DataAccessProvider Provider { get; set; }
		AppContext AppContext { get; set; }
	}
}