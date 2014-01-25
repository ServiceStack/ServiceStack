using Funq;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    public class AnyTestMq
    {
        public int Id { get; set; }
    }

    public class AnyTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class PostTestMq
    {
        public int Id { get; set; }
    }

    public class PostTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class ValidateTestMq
    {
        public int Id { get; set; }
    }

    public class ValidateTestMqResponse
    {
        public int CorrelationId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ValidateTestMqValidator : AbstractValidator<ValidateTestMq>
    {
        public ValidateTestMqValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("PositiveIntegersOnly");
        }
    }

    public class TestMqService : IService
    {
        public object Any(AnyTestMq request)
        {
            return new AnyTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(PostTestMq request)
        {
            return new PostTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(ValidateTestMq request)
        {
            return new ValidateTestMqResponse { CorrelationId = request.Id };
        }
    }
}