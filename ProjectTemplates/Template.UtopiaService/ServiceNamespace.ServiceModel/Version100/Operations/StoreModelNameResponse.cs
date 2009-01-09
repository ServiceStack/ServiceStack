/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class Store@ModelName@Response : IExtensibleDataObject
	{
		public Store@ModelName@Response()
		{
			this.Version = 100;
		}
		
		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }

		[DataMember]
		public int Version { get; set; }

		[DataMember]
		public Properties Properties { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}
}