using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IStaffManager : IManagerBase<Staff, byte>
    {
	}
	
	partial class StaffManager : ManagerBase<Staff, byte>, IStaffManager
    {
	}
}