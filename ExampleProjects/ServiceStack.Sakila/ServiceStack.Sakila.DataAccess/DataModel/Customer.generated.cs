using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Customer : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _firstName = String.Empty;
		private string _lastName = String.Empty;
		private string _email = String.Empty;
		private sbyte _active = default(SByte);
		private System.DateTime _createDate = new DateTime();
		private System.DateTime _lastUpdate = new DateTime();
		
		private Address _address = null;
		private Store _store = null;
		
		private IList<Payment> _payments = new List<Payment>();
		private IList<Rental> _rentals = new List<Rental>();
		
        #endregion

        #region Constructors

        public Customer() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_firstName);
			sb.Append(_lastName);
			sb.Append(_email);
			sb.Append(_active);
			sb.Append(_createDate);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string FirstName
        {
            get { return _firstName; }
			set
			{
				OnFirstNameChanging();
				_firstName = value;
				OnFirstNameChanged();
			}
        }
		partial void OnFirstNameChanging();
		partial void OnFirstNameChanged();
		
		public virtual string LastName
        {
            get { return _lastName; }
			set
			{
				OnLastNameChanging();
				_lastName = value;
				OnLastNameChanged();
			}
        }
		partial void OnLastNameChanging();
		partial void OnLastNameChanged();
		
		public virtual string Email
        {
            get { return _email; }
			set
			{
				OnEmailChanging();
				_email = value;
				OnEmailChanged();
			}
        }
		partial void OnEmailChanging();
		partial void OnEmailChanged();
		
		public virtual sbyte Active
        {
            get { return _active; }
			set
			{
				OnActiveChanging();
				_active = value;
				OnActiveChanged();
			}
        }
		partial void OnActiveChanging();
		partial void OnActiveChanged();
		
		public virtual System.DateTime CreateDate
        {
            get { return _createDate; }
			set
			{
				OnCreateDateChanging();
				_createDate = value;
				OnCreateDateChanged();
			}
        }
		partial void OnCreateDateChanging();
		partial void OnCreateDateChanged();
		
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
		
		public virtual Address addressMember
        {
            get { return _address; }
			set
			{
				OnaddressMemberChanging();
				_address = value;
				OnaddressMemberChanged();
			}
        }
		partial void OnaddressMemberChanging();
		partial void OnaddressMemberChanged();
		
		public virtual Store StoreMember
        {
            get { return _store; }
			set
			{
				OnStoreMemberChanging();
				_store = value;
				OnStoreMemberChanged();
			}
        }
		partial void OnStoreMemberChanging();
		partial void OnStoreMemberChanged();
		
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
		
		public virtual IList<Rental> Rentals
        {
            get { return _rentals; }
            set
			{
				OnRentalsChanging();
				_rentals = value;
				OnRentalsChanged();
			}
        }
		partial void OnRentalsChanging();
		partial void OnRentalsChanged();
		
        #endregion
    }
}
