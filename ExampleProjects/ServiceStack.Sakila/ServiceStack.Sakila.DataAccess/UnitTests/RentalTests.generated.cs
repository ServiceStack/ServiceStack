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
    public partial class RentalTests : UNuitTestBase
    {
        protected IRentalManager manager;
		
		public RentalTests()
        {
            manager = managerFactory.GetRentalManager();
        }
		
		protected Rental CreateNewRental()
		{
			Rental entity = new Rental();
			
			
			entity.RentalDate = DateTime.Now;
			entity.ReturnDate = DateTime.Now;
			entity.LastUpdate = DateTime.Now;
			
			ICustomerManager customerManager = managerFactory.GetCustomerManager();
			entity.CustomerMember = customerManager.GetAll(1)[0];
			
			IInventoryManager inventoryManager = managerFactory.GetInventoryManager();
			entity.InventoryMember = inventoryManager.GetAll(1)[0];
			
			IStaffManager staffManager = managerFactory.GetStaffManager();
			entity.StaffMember = staffManager.GetAll(1)[0];
			
			return entity;
		}
		protected Rental GetFirstRental()
        {
            IList<Rental> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Rental entity = CreateNewRental();
				
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
                Rental entityA = CreateNewRental();
				manager.Save(entityA);

                Rental entityB = manager.GetById(entityA.Id);

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
                Rental entityA = GetFirstRental();
				entityA.RentalDate = DateTime.Now;
				
				manager.Update(entityA);

                Rental entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.RentalDate, entityB.RentalDate);
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
                Rental entity = GetFirstRental();
				
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