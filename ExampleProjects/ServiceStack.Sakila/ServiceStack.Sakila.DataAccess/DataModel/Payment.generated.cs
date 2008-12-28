using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Payment : BusinessBase<ushort>
    {
        #region Declarations

		
		private decimal _amount = default(Decimal);
		private System.DateTime _paymentDate = new DateTime();
		private System.DateTime _lastUpdate = new DateTime();
		
		private Customer _customer = null;
		private Rental _rental = null;
		private Staff _staff = null;
		
		
        #endregion

        #region Constructors

        public Payment() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_amount);
			sb.Append(_paymentDate);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual decimal Amount
        {
            get { return _amount; }
			set
			{
				OnAmountChanging();
				_amount = value;
				OnAmountChanged();
			}
        }
		partial void OnAmountChanging();
		partial void OnAmountChanged();
		
		public virtual System.DateTime PaymentDate
        {
            get { return _paymentDate; }
			set
			{
				OnPaymentDateChanging();
				_paymentDate = value;
				OnPaymentDateChanged();
			}
        }
		partial void OnPaymentDateChanging();
		partial void OnPaymentDateChanged();
		
		public virtual System.DateTime LastUpdate
        {
            get { return _lastUpdate; }
			set
			{
				OnLastUpdateChanging();
				_lastUpdate = value;
				OnLastUpdateChanged();
			}
        }
		partial void OnLastUpdateChanging();
		partial void OnLastUpdateChanged();
		
		public virtual Customer CustomerMember
        {
            get { return _customer; }
			set
			{
				OnCustomerMemberChanging();
				_customer = value;
				OnCustomerMemberChanged();
			}
        }
		partial void OnCustomerMemberChanging();
		partial void OnCustomerMemberChanged();
		
		public virtual Rental RentalMember
        {
            get { return _rental; }
			set
			{
				OnRentalMemberChanging();
				_rental = value;
				OnRentalMemberChanged();
			}
        }
		partial void OnRentalMemberChanging();
		partial void OnRentalMemberChanged();
		
		public virtual Staff StaffMember
        {
            get { return _staff; }
			set
			{
				OnStaffMemberChanging();
				_staff = value;
				OnStaffMemberChanged();
			}
        }
		partial void OnStaffMemberChanging();
		partial void OnStaffMemberChanged();
		
        #endregion
    }
}
