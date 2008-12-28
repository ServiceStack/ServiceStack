/*
// $Id: CustomerTranslator.cs 433 2008-12-10 10:39:46Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 433 $
// Modified Date : $LastChangedDate: 2008-12-10 10:39:46 +0000 (Wed, 10 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DomainToData
{
	public class CustomerTranslator : ITranslator<DataAccess.DataModel.Customer, Customer>
	{
		public static readonly CustomerTranslator Instance = new CustomerTranslator();

		public DataAccess.DataModel.Customer Parse(Customer from)
		{
			var to = new DataAccess.DataModel.Customer {
				Id = (ushort)from.Id,
			};

			return to;
		}
	}
}