using System;
using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class NoRepeat : IReturn<NoRepeatResponse>
    {
        public Guid Id { get; set; }
    }

    public class NoRepeatResponse
    {
        public Guid Id { get; set; }
    }

    public class AutoBatchService : IService
    {
        private static readonly HashSet<Guid> ReceivedGuids = new HashSet<Guid>();

        public NoRepeatResponse Any(NoRepeat request)
        {
            if (ReceivedGuids.Contains(request.Id))
                throw new ArgumentException("Id {0} already received".Fmt(request.Id));

            ReceivedGuids.Add(request.Id);

            return new NoRepeatResponse
            {
                Id = request.Id
            };
        }
    }
}