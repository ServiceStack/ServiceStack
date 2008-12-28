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
    public partial class CityTests : UNuitTestBase
    {
        protected ICityManager manager;
		
		public CityTests()
        {
            manager = managerFactory.GetCityManager();
        }
		
		protected City CreateNewCity()
		{
			City entity = new City();
			
			
			entity.City = "Test Test ";
			entity.LastUpdate = DateTime.Now;
			
			ICountryManager countryManager = managerFactory.GetCountryManager();
			entity.CountryMember = countryManager.GetAll(1)[0];
			
			return entity;
		}
		protected City GetFirstCity()
        {
            IList<City> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				City entity = CreateNewCity();
				
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
                City entityA = CreateNewCity();
				manager.Save(entityA);

                City entityB = manager.GetById(entityA.Id);

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
                City entityA = GetFirstCity();
				entityA.City = "Test Test ";
				
				manager.Update(entityA);

                City entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.City, entityB.City);
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
                City entity = GetFirstCity();
				
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