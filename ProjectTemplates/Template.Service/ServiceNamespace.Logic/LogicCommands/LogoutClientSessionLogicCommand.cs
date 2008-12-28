/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class LogoutClientSessionLogicCommand : LogicCommandBase<bool>
	{
		public int @ModelName@Id { get; set; }
		public ICollection<Guid> ClientSessionIds { get; set; }

		public override bool Execute()
		{
			this.AppContext.SessionManager.RemoveClientSession(this.@ModelName@Id, this.ClientSessionIds);
			return true;
		}

	}
}