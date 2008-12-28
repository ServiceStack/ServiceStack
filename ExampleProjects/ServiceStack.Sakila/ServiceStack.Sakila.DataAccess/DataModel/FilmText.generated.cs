using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class FilmText : BusinessBase<short>
    {
        #region Declarations

		
		private string _title = String.Empty;
		private string _description = String.Empty;
		
		
		
        #endregion

        #region Constructors

        public FilmText() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_title);
			sb.Append(_description);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string Title
        {
            get { return _title; }
			set
			{
				OnTitleChanging();
				_title = value;
				OnTitleChanged();
			}
        }
		partial void OnTitleChanging();
		partial void OnTitleChanged();
		
		public virtual string Description
        {
            get { return _description; }
			set
			{
				OnDescriptionChanging();
				_description = value;
				OnDescriptionChanged();
			}
        }
		partial void OnDescriptionChanging();
		partial void OnDescriptionChanged();
		
        #endregion
    }
}
