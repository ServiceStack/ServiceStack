using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ServiceStack.Sakila.DataAccess.ManagerObjects;
using ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.DataAccess.Base
{
    public class UNuitTestBase
    {
        protected IManagerFactory managerFactory = new ManagerFactory();

        [SetUp]
        public void SetUp()
        {
            NHibernateSessionManager.Instance.Session.BeginTransaction();
        }
        [TearDown]
        public void TearDown()
        {
            NHibernateSessionManager.Instance.Session.RollbackTransaction();
        }
    }
}