using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/customers")]
	[Route("/customers/{Id}")]
    public class Customers : IReturn<CustomersResponse>
	{
		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Company { get; set; }
		public decimal Discount { get; set; }
		public string Address { get; set; }
		public string Postcode { get; set; }
		public bool HasDiscount { get; set; }
	}

    public class CustomersResponse
    {
        public Customers Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

	public class CustomersValidator : AbstractValidator<Customers>
	{
		public CustomersValidator()
		{
			RuleFor(x => x.Id).NotEqual(default(int));

			RuleSet(ApplyTo.Post | ApplyTo.Put, () => {
				RuleFor(x => x.LastName).NotEmpty().WithErrorCode("ShouldNotBeEmpty");
				RuleFor(x => x.FirstName).NotEmpty().WithMessage("Please specify a first name");
				RuleFor(x => x.Company).NotNull();
				RuleFor(x => x.Discount).NotEqual(0).When(x => x.HasDiscount);
				RuleFor(x => x.Address).Length(20, 250);
				RuleFor(x => x.Postcode).Must(BeAValidPostcode).WithMessage("Please specify a valid postcode");
			});
		}

		static readonly Regex UsPostCodeRegEx = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

		private bool BeAValidPostcode(string postcode)
		{
			return !string.IsNullOrEmpty(postcode) && UsPostCodeRegEx.IsMatch(postcode);
		}
	}

    public class CustomerService : ServiceInterface.Service
	{
		public object Get(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public object Post(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public object Put(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public object Delete(Customers request)
		{
			return new CustomersResponse { Result = request };
		}
	}

}