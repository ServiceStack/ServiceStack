using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
	public partial class CreditCardInfo : BusinessBase<uint>
    {
		public virtual bool IsActiveBool
		{
			get { return this.IsActive > 0 ? true : false; }
			set { this.IsActive = (byte)(value ? 1 : 0); }
		}
	}
}