/*
// $Id: Get@ModelName@sAction.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using @DomainModelNamespace@.@ServiceName@;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class Store@ModelName@DetailsLogicCommand : LogicCommandBase<bool>
	{
		public @ModelName@Details @ModelName@Details { get; set; }

		public override bool Execute()
		{
			//var db@ModelName@ = Provider.Get@ModelName@(@ModelName@Details.GlobalId);
			//db@ModelName@.@ModelName@Name = @ModelName@Details.@ModelName@Name;
			//db@ModelName@.Email = @ModelName@Details.Email;
			////db@ModelName@.FirstName = @ModelName@Details.FirstName;
			////db@ModelName@.LastName = @ModelName@Details.LastName;
			////db@ModelName@.Country = @ModelName@Details.Country;
			////db@ModelName@.CanNotifyEmail = @ModelName@Details.CanNotifyEmail;
			////db@ModelName@.SingleClickBuyEnabled = @ModelName@Details.SingleClickBuyEnabled;

			//Provider.Store@ModelName@(db@ModelName@);
			return true;
		}
	}
}