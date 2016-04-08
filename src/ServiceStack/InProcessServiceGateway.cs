using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;
using System;

namespace ServiceStack
{
    public class InProcessServiceGateway : IServiceGateway
    {
        private readonly IRequest req;

        public InProcessServiceGateway(IRequest req)
        {
            this.req = req;
        }

        private string SetVerb(object reqeustDto)
        {
            var hold = req.GetItem(Keywords.InvokeVerb) as string;
            if (reqeustDto is IVerb)
            {
                if (reqeustDto is IGet)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Get);
                if (reqeustDto is IPost)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
                if (reqeustDto is IPut)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Put);
                if (reqeustDto is IDelete)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Delete);
                if (reqeustDto is IPatch)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Patch);
                if (reqeustDto is IOptions)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Options);
            }
            return hold;
        }

        private void ResetVerb(string verb)
        {
            if (verb == null)
                req.Items.Remove(Keywords.InvokeVerb);
            else
                req.SetItem(Keywords.InvokeVerb, verb);
        }

        private TResponse ExecSync<TResponse>(object request)
        {
            var response = HostContext.ServiceController.Execute(request, req);
            var responseDto = response.GetResponseDto();
            return (TResponse)responseDto;
        }

        public TResponse Send<TResponse>(object requestDto)
        {
            var holdDto = req.Dto;
            var holdVerb = SetVerb(requestDto);
            try
            {
                return ExecSync<TResponse>(requestDto);
            }
            finally
            {
                req.Dto = holdDto;
                ResetVerb(holdVerb);
            }
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos)
        {
            var holdDto = req.Dto;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;
            try
            {
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);

                var requestsArray = requestDtos.ToArray();
                var elType = requestDtos.GetType().GetCollectionType();
                var toArray = (object[])Array.CreateInstance(elType, requestsArray.Length);
                for (int i = 0; i < requestsArray.Length; i++)
                {
                    toArray[i] = requestsArray[i];
                }

                return ExecSync<TResponse[]>(toArray).ToList();
            }
            finally
            {
                req.Dto = holdDto;
                ResetVerb(holdVerb);
            }
        }

        public void Publish(object requestDto)
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;
            try
            {
                req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
                req.RequestAttributes &= ~RequestAttributes.Reply;
                req.RequestAttributes |= RequestAttributes.OneWay;

                HostContext.ServiceController.Execute(requestDto, req);
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }
    }
}