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
    public partial class StoreTests : UNuitTestBase
    {
        protected IStoreManager manager;
		
		public StoreTests()
        {
            manager = managerFactory.GetStoreManager();
        }
		
		protected Store CreateNewStore()
		{
			Store entity = new Store();
			
			
			entity.LastUpdate = DateTime.Now;
			
			IaddressManager addressManager = managerFactory.GetaddressManager();
			entity.addressMember = addressManager.GetAll(1)[0];
			
			IStaffManager staffManager = managerFactory.GetStaffManager();
			entity.StaffMember = staffManager.GetAll(1)[0];
			
			return entity;
		}
		protected Store GetFirstStore()
        {
            IList<Store> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Store entity = CreateNewStore();
				
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
                Store entityA = CreateNewStore();
				manager.Save(entityA);

                Store entityB = manager.GetById(entityA.Id);

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
                Store entityA = GetFirstStore();
				entityA.LastUpdate = DateTime.Now;
				
				manager.Update(entityA);

                Store entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.LastUpdate, entityB.LastUpdate);
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
                Store entity = GetFirstStore();
				
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