using System;
using Ddn.DataAccess;
using Ddn.DataAccess.NHibernateProvider;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;

namespace @ServiceNamespace@.DataAccess.Build.TestData
{
    [TestFixture]
    public class UpdateTestData : IDisposable
    {
        private const uint USER_ID = 1;
        private ISessionFactory sessionFactory;
        private ISession session;


        [TestFixtureSetUp]
        public void Load()
        {
            sessionFactory = new Configuration().Configure().BuildSessionFactory();
            session = sessionFactory.OpenSession();
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            if (session == null) return;

            session.Dispose();
            sessionFactory.Dispose();
        }

        [Test]
        public void Update@ModelName@()
        {
            IPersistenceProvider provider = new NHibernatePersistenceProvider(sessionFactory);
            using (var transaction = provider.BeginTransaction())
            {
                var db@ModelName@ = provider.GetById<DataModel.@ModelName@>(USER_ID);
                db@ModelName@.@ModelName@Name += ".";
					 db@ModelName@.SaltPassword = "password";
                db@ModelName@.Balance = 100;
                provider.Save(db@ModelName@);
                transaction.Commit();
            }
        }
        
    }
}