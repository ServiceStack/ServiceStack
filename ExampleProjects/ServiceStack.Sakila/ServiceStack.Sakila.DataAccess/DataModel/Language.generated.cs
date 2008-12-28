using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Language : BusinessBase<byte>
    {
        #region Declarations

		
		private string _name = String.Empty;
		private System.DateTime _lastUpdate = new DateTime();
		
		
		private IList<Film> _films = new List<Film>();
		
        #endregion

        #region Constructors

        public Language() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_name);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string Name
        {
            get { return _name; }
			set
			{
				OnNameChanging();
				_name = value;
				OnNameChanged();
			}
        }
		partial void OnNameChanging();
		partial void OnNameChanged();
		
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
		
		public virtual IList<Film> Films
        {
            get { return _films; }
            set
			{
				OnFilmsChanging();
				_films = value;
				OnFilmsChanged();
			}
        }
		partial void OnFilmsChanging();
		partial void OnFilmsChanged();
		
        #endregion
    }
}
