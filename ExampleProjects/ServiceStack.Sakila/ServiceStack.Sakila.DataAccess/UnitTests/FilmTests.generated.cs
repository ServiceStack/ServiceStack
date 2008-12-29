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
    public partial class FilmTests : UNuitTestBase
    {
        protected IFilmManager manager;
		
		public FilmTests()
        {
            manager = managerFactory.GetFilmManager();
        }
		
		protected Film CreateNewFilm()
		{
			Film entity = new Film();
			
			
			entity.Title = "Test Test ";
			entity.Description = "Test Test ";
			entity.ReleaseYear = null;
			entity.RentalDuration = default(Byte);
			entity.RentalRate = 84;
			entity.Length = default(UInt16);
			entity.ReplacementCost = 97;
			entity.Rating = null;
			entity.SpecialFeature = null;
			entity.LastUpdate = DateTime.Now;
			
			ILanguageManager languageManager = managerFactory.GetLanguageManager();
			entity.LanguageMember = languageManager.GetAll(1)[0];
			
			ILanguageManager languageManager = managerFactory.GetLanguageManager();
			entity.LanguageMember = languageManager.GetAll(1)[0];
			
			return entity;
		}
		protected Film GetFirstFilm()
        {
            IList<Film> entityList = manager.GetAll(1);
            if (entityList.Count == 0)
                Assert.Fail("All tables must have at least one row for unit tests to succeed.");
            return entityList[0];
        }
		
		[Test]
        public void Create()
        {
            try
            {
				Film entity = CreateNewFilm();
				
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
                Film entityA = CreateNewFilm();
				manager.Save(entityA);

                Film entityB = manager.GetById(entityA.Id);

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
                Film entityA = GetFirstFilm();
				entityA.Title = "Test Test ";
				
				manager.Update(entityA);

                Film entityB = manager.GetById(entityA.Id);

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
                Film entity = GetFirstFilm();
				
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