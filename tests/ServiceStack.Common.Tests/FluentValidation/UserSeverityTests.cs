// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/. 

using ServiceStack.FluentValidation;

namespace ServiceStack.Common.Tests.FluentValidation
{
    namespace ServiceStack.FluentValidation.Tests
    {
        using System;
        using System.Linq;
        using NUnit.Framework;
        using ServiceStack.FluentValidation;

        public class UserSeverityTests
        {
            [Test]
            public void Stores_user_severity_against_validation_failure()
            {
                var validator = new TestValidator();
                validator.RuleFor(x => x.Lastname).NotNull().WithSeverity(Severity.Info);
                var result = validator.Validate(new ErrorCodeTests.Person());
                Assert.AreEqual(Severity.Info, result.Errors.Single().Severity);
            }

            [Test]
            public void Defaults_user_severity_to_error()
            {
                var validator = new TestValidator();
                validator.RuleFor(x => x.Lastname).NotNull();
                var result = validator.Validate(new ErrorCodeTests.Person());
                Assert.AreEqual(Severity.Error, result.Errors.Single().Severity);
            }

            public class TestValidator : AbstractValidator<ErrorCodeTests.Person>
            {
                public TestValidator()
                {
                }
            }
        }
    }
}