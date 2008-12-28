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
    public partial class addressTests : UNuitTestBase
    {
        protected IaddressManager manager;
		
		public addressTests()
        {
            manager = managerFactory.GetaddressManager();
        }
		
		protected address CreateNewaddress()
		{
			address entity = new address();
			
			
			entity.address = "Test Test ";
			entity.Address2 = "Test Test ";
			entity.District = "Test Test Test ";
			entity.PostalCode = "Test Test Test Test Test Tes";
			entity.Phone = "Test Test Test Test Te";
			entity.LastUpdate = DateTime.Now;
			
			ICityManager cityManager = managerFactory.GetCityManager();
			entity.CityMember = cityManager.GetAll(1)[0];
			
			return entity;
		}
		protected address GetFirstaddress()
        {
            IList<address> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				address entity = CreateNewaddress();
				
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
                address entityA = CreateNewaddress();
				manager.Save(entityA);

                address entityB = manager.GetById(entityA.Id);

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
                address entityA = GetFirstaddress();
				entityA.address = "Test Test ";
				
				manager.Update(entityA);

                address entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.address, entityB.address);
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
                address entity = GetFirstaddress();
				
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