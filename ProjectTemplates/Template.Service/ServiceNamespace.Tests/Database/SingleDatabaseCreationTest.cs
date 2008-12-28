/*
// $Id: SingleDatabaseCreationTest.cs 700 2008-12-23 15:27:52Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 700 $
// Modified Date : $LastChangedDate $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
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
	public class SingleDatabaseCreationTest : BaseProviderTestFixture
	{
		public SingleDatabaseCreationTest() 
			: base(new TestParameters(), 1)
		{
		}

		[Test]
		public void CreateTestDatabase()
		{
			Assert.That(base.DatabaseCount, Is.EqualTo(1));
			Assert.That(base.Databases, Has.Length(1));

			Console.WriteLine("Test database {0} created successfully", base.Databases[0].DatabaseName);
		}
	}
}