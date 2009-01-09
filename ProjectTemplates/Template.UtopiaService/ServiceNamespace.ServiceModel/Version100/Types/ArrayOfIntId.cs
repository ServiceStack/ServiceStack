/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace @ServiceModelNamespace@.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Id")]
	public class ArrayOfIntId : List<int>
	{
		public ArrayOfIntId() { }
		public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
	}
}