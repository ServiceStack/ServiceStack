using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Actor : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _firstName = String.Empty;
		private string _lastName = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		
		
        #endregion

        #region Constructors

        public Actor() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_firstName);
			sb.Append(_lastName);
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
		
        #endregion
    }
}
