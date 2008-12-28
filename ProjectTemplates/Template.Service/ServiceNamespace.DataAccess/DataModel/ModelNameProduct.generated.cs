using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@Product : BusinessBase<uint>
    {
        #region Declarations

		
		private System.DateTime _createdDate = new DateTime();
		private string _createdBy = String.Empty;
		private System.DateTime _lastModifiedDate = new DateTime();
		private string _lastModifiedBy = String.Empty;
		private uint _productId = default(UInt32);
		private uint _assetId = default(UInt32);
		private uint _parentId = default(UInt32);
		private System.DateTime _purchaseDate = new DateTime();
		private System.DateTime _downloadStartDate = new DateTime();
		private System.DateTime _downloadCompleteDate = new DateTime();
		
		private @ModelName@ _user = null;
		private @ModelName@Order _userOrder = null;
		
		private IList<Genre> _genres = new List<Genre>();
		
        #endregion

        #region Constructors

        public @ModelName@Product() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_createdDate);
			sb.Append(_createdBy);
			sb.Append(_lastModifiedDate);
			sb.Append(_lastModifiedBy);
			sb.Append(_productId);
			sb.Append(_assetId);
			sb.Append(_parentId);
			sb.Append(_purchaseDate);
			sb.Append(_downloadStartDate);
			sb.Append(_downloadCompleteDate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

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
		
		public virtual string CreatedBy
        {
            get { return _createdBy; }
			set
			{
				OnCreatedByChanging();
				_createdBy = value;
				OnCreatedByChanged();
			}
        }
		partial void OnCreatedByChanging();
		partial void OnCreatedByChanged();
		
		public virtual System.DateTime LastModifiedDate
        {
            get { return _lastModifiedDate; }
			set
			{
				OnLastModifiedDateChanging();
				_lastModifiedDate = value;
				OnLastModifiedDateChanged();
			}
        }
		partial void OnLastModifiedDateChanging();
		partial void OnLastModifiedDateChanged();
		
		public virtual string LastModifiedBy
        {
            get { return _lastModifiedBy; }
			set
			{
				OnLastModifiedByChanging();
				_lastModifiedBy = value;
				OnLastModifiedByChanged();
			}
        }
		partial void OnLastModifiedByChanging();
		partial void OnLastModifiedByChanged();
		
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
		
		public virtual uint ParentId
        {
            get { return _parentId; }
			set
			{
				OnParentIdChanging();
				_parentId = value;
				OnParentIdChanged();
			}
        }
		partial void OnParentIdChanging();
		partial void OnParentIdChanged();
		
		public virtual System.DateTime PurchaseDate
        {
            get { return _purchaseDate; }
			set
			{
				OnPurchaseDateChanging();
				_purchaseDate = value;
				OnPurchaseDateChanged();
			}
        }
		partial void OnPurchaseDateChanging();
		partial void OnPurchaseDateChanged();
		
		public virtual System.DateTime DownloadStartDate
        {
            get { return _downloadStartDate; }
			set
			{
				OnDownloadStartDateChanging();
				_downloadStartDate = value;
				OnDownloadStartDateChanged();
			}
        }
		partial void OnDownloadStartDateChanging();
		partial void OnDownloadStartDateChanged();
		
		public virtual System.DateTime DownloadCompleteDate
        {
            get { return _downloadCompleteDate; }
			set
			{
				OnDownloadCompleteDateChanging();
				_downloadCompleteDate = value;
				OnDownloadCompleteDateChanged();
			}
        }
		partial void OnDownloadCompleteDateChanging();
		partial void OnDownloadCompleteDateChanged();
		
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
		
		public virtual @ModelName@Order @ModelName@OrderMember
        {
            get { return _userOrder; }
			set
			{
				On@ModelName@OrderMemberChanging();
				_userOrder = value;
				On@ModelName@OrderMemberChanged();
			}
        }
		partial void On@ModelName@OrderMemberChanging();
		partial void On@ModelName@OrderMemberChanged();
		
		public virtual IList<Genre> Genres
        {
            get { return _genres; }
            set
			{
				OnGenresChanging();
				_genres = value;
				OnGenresChanged();
			}
        }
		partial void OnGenresChanging();
		partial void OnGenresChanged();
		
        #endregion
    }
}
