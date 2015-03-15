using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public class BatchThrows : IReturn<BatchThrowsResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BatchThrowsAsync : IReturn<BatchThrowsResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BatchThrowsResponse
    {
        public string Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class AutoBatchServices : IService
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

        public object Any(BatchThrows request)
        {
            throw new Exception("Batch Throws");
        }

        public async Task Any(BatchThrowsAsync request)
        {
            await Task.Delay(0);

            throw new Exception("Batch Throws");
        }
    }
}