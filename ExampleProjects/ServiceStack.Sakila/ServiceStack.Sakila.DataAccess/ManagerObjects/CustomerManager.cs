using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface ICustomerManager : IManagerBase<Customer, ushort>
    {
	}
	
	partial class CustomerManager : ManagerBase<Customer, ushort>, ICustomerManager
    {
	}
}