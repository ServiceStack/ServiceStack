using NUnit.Framework;
using ServiceStack.Sakila.Tests.Integration.Support;

namespace ServiceStack.Sakila.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class StoreNewCustomerTests : IntegrationTestBase
	{
		[Test]
		public void StoreNewCustomerTest()
		{
			//var requestDto = new StoreCustomers {
			//    CustomerDetails = new CustomerDetails {
			//        CustomerName = "demis.bellot@gmail.com",
			//        Email = "demis.bellot@gmail.com",
			//        Title = "Mr",
			//        FirstName = "Demis",
			//        LastName = "Bellot",
			//        CountryName = "UK",
			//        LanguageCode = "en",
			//        SingleClickBuyEnabled = true,
			//        CanNotifyEmail = true,
			//    },
			//    Base64EncryptedPassword = ServerPublicKey.EncryptData("password"),
			//    Base64EncryptedConfirmPassword = ServerPublicKey.EncryptData("password"),
			//    PrimaryCreditCard = new CreditCardInfo {
			//        CardCvv = "100",
			//        CardHolderName = "CardName",
			//        CardNumber = "4462616876152220",
			//        CardType = "Visa",
			//        ExpiryDate = DateTime.Now.AddYears(1),
			//    },
			//    Version = 100,
			//};

			//var response = (StoreNewCustomerResponse)base.ExecuteService(requestDto);
			//Assert.That(response.ResponseStatus.ErrorCode, Is.Null);

			//var getCustomers = new GetCustomersPublicProfile { CustomerNames = new ArrayOfStringId(new[] { requestDto.CustomerDetails.CustomerName }) };

			//var responseDto = (GetCustomersPublicProfileResponse)base.ExecuteService(getCustomers);

			//Assert.That(responseDto.Customers.Count, Is.EqualTo(1));
			//Assert.That(responseDto.Customers[0].CustomerName, Is.EqualTo(requestDto.CustomerDetails.CustomerName));
		}
	}
}