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

namespace @ServiceNamespace@.Logic.Translators.DataToDomain
{
	public class @ModelName@Translator : ITranslator<@ModelName@, DataAccess.DataModel.@ModelName@>
	{
		public static readonly @ModelName@Translator Instance = new @ModelName@Translator();

		public @ModelName@ Parse(DataAccess.DataModel.@ModelName@ from)
		{
			var to = new @ModelName@ {
				Id = (int)from.Id,
				Balance = from.Balance,
				GlobalId = new Guid(from.GlobalId),
				@ModelName@Details = @ModelName@DetailsTranslator.Instance.Parse(from),
			};

			return to;
		}
	}
}