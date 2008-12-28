using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
	public partial class @ModelName@ : BusinessBase<uint>
	{
		private Guid _globalGuid = Guid.Empty;

		public virtual Guid GlobalIdGuid
		{
			get { return _globalGuid; }
		}

		partial void OnGlobalIdChanged()
		{
			_globalGuid = new Guid(_globalId);
		}

		public virtual bool CanNotifyEmailBool
		{
			get { return this.CanNotifyEmail > 0 ? true : false; }
			set { this.CanNotifyEmail = (byte)(value ? 1 : 0); }
		}

		public virtual bool StoreCreditCardBool
		{
			get { return this.StoreCreditCard > 0 ? true : false; }
			set { this.StoreCreditCard = (byte)(value ? 1 : 0); }
		}

		public virtual bool SingleClickBuyEnabledBool
		{
			get { return this.SingleClickBuyEnabled > 0 ? true : false; }
			set { this.SingleClickBuyEnabled = (byte)(value ? 1 : 0); }
		}

		public virtual CreditCardInfo PrimaryCreditCard
		{
			set
			{
				value.@ModelName@Member = this;
				this.CreditCardInfos = new List<CreditCardInfo> { value };
			}
			get
			{
				return this.CreditCardInfos.Count > 0 ? this.CreditCardInfos[0] : null;
			}
		}
	}
}