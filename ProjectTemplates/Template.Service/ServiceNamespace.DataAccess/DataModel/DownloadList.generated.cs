using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class DownloadList : BusinessBase<uint>
    {
        #region Declarations

		
		private uint _productId = default(UInt32);
		private uint _assetId = default(UInt32);
		private uint _sortOrder = default(UInt32);
		private System.DateTime _createdDate = new DateTime();
		
		private @ModelName@ _user = null;
		
		
        #endregion

        #region Constructors

        public DownloadList() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_productId);
			sb.Append(_assetId);
			sb.Append(_sortOrder);
			sb.Append(_createdDate);

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
		
		public virtual uint AssetId
        {
            get { return _assetId; }
			set
			{
				OnAssetIdChanging();
				_assetId = value;
				OnAssetIdChanged();
			}
        }
		partial void OnAssetIdChanging();
		partial void OnAssetIdChanged();
		
		public virtual uint SortOrder
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
		
		public virtual System.DateTime CreatedDate
        {
            get { return _createdDate; }
			set
			{
				OnCreatedDateChanging();
				_createdDate = value;
				OnCreatedDateChanged();
			}
        }
		partial void OnCreatedDateChanging();
		partial void OnCreatedDateChanged();
		
		public virtual @ModelName@ @ModelName@Member
        {
            get { return _user; }
			set
			{
				On@ModelName@MemberChanging();
				_user = value;
				On@ModelName@MemberChanged();
			}
        }
		partial void On@ModelName@MemberChanging();
		partial void On@ModelName@MemberChanged();
		
        #endregion
    }
}
