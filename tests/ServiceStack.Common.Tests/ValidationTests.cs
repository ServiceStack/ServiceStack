using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests
{
    internal class DtoARequestValidator : AbstractValidator<DtoA>
    {
        public IDtoBValidator DtoBValidator { get; set; }

        public DtoARequestValidator()
        {
            RuleFor(dto => dto.FieldA).NotEmpty();
            RuleFor(dto => dto.Items).SetCollectionValidator(new DtoBValidator());
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
        public void Does_ignore_registering_IDtoBValidator()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = c =>
                    c.RegisterValidators(typeof(DtoARequestValidator).Assembly)
            }.Init())
            {
                var c = appHost.Container;
                Assert.That(c.TryResolve<IValidator<DtoA>>(), Is.Not.Null);
                Assert.That(c.TryResolve<IValidator<DtoB>>(), Is.Not.Null);
                Assert.That(c.TryResolve<IDtoBValidator>(), Is.Null);
            }
        }

        [Test]
        public void Can_register_IDtoBValidator_separately()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureContainer = c => {
                    c.RegisterValidators(typeof(DtoARequestValidator).Assembly);
                    c.RegisterAs<DtoBValidator, IDtoBValidator>();
                }
            }.Init())
            {
                var c = appHost.Container;
                Assert.That(c.TryResolve<IValidator<DtoA>>(), Is.Not.Null);
                Assert.That(c.TryResolve<IValidator<DtoB>>(), Is.Not.Null);
                Assert.That(c.TryResolve<IDtoBValidator>(), Is.Not.Null);
            }
        }

    }
}