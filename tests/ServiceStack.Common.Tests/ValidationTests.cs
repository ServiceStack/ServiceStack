using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests
{
    internal class DtoARequestValidator : AbstractValidator<DtoA>
    {
        internal readonly IDtoBValidator dtoBValidator;

        public DtoARequestValidator(IDtoBValidator dtoBValidator)
        {
            this.dtoBValidator = dtoBValidator;
            RuleFor(dto => dto.FieldA).NotEmpty();
            RuleFor(dto => dto.Items).SetCollectionValidator(dtoBValidator);
        }
    }

    internal class DtoBValidator : AbstractValidator<DtoB>, IDtoBValidator
    {
        public DtoBValidator()
        {
            RuleFor(dto => dto.FieldB).NotEmpty();
        }
    }

    public class DtoAResponse
    {
    }

    public class DtoA : IReturn<DtoAResponse>
    {
        public string FieldA { get; set; }
        public List<DtoB> Items { get; set; }
    }

    public class DtoB
    {
        public string FieldB { get; set; }
    }

    internal interface IDtoBValidator : IValidator<DtoB> {}

    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void Can_register_IDtoBValidator_separately()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = c => {
                    c.RegisterAs<DtoBValidator, IDtoBValidator>();
                    c.RegisterValidators(typeof(DtoARequestValidator).Assembly);
                }
            }.Init())
            {
                var c = appHost.Container;
                var dtoAValidator = (DtoARequestValidator)c.TryResolve<IValidator<DtoA>>();
                Assert.That(dtoAValidator, Is.Not.Null);
                Assert.That(dtoAValidator.dtoBValidator, Is.Not.Null);
                Assert.That(c.TryResolve<IValidator<DtoB>>(), Is.Not.Null);
                Assert.That(c.TryResolve<IDtoBValidator>(), Is.Not.Null);
            }
        }
    }
}