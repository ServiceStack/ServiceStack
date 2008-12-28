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
    public partial class LanguageTests : UNuitTestBase
    {
        protected ILanguageManager manager;
		
		public LanguageTests()
        {
            manager = managerFactory.GetLanguageManager();
        }
		
		protected Language CreateNewLanguage()
		{
			Language entity = new Language();
			
			
			entity.Name = "Test Test Test Test Test Test Test Test Test Test Test Test";
			entity.LastUpdate = DateTime.Now;
			
			return entity;
		}
		protected Language GetFirstLanguage()
        {
            IList<Language> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Language entity = CreateNewLanguage();
				
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
                Language entityA = CreateNewLanguage();
				manager.Save(entityA);

                Language entityB = manager.GetById(entityA.Id);

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
                Language entityA = GetFirstLanguage();
				entityA.Name = "Test Test Test Test Test Test Test Test";
				
				manager.Update(entityA);

                Language entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.Name, entityB.Name);
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
                Language entity = GetFirstLanguage();
				
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