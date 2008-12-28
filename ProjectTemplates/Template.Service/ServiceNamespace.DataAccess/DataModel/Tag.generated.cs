using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class Tag : BusinessBase<uint>
    {
        #region Declarations

		
		private string _name = String.Empty;
		
		private @ModelName@Product _userProduct = null;
		
		
        #endregion

        #region Constructors

        public Tag() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_name);

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
		
		public virtual @ModelName@Product @ModelName@ProductMember
        {
            get { return _userProduct; }
			set
			{
				On@ModelName@ProductMemberChanging();
				_userProduct = value;
				On@ModelName@ProductMemberChanged();
			}
        }
		partial void On@ModelName@ProductMemberChanging();
		partial void On@ModelName@ProductMemberChanged();
		
        #endregion
    }
}
