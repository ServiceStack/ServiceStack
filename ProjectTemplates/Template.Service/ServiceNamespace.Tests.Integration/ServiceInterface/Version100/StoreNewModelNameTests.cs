using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class StoreNew@ModelName@Tests : BaseIntegrationTest
	{
		[Test]
		public void StoreNew@ModelName@Test()
		{
			var requestDto = new StoreNew@ModelName@ {
				@ModelName@Details = new @ModelName@Details {
					@ModelName@Name = "demis.bellot@gmail.com",
					Email = "demis.bellot@gmail.com",
					Title = "Mr",
					FirstName = "Demis",
					LastName = "Bellot",
					Country = "UK",
					LanguageCode = "en",
					SingleClickBuyEnabled = true,
					CanNotifyEmail = true,
				},
				Base64EncryptedPassword = ServerPublicKey.EncryptData("password"),
				Base64EncryptedConfirmPassword = ServerPublicKey.EncryptData("password"),
				PrimaryCreditCard = new CreditCardInfo {
					CardCvv = "100",
					CardHolderName = "CardName",
					CardNumber = "4462616876152220",
					CardType = "Visa",
					ExpiryDate = DateTime.Now.AddYears(1),
				},
				Version = 100,
			};

			var response = (StoreNew@ModelName@Response)base.ExecuteService(requestDto);
			Assert.That(response.ResponseStatus.ErrorCode, Is.Null);

			var get@ModelName@s = new Get@ModelName@sPublicProfile { @ModelName@Names = new ArrayOfStringId(new[] { requestDto.@ModelName@Details.@ModelName@Name }) };

			var responseDto = (Get@ModelName@sPublicProfileResponse)base.ExecuteService(get@ModelName@s);

            Assert.That(responseDto.PublicProfiles.Count, Is.EqualTo(1));
			Assert.That(responseDto.PublicProfiles[0].@ModelName@Name, Is.EqualTo(requestDto.@ModelName@Details.@ModelName@Name));
		}
	}
}