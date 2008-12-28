using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Country : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _country = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		
		private IList<City> _cities = new List<City>();
		
        #endregion

        #region Constructors

        public Country() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_country);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string CountryName
        {
            get { return _country; }
			set
			{
				OnCountryChanging();
				_country = value;
				OnCountryChanged();
			}
        }
		partial void OnCountryChanging();
		partial void OnCountryChanged();
		
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
		
		public virtual IList<City> Cities
        {
            get { return _cities; }
            set
			{
				OnCitiesChanging();
				_cities = value;
				OnCitiesChanged();
			}
        }
		partial void OnCitiesChanging();
		partial void OnCitiesChanged();
		
        #endregion
    }
}
