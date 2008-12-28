/*
// $Id: ArrayOfIntId.cs 276 2008-12-02 10:52:36Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 276 $
// Modified Date : $LastChangedDate: 2008-12-02 10:52:36 +0000 (Tue, 02 Dec 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfIntId : List<int>
	{
		public ArrayOfIntId() { }
		public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
	}
}