using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@;

namespace @ServiceNamespace@.Tests.Logic.Validation
{
	[TestFixture]
	public class @ModelName@DetailsValidationTests : ValidationTestBase
	{
		static @ModelName@ Valid@ModelName@Details
		{
			get
			{
				return new @ModelName@ {
					Id = new Random().Next(),
				};
			}
		}

		[Test]
		public void Valid@ModelName@DetailsPassesValidation()
		{
			Assert.That(Valid@ModelName@Details.Validate().Errors.Count, Is.EqualTo(0));
		}

	}
}