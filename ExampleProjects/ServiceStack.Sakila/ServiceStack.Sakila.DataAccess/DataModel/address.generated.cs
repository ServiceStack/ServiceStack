using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Address : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _address = String.Empty;
		private string _address2 = String.Empty;
		private string _district = String.Empty;
		private string _postalCode = String.Empty;
		private string _phone = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		private City _city = null;
		
		private IList<Customer> _customers = new List<Customer>();
		private IList<Staff> _staffs = new List<Staff>();
		private IList<Store> _stores = new List<Store>();
		
        #endregion

        #region Constructors

        public Address() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_address);
			sb.Append(_address2);
			sb.Append(_district);
			sb.Append(_postalCode);
			sb.Append(_phone);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string address
        {
            get { return _address; }
			set
			{
				OnaddressChanging();
				_address = value;
				OnaddressChanged();
			}
        }
		partial void OnaddressChanging();
		partial void OnaddressChanged();
		
		public virtual string Address2
        {
            get { return _address2; }
			set
			{
				OnAddress2Changing();
				_address2 = value;
				OnAddress2Changed();
			}
        }
		partial void OnAddress2Changing();
		partial void OnAddress2Changed();
		
		public virtual string District
        {
            get { return _district; }
			set
			{
				OnDistrictChanging();
				_district = value;
				OnDistrictChanged();
			}
        }
		partial void OnDistrictChanging();
		partial void OnDistrictChanged();
		
		public virtual string PostalCode
        {
            get { return _postalCode; }
			set
			{
				OnPostalCodeChanging();
				_postalCode = value;
				OnPostalCodeChanged();
			}
        }
		partial void OnPostalCodeChanging();
		partial void OnPostalCodeChanged();
		
		public virtual string Phone
        {
            get { return _phone; }
			set
			{
				OnPhoneChanging();
				_phone = value;
				OnPhoneChanged();
			}
        }
		partial void OnPhoneChanging();
		partial void OnPhoneChanged();
		
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
		
		public virtual City CityMember
        {
            get { return _city; }
			set
			{
				OnCityMemberChanging();
				_city = value;
				OnCityMemberChanged();
			}
        }
		partial void OnCityMemberChanging();
		partial void OnCityMemberChanged();
		
		public virtual IList<Customer> Customers
        {
            get { return _customers; }
            set
			{
				OnCustomersChanging();
				_customers = value;
				OnCustomersChanged();
			}
        }
		partial void OnCustomersChanging();
		partial void OnCustomersChanged();
		
		public virtual IList<Staff> Staffs
        {
            get { return _staffs; }
            set
			{
				OnStaffsChanging();
				_staffs = value;
				OnStaffsChanged();
			}
        }
		partial void OnStaffsChanging();
		partial void OnStaffsChanged();
		
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
