using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Staff : BusinessBase<byte>
    {
        #region Declarations

		
		private string _firstName = String.Empty;
		private string _lastName = String.Empty;
		private byte[] _picture = null;
		private string _email = String.Empty;
		private sbyte _active = default(SByte);
		private string _username = String.Empty;
		private string _password = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		private Address _address = null;
		private Store _store = null;
		
		private IList<Payment> _payments = new List<Payment>();
		private IList<Rental> _rentals = new List<Rental>();
		private IList<Store> _stores = new List<Store>();
		
        #endregion

        #region Constructors

        public Staff() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_firstName);
			sb.Append(_lastName);
			sb.Append(_picture);
			sb.Append(_email);
			sb.Append(_active);
			sb.Append(_username);
			sb.Append(_password);
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
		
		public virtual byte[] Picture
        {
            get { return _picture; }
			set
			{
				OnPictureChanging();
				_picture = value;
				OnPictureChanged();
			}
        }
		partial void OnPictureChanging();
		partial void OnPictureChanged();
		
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
		
		public virtual string Username
        {
            get { return _username; }
			set
			{
				OnUsernameChanging();
				_username = value;
				OnUsernameChanged();
			}
        }
		partial void OnUsernameChanging();
		partial void OnUsernameChanged();
		
		public virtual string Password
        {
            get { return _password; }
			set
			{
				OnPasswordChanging();
				_password = value;
				OnPasswordChanged();
			}
        }
		partial void OnPasswordChanging();
		partial void OnPasswordChanged();
		
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
		
		public virtual IList<Store> Stores
        {
            get { return _stores; }
            set
			{
				OnStoresChanging();
				_stores = value;
				OnStoresChanged();
			}
        }
		partial void OnStoresChanging();
		partial void OnStoresChanged();
		
        #endregion
    }
}
