using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IRentalManager : IManagerBase<Rental, int>
    {
	}
	
	partial class RentalManager : ManagerBase<Rental, int>, IRentalManager
    {
	}
}