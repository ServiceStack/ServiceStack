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
    public partial class CustomerTests : UNuitTestBase
    {
        protected ICustomerManager manager;
		
		public CustomerTests()
        {
            manager = managerFactory.GetCustomerManager();
        }
		
		protected Customer CreateNewCustomer()
		{
			Customer entity = new Customer();
			
			
			entity.FirstName = "Test Test ";
			entity.LastName = "Test Test ";
			entity.Email = "Test Test ";
			entity.Active = default(SByte);
			entity.CreateDate = DateTime.Now;
			entity.LastUpdate = DateTime.Now;
			
			IaddressManager addressManager = managerFactory.GetaddressManager();
			entity.addressMember = addressManager.GetAll(1)[0];
			
			IStoreManager storeManager = managerFactory.GetStoreManager();
			entity.StoreMember = storeManager.GetAll(1)[0];
			
			return entity;
		}
		protected Customer GetFirstCustomer()
        {
            IList<Customer> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Customer entity = CreateNewCustomer();
				
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
                Customer entityA = CreateNewCustomer();
				manager.Save(entityA);

                Customer entityB = manager.GetById(entityA.Id);

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
                Customer entityA = GetFirstCustomer();
				entityA.FirstName = "Test Test ";
				
				manager.Update(entityA);

                Customer entityB = manager.GetById(entityA.Id);

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
                Customer entity = GetFirstCustomer();
				
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