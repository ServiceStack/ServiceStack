using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@Set : BusinessBase<uint>
    {
        #region Declarations

		
		private System.DateTime _createdDate = new DateTime();
		private string _createdBy = String.Empty;
		private System.DateTime _lastModifiedDate = new DateTime();
		private string _lastModifiedBy = String.Empty;
		private string _name = String.Empty;
		private string _type = String.Empty;
		
		private Discussion _discussion = null;
		private @ModelName@ _user = null;
		
		private IList<@ModelName@SetProduct> _userSetProducts = new List<@ModelName@SetProduct>();
		
        #endregion

        #region Constructors

        public @ModelName@Set() { }

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
			sb.Append(_name);
			sb.Append(_type);

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
		
		public virtual string Type
        {
            get { return _type; }
			set
			{
				OnTypeChanging();
				_type = value;
				OnTypeChanged();
			}
        }
		partial void OnTypeChanging();
		partial void OnTypeChanged();
		
		public virtual Discussion DiscussionMember
        {
            get { return _discussion; }
			set
			{
				OnDiscussionMemberChanging();
				_discussion = value;
				OnDiscussionMemberChanged();
			}
        }
		partial void OnDiscussionMemberChanging();
		partial void OnDiscussionMemberChanged();
		
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
		
		public virtual IList<@ModelName@SetProduct> @ModelName@SetProducts
        {
            get { return _userSetProducts; }
            set
			{
				On@ModelName@SetProductsChanging();
				_userSetProducts = value;
				On@ModelName@SetProductsChanged();
			}
        }
		partial void On@ModelName@SetProductsChanging();
		partial void On@ModelName@SetProductsChanged();
		
        #endregion
    }
}
