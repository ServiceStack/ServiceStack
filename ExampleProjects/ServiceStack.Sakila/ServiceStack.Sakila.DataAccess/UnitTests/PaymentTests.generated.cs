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
    public partial class PaymentTests : UNuitTestBase
    {
        protected IPaymentManager manager;
		
		public PaymentTests()
        {
            manager = managerFactory.GetPaymentManager();
        }
		
		protected Payment CreateNewPayment()
		{
			Payment entity = new Payment();
			
			
			entity.Amount = 11;
			entity.PaymentDate = DateTime.Now;
			entity.LastUpdate = DateTime.Now;
			
			ICustomerManager customerManager = managerFactory.GetCustomerManager();
			entity.CustomerMember = customerManager.GetAll(1)[0];
			
			IRentalManager rentalManager = managerFactory.GetRentalManager();
			entity.RentalMember = rentalManager.GetAll(1)[0];
			
			IStaffManager staffManager = managerFactory.GetStaffManager();
			entity.StaffMember = staffManager.GetAll(1)[0];
			
			return entity;
		}
		protected Payment GetFirstPayment()
        {
            IList<Payment> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Payment entity = CreateNewPayment();
				
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
                Payment entityA = CreateNewPayment();
				manager.Save(entityA);

                Payment entityB = manager.GetById(entityA.Id);

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
                Payment entityA = GetFirstPayment();
				entityA.Amount = 23;
				
				manager.Update(entityA);

                Payment entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.Amount, entityB.Amount);
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
                Payment entity = GetFirstPayment();
				
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