using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.SakilaNHibernate.DataAccess.Base;

namespace ServiceStack.SakilaNHibernate.DataAccess.DataModel
{
	public partial class Customer : BusinessBase<ushort>
	{
		#region Declarations

		
		private string _firstName = String.Empty;
		private string _lastName = String.Empty;
		private string _email = String.Empty;
		private sbyte _active = default(SByte);
		private System.DateTime _createDate = new DateTime();
		private System.DateTime _lastUpdate = new DateTime();
		
		#endregion

		#region Constructors

		public Customer() { }

		#endregion

		#region Methods

		public override int GetHashCode()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
			sb.Append(this.GetType().FullName);
			sb.Append(_firstName);
			sb.Append(_lastName);
			sb.Append(_email);
			sb.Append(_active);
			sb.Append(_createDate);
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
		
		public virtual string Email
		{
			get { return _email; }
			set
			{
				OnEmailChanging();
				_email = value;
				OnEmailChanged();
			}
		}
		partial void OnEmailChanging();
		partial void OnEmailChanged();
		
		public virtual sbyte Active
		{
			get { return _active; }
			set
			{
				OnActiveChanging();
				_active = value;
				OnActiveChanged();
			}
		}
		partial void OnActiveChanging();
		partial void OnActiveChanged();
		
		public virtual System.DateTime CreateDate
		{
			get { return _createDate; }
			set
			{
				OnCreateDateChanging();
				_createDate = value;
				OnCreateDateChanged();
			}
		}
		partial void OnCreateDateChanging();
		partial void OnCreateDateChanged();
		
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