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
    public partial class InventoryTests : UNuitTestBase
    {
        protected IInventoryManager manager;
		
		public InventoryTests()
        {
            manager = managerFactory.GetInventoryManager();
        }
		
		protected Inventory CreateNewInventory()
		{
			Inventory entity = new Inventory();
			
			
			entity.LastUpdate = DateTime.Now;
			
			IFilmManager filmManager = managerFactory.GetFilmManager();
			entity.FilmMember = filmManager.GetAll(1)[0];
			
			IStoreManager storeManager = managerFactory.GetStoreManager();
			entity.StoreMember = storeManager.GetAll(1)[0];
			
			return entity;
		}
		protected Inventory GetFirstInventory()
        {
            IList<Inventory> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Inventory entity = CreateNewInventory();
				
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
                Inventory entityA = CreateNewInventory();
				manager.Save(entityA);

                Inventory entityB = manager.GetById(entityA.Id);

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
                Inventory entityA = GetFirstInventory();
				entityA.LastUpdate = DateTime.Now;
				
				manager.Update(entityA);

                Inventory entityB = manager.GetById(entityA.Id);

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
                Inventory entity = GetFirstInventory();
				
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