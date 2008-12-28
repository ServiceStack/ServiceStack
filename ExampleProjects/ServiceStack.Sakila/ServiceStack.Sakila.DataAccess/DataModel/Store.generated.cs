using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Store : BusinessBase<byte>
    {
        #region Declarations

		
		private System.DateTime _lastUpdate = new DateTime();
		
		private Address _address = null;
		private Staff _staff = null;
		
		private IList<Customer> _customers = new List<Customer>();
		private IList<Inventory> _inventories = new List<Inventory>();
		private IList<Staff> _staffs = new List<Staff>();
		
        #endregion

        #region Constructors

        public Store() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

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
		
		public virtual Address addressMember
        {
            get { return _address; }
			set
			{
				OnaddressMemberChanging();
				_address = value;
				OnaddressMemberChanged();
			}
        }
		partial void OnaddressMemberChanging();
		partial void OnaddressMemberChanged();
		
		public virtual Staff StaffMember
        {
            get { return _staff; }
			set
			{
				OnStaffMemberChanging();
				_staff = value;
				OnStaffMemberChanged();
			}
        }
		partial void OnStaffMemberChanging();
		partial void OnStaffMemberChanged();
		
		public virtual IList<Customer> Customers
        {
            get { return _customers; }
            set
			{
				OnCustomersChanging();
				_customers = value;
				OnCustomersChanged();
			}
        }
		partial void OnCustomersChanging();
		partial void OnCustomersChanged();
		
		public virtual IList<Inventory> Inventories
        {
            get { return _inventories; }
            set
			{
				OnInventoriesChanging();
				_inventories = value;
				OnInventoriesChanged();
			}
        }
		partial void OnInventoriesChanging();
		partial void OnInventoriesChanged();
		
		public virtual IList<Staff> Staffs
        {
            get { return _staffs; }
            set
			{
				OnStaffsChanging();
				_staffs = value;
				OnStaffsChanged();
			}
        }
		partial void OnStaffsChanging();
		partial void OnStaffsChanged();
		
        #endregion
    }
}
