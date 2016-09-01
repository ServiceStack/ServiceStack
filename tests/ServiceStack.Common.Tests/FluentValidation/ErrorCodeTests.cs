#if !NETCORE_SUPPORT
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;

namespace ServiceStack.Common.Tests.FluentValidation
{
    [TestFixture]
    public class ErrorCodeTests
    {
        public class Person
        {
            public string Firstname { get; set; }
            public string CreditCard { get; set; }
            public int Age { get; set; }
            public List<Car> Cars { get; set; }
            public List<Car> Favorites { get; set; }
            public string Lastname { get; set; }
        }

        public class Car
        {
            public string Manufacturer { get; set; }
            public int Age { get; set; }
        }

        public class PersonValidator : AbstractValidator<Person>
        {
            public PersonValidator()
            {
                RuleFor(x => x.Firstname).Matches("asdfj");

                RuleFor(x => x.CreditCard).CreditCard().Length(10).EmailAddress().Equal("537")
                    .ExclusiveBetween("asdlöfjasdf", "asldfjlöakjdfsadf");

                RuleFor(x => x.Age).GreaterThan(100).GreaterThanOrEqualTo(100).InclusiveBetween(100, 200).LessThan(10);

                RuleFor(x => x.Cars).SetCollectionValidator(new CarValidator());
                RuleFor(x => x.Favorites).NotNull().NotEmpty().WithErrorCode("ShouldNotBeEmpty");

                RuleFor(x => x.Lastname).NotEmpty();
            }
        }

        public class CarValidator : AbstractValidator<Car>
        {
            public CarValidator()
            {
                RuleFor(x => x.Age).LessThanOrEqualTo(20).NotEqual(100);
                RuleFor(x => x.Manufacturer).Must(m => m == "BMW");
            }
        }

        public ValidationResult Result { get; set; }

        [TestFixtureSetUp]
        public void SetUp()
        {
            var person = new Person()
            {
                Firstname = "max",
                CreditCard = "1asdf2",
                Age = 10,
                Cars = new List<Car>()
                {
                    new Car() { Manufacturer = "Audi", Age = 100 }
                }
            };

            var validator = new PersonValidator();
            this.Result = validator.Validate(person);
        }

        [Test]
        public void CreditCard()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.CreditCard));
        }

        [Test]
        public void Email()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.Email));
        }

        [Test]
        public void Equal()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.Equal));
        }

        [Test]
        public void ExclusiveBetween()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.ExclusiveBetween));
        }

        [Test]
        public void GreaterThan()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.GreaterThan));
        }

        [Test]
        public void GreaterThanOrEqual()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.GreaterThanOrEqual));
        }

        [Test]
        public void InclusiveBetween()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.InclusiveBetween));
        }

        [Test]
        public void Length()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.Length));
        }

        [Test]
        public void LengthContainsPlaceholders()
        {
            Assert.IsTrue(Result.Errors.Where(f => f.ErrorCode == ValidationErrors.Length).Any(f => f.PlaceholderValues.ContainsKey("MinLength")));
        }

        [Test]
        public void LessThan()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.LessThan));
        }

        [Test]
        public void LessThanOrEqual()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.LessThanOrEqual));
        }

        [Test]
        public void NotEmpty()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.NotEmpty));
        }

        [Test]
        public void NotEqual()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.NotEqual));
        }

        [Test]
        public void NotNull()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.NotNull));
        }

        [Test]
        public void Predicate()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.Predicate));
        }

        [Test]
        public void RegularExpression()
        {
            Assert.IsTrue(Result.Errors.Any(f => f.ErrorCode == ValidationErrors.RegularExpression));
        }

        [Test]
        public void Custom()
        {
            Assert.AreEqual(1, Result.Errors.Where(f => f.ErrorCode == "ShouldNotBeEmpty").Count());
        }
    }
}
#endif
