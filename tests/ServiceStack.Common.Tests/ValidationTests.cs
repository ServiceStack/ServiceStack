﻿#if !NETCORE_SUPPORT
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Testing;
using ServiceStack.Text;
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

    public class DtoA : IReturn<DtoAResponse>
    {
        public string FieldA { get; set; }
        public List<DtoB> Items { get; set; }
    }

    public class DtoAResponse
    {
        public string FieldA { get; set; }
        public List<DtoB> Items { get; set; }
    }

    public class DtoB
    {
        public string FieldB { get; set; }
    }

    internal interface IDtoBValidator : IValidator<DtoB> {}

    public class DtoAService : Service
    {
        public object Any(DtoA request)
        {
            return request.ConvertTo<DtoAResponse>();
        }
    }

    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void Can_register_IDtoBValidator_separately()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureAppHost = host => {
                    host.RegisterService<DtoAService>();
                    host.Plugins.Add(new ValidationFeature());
                },
                ConfigureContainer = c => {
                    c.RegisterAs<DtoBValidator, IDtoBValidator>();
                    c.RegisterValidators(typeof(DtoARequestValidator).Assembly);
                }
            }.Init())
            {
                var dtoAValidator = (DtoARequestValidator)appHost.TryResolve<IValidator<DtoA>>();
                Assert.That(dtoAValidator, Is.Not.Null);
                Assert.That(dtoAValidator.dtoBValidator, Is.Not.Null);
                Assert.That(appHost.TryResolve<IValidator<DtoB>>(), Is.Not.Null);
                Assert.That(appHost.TryResolve<IDtoBValidator>(), Is.Not.Null);

                var result = dtoAValidator.Validate(new DtoA());
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Count, Is.EqualTo(1));

                result = dtoAValidator.Validate(new DtoA { FieldA = "foo", Items = new[] { new DtoB() }.ToList() });
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Count, Is.EqualTo(1));

                result = dtoAValidator.Validate(new DtoA { FieldA = "foo", Items = new[] { new DtoB { FieldB = "bar" } }.ToList() });
                Assert.That(result.IsValid, Is.True);
            }
        }
    }
}
#endif
