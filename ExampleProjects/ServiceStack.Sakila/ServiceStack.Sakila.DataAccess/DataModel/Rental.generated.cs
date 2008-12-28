using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Rental : BusinessBase<int>
    {
        #region Declarations

		
		private System.DateTime _rentalDate = new DateTime();
		private System.DateTime _returnDate = new DateTime();
		private System.DateTime _lastUpdate = new DateTime();
		
		private Customer _customer = null;
		private Inventory _inventory = null;
		private Staff _staff = null;
		
		private IList<Payment> _payments = new List<Payment>();
		
        #endregion

        #region Constructors

        public Rental() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_rentalDate);
			sb.Append(_returnDate);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual System.DateTime RentalDate
        {
            get { return _rentalDate; }
			set
			{
				OnRentalDateChanging();
				_rentalDate = value;
				OnRentalDateChanged();
			}
        }
		partial void OnRentalDateChanging();
		partial void OnRentalDateChanged();
		
		public virtual System.DateTime ReturnDate
        {
            get { return _returnDate; }
			set
			{
				OnReturnDateChanging();
				_returnDate = value;
				OnReturnDateChanged();
			}
        }
		partial void OnReturnDateChanging();
		partial void OnReturnDateChanged();
		
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
		
		public virtual Inventory InventoryMember
        {
            get { return _inventory; }
			set
			{
				OnInventoryMemberChanging();
				_inventory = value;
				OnInventoryMemberChanged();
			}
        }
		partial void OnInventoryMemberChanging();
		partial void OnInventoryMemberChanged();
		
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
		
		public virtual IList<Payment> Payments
        {
            get { return _payments; }
            set
			{
				OnPaymentsChanging();
				_payments = value;
				OnPaymentsChanged();
			}
        }
		partial void OnPaymentsChanging();
		partial void OnPaymentsChanged();
		
        #endregion
    }
}
