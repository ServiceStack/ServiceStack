using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;

namespace ServiceStack.SakilaNHibernate.Tests.Logic.Validation
{
	[TestFixture]
	public class CustomerDetailsValidationTests : ValidationTestBase
	{
		static Customer ValidCustomerDetails
		{
			get
			{
				return new Customer {
					Id = new Random().Next(),
				};
			}
		}

		[Test]
		public void ValidCustomerDetailsPassesValidation()
		{
			Assert.That(ValidCustomerDetails.Validate().Errors.Count, Is.EqualTo(0));
		}

	}
}