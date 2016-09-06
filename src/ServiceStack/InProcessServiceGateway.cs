using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;
using System;
using ServiceStack.FluentValidation;
using ServiceStack.Validation;

namespace ServiceStack
{
    public class InProcessServiceGateway : IServiceGateway, IServiceGatewayAsync
    {
        private readonly IRequest req;
        public IRequest Request => req;

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
            foreach (var filter in HostContext.AppHost.GatewayRequestFilters)
            {
                filter(req, request);
                if (req.Response.IsClosed)
                    return default(TResponse);
            }

            if (HostContext.HasPlugin<ValidationFeature>())
            {
                var validator = ValidatorCache.GetValidator(req, request.GetType());
                if (validator != null)
                {
                    var ruleSet = (string)(req.GetItem(Keywords.InvokeVerb) ?? req.Verb);
                    var result = validator.Validate(new ValidationContext(
                        request, null, new MultiRuleSetValidatorSelector(ruleSet)) {
                        Request = req
                    });
                    if (!result.IsValid)
                        throw new ValidationException(result.Errors);
                }
            }

            var response = HostContext.ServiceController.Execute(request, req);
            var responseTask = response as Task;
            if (responseTask != null)
                response = responseTask.GetResult();

            return ConvertToResponse<TResponse>(response);
        }

        private TResponse ConvertToResponse<TResponse>(object response)
        {
            var error = response as HttpError;
            if (error != null)
                throw error.ToWebServiceException();

            var responseDto = response.GetResponseDto();

            foreach (var filter in HostContext.AppHost.GatewayResponseFilters)
            {
                filter(req, responseDto);
                if (req.Response.IsClosed)
                    return default(TResponse);
            }

            return (TResponse) responseDto;
        }

        private Task<TResponse> ExecAsync<TResponse>(object request)
        {
            var responseTask = HostContext.ServiceController.ExecuteAsync(request, req, applyFilters:false);
            return responseTask.ContinueWith(task => ConvertToResponse<TResponse>(task.Result));
        }

        public TResponse Send<TResponse>(object requestDto)
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            var holdVerb = SetVerb(requestDto);

            req.RequestAttributes |= RequestAttributes.InProcess;
            try
            {
                return ExecSync<TResponse>(requestDto);
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            var holdVerb = SetVerb(requestDto);

            req.RequestAttributes |= RequestAttributes.InProcess;

            return ExecAsync<TResponse>(requestDto)
                .ContinueWith(task => {
                    req.Dto = holdDto;
                    req.RequestAttributes = holdAttrs;
                    ResetVerb(holdVerb);
                    return task.Result;
                }, token);
        }

        private static object[] CreateTypedArray(IEnumerable<object> requestDtos)
        {
            var requestsArray = requestDtos.ToArray();
            var elType = requestDtos.GetType().GetCollectionType();
            var toArray = (object[])Array.CreateInstance(elType, requestsArray.Length);
            for (int i = 0; i < requestsArray.Length; i++)
            {
                toArray[i] = requestsArray[i];
            }
            return toArray;
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos)
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes |= RequestAttributes.InProcess;

            try
            {
                return ExecSync<TResponse[]>(typedArray).ToList();
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes |= RequestAttributes.InProcess;

            return ExecAsync<TResponse[]>(typedArray)
                .ContinueWith(task => 
                {
                    req.Dto = holdDto;
                    req.RequestAttributes = holdAttrs;
                    ResetVerb(holdVerb);
                    return task.Result.ToList();
                }, token);
        }

        public void Publish(object requestDto)
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;
            req.RequestAttributes |= RequestAttributes.InProcess;

            try
            {
                var response = HostContext.ServiceController.Execute(requestDto, req);
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public Task PublishAsync(object requestDto, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;
            req.RequestAttributes |= RequestAttributes.InProcess;

            return HostContext.ServiceController.ExecuteAsync(requestDto, req, applyFilters: false)
                .ContinueWith(task => 
                {
                    req.Dto = holdDto;
                    req.RequestAttributes = holdAttrs;
                    ResetVerb(holdVerb);
                }, token);
        }

        public void PublishAll(IEnumerable<object> requestDtos)
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;
            req.RequestAttributes |= RequestAttributes.InProcess;

            try
            {
                var response = HostContext.ServiceController.Execute(typedArray, req);
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;

            return HostContext.ServiceController.ExecuteAsync(typedArray, req, applyFilters: false)
                .ContinueWith(task =>
                {
                    req.Dto = holdDto;
                    req.RequestAttributes = holdAttrs;
                    ResetVerb(holdVerb);
                }, token);
        }
    }
}