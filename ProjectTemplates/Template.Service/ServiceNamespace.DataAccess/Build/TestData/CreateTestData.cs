/*
// $Id: CreateTestData.cs 276 2008-12-02 10:52:36Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 276 $
// Modified Date : $LastChangedDate: 2008-12-02 10:52:36 +0000 (Tue, 02 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;
using @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.DataAccess.Build.TestData
{
	[TestFixture]
	public class CreateTestData : IDisposable
	{
		private ISessionFactory sessionFactory;
		private ISession session;

		public TestData@ServiceName@ TestData { get; set; }

		[TestFixtureSetUp]
		public void Load()
		{
			sessionFactory = new Configuration().Configure().BuildSessionFactory();
			session = sessionFactory.OpenSession();
			TestData = new TestData@ServiceName@();
		}

		[TestFixtureTearDown]
		public void Dispose()
		{
			if (session == null) return;

			session.Dispose();
			sessionFactory.Dispose();
		}

		[Test]
		public void AddNew@ModelName@()
		{
			session.Transaction.Begin();
			var user = TestData.@ModelName@;

			session.Save(user);
			session.Flush();

			AddCreditCard(user);
			Add@ModelName@Order(user);
			Add@ModelName@Set(user);
			Add@ModelName@Product(user);

			session.Flush();
			session.Transaction.Commit();
		}

		[Test]
		public void DeleteAll@ModelName@s()
		{
			session.Transaction.Begin();
			var users = session.CreateCriteria(typeof(@ModelName@)).List();
			foreach (var user in users)
			{
				Delete@ModelName@((@ModelName@)user);
			}
			session.Transaction.Commit();
		}

		private void Add@ModelName@Set(@ModelName@ user)
		{
			var @ModelName@Set = TestData.@ModelName@Set;
			user.@ModelName@Sets.Add(@ModelName@Set);
			@ModelName@Set.@ModelName@Member = user;
			session.Save(@ModelName@Set);
			session.Flush();

			var @ModelName@SetProduct = TestData.@ModelName@SetProduct;
			@ModelName@SetProduct.@ModelName@SetMember = @ModelName@Set;
			session.Save(@ModelName@SetProduct);
		}

		private void Add@ModelName@Product(@ModelName@ user)
		{
			var @ModelName@Product = TestData.@ModelName@Product;
			user.@ModelName@Products.Add(@ModelName@Product);
			@ModelName@Product.@ModelName@Member = user;
			session.Save(@ModelName@Product);
			session.Flush();

			@ModelName@Product.Genres.Add(TestData.Genre);
			@ModelName@Product.Genres[0].@ModelName@ProductMember = @ModelName@Product;
			session.Save(@ModelName@Product.Genres[0]);
		}

		private void Add@ModelName@Order(@ModelName@ user)
		{
			var @ModelName@Order = TestData.@ModelName@Order;
			user.@ModelName@Orders.Add(@ModelName@Order);
			@ModelName@Order.@ModelName@Member = user;
			session.Save(@ModelName@Order);
			session.Flush();
			@ModelName@Order.@ModelName@OrderLineItems.Add(TestData.@ModelName@OrderLineItem);
			@ModelName@Order.@ModelName@OrderLineItems[0].@ModelName@OrderMember = @ModelName@Order;
			session.Save(@ModelName@Order.@ModelName@OrderLineItems[0]);
		}

		private void AddCreditCard(@ModelName@ user)
		{
			user.CreditCardInfos.Add(TestData.CreditCardInfo);
			user.CreditCardInfos[0].@ModelName@Member = user;
			session.Save(user.CreditCardInfos[0]);
		}

		public void Delete@ModelName@(@ModelName@ user)
		{
			foreach (var CreditCardInfo in user.CreditCardInfos)
			{
				session.Delete(CreditCardInfo);
			}
			foreach (var @ModelName@Order in user.@ModelName@Orders)
			{
				foreach (var @ModelName@OrderLineItem in @ModelName@Order.@ModelName@OrderLineItems)
				{
					session.Delete(@ModelName@OrderLineItem);
				}
				session.Delete(@ModelName@Order);
			}
			foreach (var @ModelName@Set in user.@ModelName@Sets)
			{
				session.Delete(@ModelName@Set);
			}
			session.Delete(user);
		}

	}
}