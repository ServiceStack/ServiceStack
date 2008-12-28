using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class Discussion : BusinessBase<uint>
    {
        #region Declarations

		
		private System.DateTime _createdDate = new DateTime();
		
		
		private IList<DiscussionPage> _discussionPages = new List<DiscussionPage>();
		private IList<@ModelName@> _users = new List<@ModelName@>();
		private IList<@ModelName@Set> _userSets = new List<@ModelName@Set>();
		
        #endregion

        #region Constructors

        public Discussion() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_createdDate);

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
		
		public virtual IList<DiscussionPage> DiscussionPages
        {
            get { return _discussionPages; }
            set
			{
				OnDiscussionPagesChanging();
				_discussionPages = value;
				OnDiscussionPagesChanged();
			}
        }
		partial void OnDiscussionPagesChanging();
		partial void OnDiscussionPagesChanged();
		
		public virtual IList<@ModelName@> @ModelName@s
        {
            get { return _users; }
            set
			{
				On@ModelName@sChanging();
				_users = value;
				On@ModelName@sChanged();
			}
        }
		partial void On@ModelName@sChanging();
		partial void On@ModelName@sChanged();
		
		public virtual IList<@ModelName@Set> @ModelName@Sets
        {
            get { return _userSets; }
            set
			{
				On@ModelName@SetsChanging();
				_userSets = value;
				On@ModelName@SetsChanged();
			}
        }
		partial void On@ModelName@SetsChanging();
		partial void On@ModelName@SetsChanged();
		
        #endregion
    }
}
