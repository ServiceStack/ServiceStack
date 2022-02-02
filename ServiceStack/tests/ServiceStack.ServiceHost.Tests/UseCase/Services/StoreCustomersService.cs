using System;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;

namespace ServiceStack.ServiceHost.Tests.UseCase.Services
{
    public class StoreCustomersService : IService
    {
        private readonly IDbConnection db;

        public StoreCustomersService(IDbConnection dbConn)
        {
            this.db = dbConn;
            //Console.WriteLine("StoreCustomersService()");
        }

        public object Any(StoreCustomers request)
        {
            db.DropAndCreateTable<Customer>();
            foreach (var customer in request.Customers)
            {
                db.Insert(customer);
            }

            return null;
        }
    }

}