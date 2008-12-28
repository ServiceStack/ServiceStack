using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ServiceStack.Sakila.DataAccess.ManagerObjects;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.UnitTests
{
	[TestFixture]
    public partial class CountryTests : UNuitTestBase
    {
        protected ICountryManager manager;
		
		public CountryTests()
        {
            manager = managerFactory.GetCountryManager();
        }
		
		protected Country CreateNewCountry()
		{
			Country entity = new Country();
			
			
			entity.Country = "Test Test ";
			entity.LastUpdate = DateTime.Now;
			
			return entity;
		}
		protected Country GetFirstCountry()
        {
            IList<Country> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Country entity = CreateNewCountry();
				
                object result = manager.Save(entity);

                Assert.IsNotNull(result);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
        [Test]
        public void Read()
        {
            try
            {
                Country entityA = CreateNewCountry();
				manager.Save(entityA);

                Country entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA, entityB);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
		[Test]
		public void Update()
        {
            try
            {
                Country entityA = GetFirstCountry();
				entityA.Country = "Test Test ";
				
				manager.Update(entityA);

                Country entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.Country, entityB.Country);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
        [Test]
        public void Delete()
        {
            try
            {
                Country entity = GetFirstCountry();
				
                manager.Delete(entity);

                entity = manager.GetById(entity.Id);
                Assert.IsNull(entity);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
	}
}