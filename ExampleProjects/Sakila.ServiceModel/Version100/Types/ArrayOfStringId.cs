/*
// $Id: ArrayOfStringId.cs 500 2008-12-12 14:49:19Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 500 $
// Modified Date : $LastChangedDate: 2008-12-12 14:49:19 +0000 (Fri, 12 Dec 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfStringId : List<string>
	{
		public ArrayOfStringId() { }
		public ArrayOfStringId(IEnumerable<string> collection) : base(collection) { }
	}
}