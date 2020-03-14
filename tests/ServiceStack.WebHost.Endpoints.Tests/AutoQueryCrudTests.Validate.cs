using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ValidateCreateRockstar 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate(nameof(ValidateScripts.NotNull))]
        // [Validate("NotNull")]
        public string FirstName { get; set; }
        
        //Added by Fluent Validator 
        public string LastName { get; set; }
        
        // [Validate("[" + nameof(ValidateScripts.NotNull) + "," + nameof(ValidateScripts.Length) + "(13,100)]")] e.g. Typed
        // [Validate("[NotNull,Length(13,100)]")]
        [Validate("NotNull")]
        [Validate("InclusiveBetween(13,100)")]
        public int? Age { get; set; }

        [Validate("NotEmpty(default('DateTime'))")]
        //[Validate("NotEmpty")] equivalent to above thanks to: Validators.AppendDefaultValueOnEmptyValidators
        public DateTime DateOfBirth { get; set; }
        
        public DateTime? DateDied { get; set; }
        
        public LivingStatus LivingStatus { get; set; }
    }

    public class ValidateCreateRockstarValidator : AbstractValidator<ValidateCreateRockstar>
    {
        public ValidateCreateRockstarValidator()
        {
            RuleFor(x => x.LastName).NotNull();
        }
    }

    [AutoPopulate(nameof(LivingStatus), Value = LivingStatus.Alive)]
    public class NoAbstractValidator 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")]
        public string FirstName { get; set; }
        
        [Validate("NotNull")]
        public string LastName { get; set; }

        [Validate("[NotNull,InclusiveBetween(13,100)]")]
        public int? Age { get; set; }
     
        [Validate("NotEmpty")]
        public DateTime DateOfBirth { get; set; }
     
        public LivingStatus LivingStatus { get; set; }
    }

    public class EmptyValidators 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        // [Validate("NotEmpty(0)")]
        [Validate("NotEmpty")]
        public int Int { get; set; }
        [Validate("NotEmpty")]
        public int? NInt { get; set; }
        [Validate("NotEmpty")]
        // [Validate("NotEmpty(default('System.TimeSpan'))")]
        public TimeSpan TimeSpan { get; set; }
        [Validate("NotEmpty")]
        public TimeSpan? NTimeSpan { get; set; }
        [Validate("NotEmpty")]
        public string String { get; set; }
        [Validate("NotEmpty")]
        public int[] IntArray { get; set; }
        [Validate("NotEmpty")]
        public List<string> StringList { get; set; }
    }

    public class TriggerAllValidators 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("CreditCard")]
        public string CreditCard { get; set; }
        [Validate("Email")]
        public string Email { get; set; }
        [Validate("Empty")]
        public string Empty { get; set; }
        [Validate("Equal('Equal')")]
        public string Equal { get; set; }
        [Validate("ExclusiveBetween(10, 20)")]
        public int ExclusiveBetween { get; set; }
        [Validate("GreaterThanOrEqual(10)")]
        public int GreaterThanOrEqual { get; set; }
        [Validate("GreaterThan(10)")]
        public int GreaterThan { get; set; }
        [Validate("InclusiveBetween(10, 20)")]
        public int InclusiveBetween { get; set; }
        [Validate("ExactLength(10)")]
        public string Length { get; set; }
        [Validate("LessThanOrEqual(10)")]
        public int LessThanOrEqual { get; set; }
        [Validate("LessThan(10)")]
        public int LessThan { get; set; }
        [Validate("NotEmpty")]
        public string NotEmpty { get; set; }
        [Validate("NotEqual('NotEqual')")]
        public string NotEqual { get; set; }
        [Validate("Null")]
        public string Null { get; set; }
        [Validate("RegularExpression('^[a-z]*$')")]
        public string RegularExpression { get; set; }
        [Validate("ScalePrecision(1,1)")]
        public decimal ScalePrecision { get; set; }
    }

    public class DynamicValidationRules
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")]
        public string FirstName { get; set; }
        
        //[Validate("NotNull")] added in IValidationSource
        public string LastName { get; set; }

        // [Validate("[NotNull,InclusiveBetween(13,100)]")]
        [Validate("NotNull")]
        //[Validate("InclusiveBetween(13,100)")] added in IValidationSource
        public int? Age { get; set; }
     
        [Validate("NotEmpty")]
        public DateTime DateOfBirth { get; set; }
     
        public LivingStatus LivingStatus { get; set; }
    }

    public class CustomValidationErrors
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull", ErrorCode = "ZERROR")]
        public string ErrorCode { get; set; }
        
        [Validate("InclusiveBetween(1,2)", ErrorCode = "ZERROR", 
            Message = "{PropertyName} has to be between {From} and {To}, you: {PropertyValue}")]
        public int ErrorCodeAndMessage { get; set; }

        [Validate("NotNull", ErrorCode = "RuleMessage")]
        public string ErrorCodeRule { get; set; }
    }
    
    public partial class AutoQueryCrudTests
    {
        private bool UseDbSource = true;
        
        partial void OnConfigure(AutoQueryAppHost host, Container container)
        {
            host.Plugins.Add(new ValidationFeature());

            if (UseDbSource)
            {
                container.Register<IValidationSource>(c => 
                    new OrmLiteValidationSource(c.Resolve<IDbConnectionFactory>(), host.GetMemoryCacheClient()));
            }
            else
            {
                container.Register<IValidationSource>(new MemoryValidationSource());
            }
            
            var validationSource = container.Resolve<IValidationSource>();
            validationSource.InitSchema();
            validationSource.SaveValidationRules(new List<ValidateRule> {
                new ValidateRule { Type = nameof(DynamicValidationRules), Field = nameof(DynamicValidationRules.LastName), Validator = "NotNull" },
                new ValidateRule { Type = nameof(DynamicValidationRules), Field = nameof(DynamicValidationRules.Age), Validator = "InclusiveBetween(13,100)" },
            });
        }

        private static void AssertErrorResponse(WebServiceException ex)
        {
            Assert.That(ex.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(ex.ErrorMessage, Is.EqualTo("'First Name' must not be empty."));
            var status = ex.ResponseStatus;
            Assert.That(status.Errors.Count, Is.EqualTo(3));

            var fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.FirstName));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'First Name' must not be empty."));
            
            fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.Age));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'Age' must not be empty."));
            
            fieldError = status.Errors.First(x => x.FieldName == nameof(RockstarBase.LastName));
            Assert.That(fieldError.ErrorCode, Is.EqualTo("NotNull"));
            Assert.That(fieldError.Message, Is.EqualTo("'Last Name' must not be empty."));
        }

        [Test]
        public void Does_validate_when_no_Abstract_validator()
        {
            try
            {
                var response = client.Post(new NoAbstractValidator {
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }

            try
            {
                var response = client.Post(new NoAbstractValidator {
                    FirstName = "A",
                    LastName = "B",
                    Age = 12,
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("'Age' must be between 13 and 100. You entered 12."));
                var status = ex.ResponseStatus;
                Assert.That(status.Errors.Count, Is.EqualTo(1));
            }
            
            client.Post(new NoAbstractValidator {
                FirstName = "A",
                LastName = "B",
                Age = 13,
                DateOfBirth = new DateTime(2001,1,1),
            });
        }

        [Test]
        public void Does_validate_DynamicValidationRules_with_IValidationSource_rules()
        {
            try
            {
                var response = client.Post(new DynamicValidationRules {
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }

            try
            {
                var response = client.Post(new DynamicValidationRules {
                    FirstName = "A",
                    LastName = "B",
                    Age = 12,
                    DateOfBirth = new DateTime(2001,1,1),
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(ex.ErrorMessage, Is.EqualTo("'Age' must be between 13 and 100. You entered 12."));
                var status = ex.ResponseStatus;
                Assert.That(status.Errors.Count, Is.EqualTo(1));
            }
            
            client.Post(new DynamicValidationRules {
                FirstName = "A",
                LastName = "B",
                Age = 13,
                DateOfBirth = new DateTime(2001,1,1),
            });
        }
        
        [Test]
        public void Does_validate_combined_declarative_and_AbstractValidator()
        {
            try
            {
                var response = client.Post(new ValidateCreateRockstar());
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                AssertErrorResponse(ex);
                Console.WriteLine(ex);
            }
        }

        [Test]
        public void Does_validate_all_NotEmpty_Fields()
        {
            try
            {
                var response = client.Post(new EmptyValidators());
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.Errors.Count, 
                    Is.EqualTo(typeof(EmptyValidators).GetPublicProperties().Length));
                Assert.That(ex.ResponseStatus.Errors.All(x => x.ErrorCode == "NotEmpty"));
                Console.WriteLine(ex);
            }
        }

        [Test]
        public void Does_Validate_TriggerAllValidators()
        {
            try
            {
                var response = client.Post(new TriggerAllValidators {
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
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ValidationExceptionTests.AssertTriggerValidators(ex);
                Console.WriteLine(ex);
            }
        }
    }
}