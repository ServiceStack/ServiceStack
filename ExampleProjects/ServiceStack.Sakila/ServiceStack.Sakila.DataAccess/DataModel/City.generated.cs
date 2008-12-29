using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class City : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _city = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		private Country _country = null;
		
		private IList<Address> _address = new List<Address>();
		
        #endregion

        #region Constructors

        public City() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_city);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string Name
        {
            get { return _city; }
			set
			{
				OnCityChanging();
				_city = value;
				OnCityChanged();
			}
        }
		partial void OnCityChanging();
		partial void OnCityChanged();
		
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
		
		public virtual Country CountryMember
        {
            get { return _country; }
			set
			{
				OnCountryMemberChanging();
				_country = value;
				OnCountryMemberChanged();
			}
        }
		partial void OnCountryMemberChanging();
		partial void OnCountryMemberChanged();
		
		public virtual IList<Address> addressList
        {
            get { return _address; }
            set
			{
				OnaddressListChanging();
				_address = value;
				OnaddressListChanged();
			}
        }
		partial void OnaddressListChanging();
		partial void OnaddressListChanged();
		
        #endregion
    }
}
