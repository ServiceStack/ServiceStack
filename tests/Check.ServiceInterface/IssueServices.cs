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

    [Route("/code", "post", Summary = @"Intellisense desteği sunar",
                    Notes = "")]
    public class AutoComplete : IReturn<StringListResponse>
    {
        public int objectId { get; set; }
        public int line { get; set; }
        public int col { get; set; }
        public string code { get; set; }
    }
    [Route("/code/object", "GET", Summary = @"Objenin adından objeyi döner",
                Notes = "")]
    public class ObjectId : IReturn<ObjectDesignResponse>
    {
        public string objectName { get; set; }
    }
    [Route("/code/execute", "get", Summary = @"Objeyi çalıştırır",
                Notes = "")]
    public class Execute : IReturn<StringListResponse>
    {
        public string objectName { get; set; }
        public string args { get; set; }
    }

    public class StringListResponse
    {
        public List<string> data { get; set; }
    }
    public class StringResponse
    {
        public string data { get; set; }
    }
    public class ObjectDesignResponse
    {
        public ObjectDesign data { get; set; }
    }

    public class ObjectDesign
    {
        public int Id { get; set; }
    }
    public class IntegerResponse
    {
        public int data { get; set; }
    }

    public class CodeServices : Service
    {
        public object Get(ObjectId request)
        {
            int data;
            int.TryParse(request.objectName ?? "-1", out data);
            return new IntegerResponse { data =  data };
        }
    }
}