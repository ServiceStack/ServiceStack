using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/factorial/sync/{ForNumber}")]
    [DataContract]
    public class GetFactorialSync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/async/{ForNumber}")]
    [DataContract]
    public class GetFactorialGenericAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/object/{ForNumber}")]
    [DataContract]
    public class GetFactorialObjectAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/await/{ForNumber}")]
    [DataContract]
    public class GetFactorialAwaitAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/delay/{ForNumber}")]
    [DataContract]
    public class GetFactorialDelayAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/newtask/{ForNumber}")]
    [DataContract]
    public class GetFactorialNewTaskAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/newtcs/{ForNumber}")]
    [DataContract]
    public class GetFactorialNewTcsAsync : IReturn<GetFactorialResponse>
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/factorial/unmarked/{ForNumber}")]
    [DataContract]
    public class GetFactorialUnmarkedAsync
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [Route("/voidasync")]
    [DataContract]
    public class VoidAsync : IReturnVoid
    {
        [DataMember]
        public string Message { get; set; }
    }

    public class GetFactorialAsyncService : IService
    {
        public object Any(GetFactorialSync request)
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        }

        public Task<GetFactorialResponse> Any(GetFactorialGenericAsync request)
        {
            return Task.Factory.StartNew(() =>
            {
                return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
            });
        }

        public object Any(GetFactorialObjectAsync request)
        {
            return Task.Factory.StartNew(() =>
            {
                return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
            });
        }

        public async Task<GetFactorialResponse> Any(GetFactorialAwaitAsync request)
        {
            return await Task.Factory.StartNew(() =>
            {
                return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
            });
        }

        public async Task<GetFactorialResponse> Any(GetFactorialDelayAsync request)
        {
            await Task.Delay(1000);

            return await Task.Factory.StartNew(() =>
            {
                return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
            });
        }

        public Task<GetFactorialResponse> Any(GetFactorialNewTaskAsync request)
        {
            return new Task<GetFactorialResponse>(() =>
                new GetFactorialResponse { Result = GetFactorial(request.ForNumber) });
        }

        public Task<GetFactorialResponse> Any(GetFactorialNewTcsAsync request)
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) }.AsTaskResult();
        }

        public async Task<GetFactorialResponse> Any(GetFactorialUnmarkedAsync request)
        {
            return await Task.Factory.StartNew(() =>
            {
                return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
            });
        }

        public async Task Any(VoidAsync request)
        {
            await Task.Delay(1);
        }

        public static long GetFactorial(long n)
        {
            return n > 1 ? n * GetFactorial(n - 1) : 1;
        }
    }
}