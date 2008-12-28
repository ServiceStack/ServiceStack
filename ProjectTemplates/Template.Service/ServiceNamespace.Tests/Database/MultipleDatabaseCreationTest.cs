/*
// $Id$
//
// Revision      : $Revision: 703 $
// Modified Date : $LastChangedDate: 2008-12-23 15:50:11 +0000 (Tue, 23 Dec 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using Ddn.Common.Testing;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @ServiceNamespace@.Tests.Support;

namespace @ServiceNamespace@.Tests.Database
{
	[Ignore("This should be moved to an Integration Test project as it relies on external dependencies")]
	[TestFixture]
	public class MultipleDatabaseCreationTest : BaseProviderTestFixture
	{
		public MultipleDatabaseCreationTest() 
			: base(new TestParameters(), 3) {}

		[Test]
		public void CreateMultipleTestDatabase()
		{
			Assert.That(base.DatabaseCount, Is.EqualTo(3));
			Assert.That(base.Databases, Has.Length(3));

			Console.WriteLine("Test database 0: {0} created successfully", base.Databases[0].DatabaseName);
			Console.WriteLine("Test database 1: {0} created successfully", base.Databases[1].DatabaseName);
			Console.WriteLine("Test database 2: {0} created successfully", base.Databases[2].DatabaseName);
		}
	}
}