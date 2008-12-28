using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@SetProduct : BusinessBase<uint>
    {
        #region Declarations

		
		private uint _productId = default(UInt32);
		private int _sortOrder = default(Int32);
		
		private @ModelName@Set _userSet = null;
		
		
        #endregion

        #region Constructors

        public @ModelName@SetProduct() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_productId);
			sb.Append(_sortOrder);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual uint ProductId
        {
            get { return _productId; }
			set
			{
				OnProductIdChanging();
				_productId = value;
				OnProductIdChanged();
			}
        }
		partial void OnProductIdChanging();
		partial void OnProductIdChanged();
		
		public virtual int SortOrder
        {
            get { return _sortOrder; }
			set
			{
				OnSortOrderChanging();
				_sortOrder = value;
				OnSortOrderChanged();
			}
        }
		partial void OnSortOrderChanging();
		partial void OnSortOrderChanged();
		
		public virtual @ModelName@Set @ModelName@SetMember
        {
            get { return _userSet; }
			set
			{
				On@ModelName@SetMemberChanging();
				_userSet = value;
				On@ModelName@SetMemberChanged();
			}
        }
		partial void On@ModelName@SetMemberChanging();
		partial void On@ModelName@SetMemberChanged();
		
        #endregion
    }
}
