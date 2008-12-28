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
    public partial class ActorTests : UNuitTestBase
    {
        protected IActorManager manager;
		
		public ActorTests()
        {
            manager = managerFactory.GetActorManager();
        }
		
		protected Actor CreateNewActor()
		{
			Actor entity = new Actor();
			
			
			entity.FirstName = "Test Test ";
			entity.LastName = "Test Test ";
			entity.LastUpdate = DateTime.Now;
			
			return entity;
		}
		protected Actor GetFirstActor()
        {
            IList<Actor> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Actor entity = CreateNewActor();
				
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
                Actor entityA = CreateNewActor();
				manager.Save(entityA);

                Actor entityB = manager.GetById(entityA.Id);

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
                Actor entityA = GetFirstActor();
				entityA.FirstName = "Test Test ";
				
				manager.Update(entityA);

                Actor entityB = manager.GetById(entityA.Id);

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
                Actor entity = GetFirstActor();
				
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