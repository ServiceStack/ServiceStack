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
    public partial class StaffTests : UNuitTestBase
    {
        protected IStaffManager manager;
		
		public StaffTests()
        {
            manager = managerFactory.GetStaffManager();
        }
		
		protected Staff CreateNewStaff()
		{
			Staff entity = new Staff();
			
			
			entity.FirstName = "Test Test ";
			entity.LastName = "Test Test ";
			entity.Picture = null;
			entity.Email = "Test Test ";
			entity.Active = default(SByte);
			entity.Username = "Test Test Test T";
			entity.Password = "Test Test ";
			entity.LastUpdate = DateTime.Now;
			
			IaddressManager addressManager = managerFactory.GetaddressManager();
			entity.addressMember = addressManager.GetAll(1)[0];
			
			IStoreManager storeManager = managerFactory.GetStoreManager();
			entity.StoreMember = storeManager.GetAll(1)[0];
			
			return entity;
		}
		protected Staff GetFirstStaff()
        {
            IList<Staff> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Staff entity = CreateNewStaff();
				
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
                Staff entityA = CreateNewStaff();
				manager.Save(entityA);

                Staff entityB = manager.GetById(entityA.Id);

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
                Staff entityA = GetFirstStaff();
				entityA.FirstName = "Test Test ";
				
				manager.Update(entityA);

                Staff entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.FirstName, entityB.FirstName);
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
                Staff entity = GetFirstStaff();
				
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