using System;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TriggerValidators : IReturn<TriggerValidators>
    {
        public string CreditCard { get; set; }
        public string Email { get; set; }
        public string Empty { get; set; }
        public string Equal { get; set; }
        public int ExclusiveBetween { get; set; }
        public int GreaterThanOrEqual { get; set; }
        public int GreaterThan { get; set; }
        public int InclusiveBetween { get; set; }
        public string Length { get; set; }
        public int LessThanOrEqual { get; set; }
        public int LessThan { get; set; }
        public string NotEmpty { get; set; }
        public string NotEqual { get; set; }
        public string Null { get; set; }
        public string RegularExpression { get; set; }
        public decimal ScalePrecision { get; set; }
    }

    public class TriggerValidatorsValidtor : AbstractValidator<TriggerValidators>
    {
        public TriggerValidatorsValidtor()
        {
            RuleFor(x => x.CreditCard).CreditCard();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Empty).Empty();
            RuleFor(x => x.Equal).Equal("Equal");
            RuleFor(x => x.ExclusiveBetween).ExclusiveBetween(10, 20);
            RuleFor(x => x.GreaterThanOrEqual).GreaterThanOrEqualTo(10);
            RuleFor(x => x.GreaterThan).GreaterThan(10);
            RuleFor(x => x.InclusiveBetween).InclusiveBetween(10, 20);
            RuleFor(x => x.Length).Length(10);
            RuleFor(x => x.LessThanOrEqual).LessThanOrEqualTo(10);
            RuleFor(x => x.LessThan).LessThan(10);
            RuleFor(x => x.NotEmpty).NotEmpty();
            RuleFor(x => x.NotEqual).NotEqual("NotEqual");
            RuleFor(x => x.Null).Null();
            RuleFor(x => x.RegularExpression).Matches(@"^[a-z]*$");
            RuleFor(x => x.ScalePrecision).SetValidator(new ScalePrecisionValidator(1, 1));
        }
    }

    public class ValidatorIssues : IReturn<ValidatorIssues>
    {
        public DateTime ValidTo { get; set; }
    }

    public class ValidatorIssuesValidator : AbstractValidator<ValidatorIssues>
    {
        public ValidatorIssuesValidator()
        {
            RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => DateTime.UtcNow);
        }
    }

    public class ValidationService : Service
    {
        public object Any(TriggerValidators request) => request;
        public object Any(ValidatorIssues request) => request;
    }

    public class ValidationExceptionTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ValidationExceptionTests), typeof(ValidationExceptionTests).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());

                container.RegisterValidator(typeof(TriggerValidatorsValidtor));
                container.RegisterValidator(typeof(ValidatorIssuesValidator));
            }
        }

        private readonly ServiceStackHost appHost;
        public ValidationExceptionTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        protected virtual JsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn);

        [Test]
        public void Triggering_all_validtors_returns_right_ErrorCode()
        {
            var client = GetClient();
            var request = new TriggerValidators
            {
                CreditCard = "NotCreditCard",
                Email = "NotEmail",
                Empty = "NotEmpty",
                Equal = "NotEqual",
                ExclusiveBetween = 1,
                GreaterThan = 1,
                GreaterThanOrEqual = 1,
                InclusiveBetween = 1,
                Length = "Length",
                LessThan = 20,
                LessThanOrEqual = 20,
                NotEmpty = "",
                NotEqual = "NotEqual",
                Null = "NotNull",
                RegularExpression = "FOO",
                ScalePrecision = 123.456m
            };

            try
            {
                var response = client.Post(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                //ex.ResponseStatus.PrintDump();
                var errors = ex.ResponseStatus.Errors;
                Assert.That(errors.First(x => x.FieldName == "CreditCard").ErrorCode, Is.EqualTo("CreditCard"));
                Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
                Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
                Assert.That(errors.First(x => x.FieldName == "Empty").ErrorCode, Is.EqualTo("Empty"));
                Assert.That(errors.First(x => x.FieldName == "Equal").ErrorCode, Is.EqualTo("Equal"));
                Assert.That(errors.First(x => x.FieldName == "ExclusiveBetween").ErrorCode, Is.EqualTo("ExclusiveBetween"));
                Assert.That(errors.First(x => x.FieldName == "GreaterThan").ErrorCode, Is.EqualTo("GreaterThan"));
                Assert.That(errors.First(x => x.FieldName == "GreaterThanOrEqual").ErrorCode, Is.EqualTo("GreaterThanOrEqual"));
                Assert.That(errors.First(x => x.FieldName == "InclusiveBetween").ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(errors.First(x => x.FieldName == "Length").ErrorCode, Is.EqualTo("Length"));
                Assert.That(errors.First(x => x.FieldName == "LessThan").ErrorCode, Is.EqualTo("LessThan"));
                Assert.That(errors.First(x => x.FieldName == "LessThanOrEqual").ErrorCode, Is.EqualTo("LessThanOrEqual"));
                Assert.That(errors.First(x => x.FieldName == "NotEmpty").ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(errors.First(x => x.FieldName == "NotEqual").ErrorCode, Is.EqualTo("NotEqual"));
                Assert.That(errors.First(x => x.FieldName == "Null").ErrorCode, Is.EqualTo("Null"));
                Assert.That(errors.First(x => x.FieldName == "RegularExpression").ErrorCode, Is.EqualTo("RegularExpression"));
                Assert.That(errors.First(x => x.FieldName == "ScalePrecision").ErrorCode, Is.EqualTo("ScalePrecision"));
            }
        }

        [Test]
        public void Does_handle_reported_issues_correctly()
        {
            var client = GetClient();
            var request = new ValidatorIssues
            {
                ValidTo = DateTime.UtcNow.AddDays(-1),
            };

            try
            {
                var response = client.Post(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                var errors = ex.ResponseStatus.Errors;
                Assert.That(errors.First(x => x.FieldName == "ValidTo").ErrorCode, Is.EqualTo("GreaterThanOrEqual"));
            }
        }
    }
}