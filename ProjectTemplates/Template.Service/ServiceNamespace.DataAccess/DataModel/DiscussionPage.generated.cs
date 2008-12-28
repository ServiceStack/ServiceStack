using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class DiscussionPage : BusinessBase<uint>
    {
        #region Declarations

		
		private System.DateTime _createdDate = new DateTime();
		private System.DateTime _lastModifiedDate = new DateTime();
		private int _postCount = default(Int32);
		private string _postContent = String.Empty;
		private byte[] _postStatistic = null;
		
		private Discussion _discussion = null;
		
		
        #endregion

        #region Constructors

        public DiscussionPage() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_createdDate);
			sb.Append(_lastModifiedDate);
			sb.Append(_postCount);
			sb.Append(_postContent);
			sb.Append(_postStatistic);

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
		
		public virtual int PostCount
        {
            get { return _postCount; }
			set
			{
				OnPostCountChanging();
				_postCount = value;
				OnPostCountChanged();
			}
        }
		partial void OnPostCountChanging();
		partial void OnPostCountChanged();
		
		public virtual string PostContent
        {
            get { return _postContent; }
			set
			{
				OnPostContentChanging();
				_postContent = value;
				OnPostContentChanged();
			}
        }
		partial void OnPostContentChanging();
		partial void OnPostContentChanged();
		
		public virtual byte[] PostStatistic
        {
            get { return _postStatistic; }
			set
			{
				OnPostStatisticChanging();
				_postStatistic = value;
				OnPostStatisticChanged();
			}
        }
		partial void OnPostStatisticChanging();
		partial void OnPostStatisticChanged();
		
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
		
        #endregion
    }
}
