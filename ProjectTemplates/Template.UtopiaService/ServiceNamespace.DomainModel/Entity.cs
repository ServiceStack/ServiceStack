/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$ 
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

namespace @DomainModelNamespace@
{
	public abstract class Entity : ModelBase
	{
		//Make it a long so we can store the unique db4o internal id for fast access
		public long Id { get; set; }
	}
}