/*
// $Id: @ModelName@Translator.cs 433 2008-12-10 10:39:46Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 433 $
// Modified Date : $LastChangedDate: 2008-12-10 10:39:46 +0000 (Wed, 10 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;

namespace @ServiceNamespace@.Logic.Translators.DomainToData
{
	public class @ModelName@Translator : ITranslator<DataAccess.DataModel.@ModelName@, @ModelName@Details>
	{
		public static readonly @ModelName@Translator Instance = new @ModelName@Translator();

		public DataAccess.DataModel.@ModelName@ Parse(@ModelName@Details from)
		{
			var to = new DataAccess.DataModel.@ModelName@ {
				GlobalId = from.GlobalId.ToByteArray(),
				@ModelName@Name = from.@ModelName@Name,
				//SaltPassword = from.SaltPassword,
			};

			return to;
		}
	}
}