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
    public partial class FilmTextTests : UNuitTestBase
    {
        protected IFilmTextManager manager;
		
		public FilmTextTests()
        {
            manager = managerFactory.GetFilmTextManager();
        }
		
		protected FilmText CreateNewFilmText()
		{
			FilmText entity = new FilmText();
			
			// You may need to maually enter this key if there is a constraint violation.
			entity.Id = default(Int16);
			
			entity.Title = "Test Test ";
			entity.Description = "Test Test ";
			
			return entity;
		}
		protected FilmText GetFirstFilmText()
        {
            IList<FilmText> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				FilmText entity = CreateNewFilmText();
				
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
                FilmText entityA = CreateNewFilmText();
				manager.Save(entityA);

                FilmText entityB = manager.GetById(entityA.Id);

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
                FilmText entityA = GetFirstFilmText();
				entityA.Title = "Test Test ";
				
				manager.Update(entityA);

                FilmText entityB = manager.GetById(entityA.Id);

                Assert.AreEqual(entityA.Title, entityB.Title);
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
                FilmText entity = GetFirstFilmText();
				
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