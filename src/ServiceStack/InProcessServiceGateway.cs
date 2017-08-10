using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;
using System;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
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

            ExecValidators(request).Wait();

            var response = HostContext.ServiceController.Execute(request, req);
            var responseTask = response as Task;
            if (responseTask != null)
                response = responseTask.GetResult();

            return ConvertToResponse<TResponse>(response);
        }

        private async Task<TResponse> ExecAsync<TResponse>(object request)
        {
            foreach (var filter in HostContext.AppHost.GatewayRequestFilters)
            {
                filter(req, request);
                if (req.Response.IsClosed)
                    return default(TResponse);
            }
            
            await ExecValidators(request);

            var response = await HostContext.ServiceController.ExecuteAsync(request, req, applyFilters: false);
            var responseTask = response as Task;
            if (responseTask != null)
                response = responseTask.GetResult();

            return ConvertToResponse<TResponse>(response);
        }

        private async Task ExecValidators(object request)
        {
            var feature = HostContext.GetPlugin<ValidationFeature>();
            if (feature != null)
            {
                var validator = ValidatorCache.GetValidator(req, request.GetType());
                if (validator != null)
                {
                    var ruleSet = (string) (req.GetItem(Keywords.InvokeVerb) ?? req.Verb);
                    var validationContext = new ValidationContext(request, null, new MultiRuleSetValidatorSelector(ruleSet))
                    {
                        Request = req
                    };
                    
                    ValidationResult result;
                    if (!validator.HasAsyncValidators())
                    {
                        result = validator.Validate(validationContext);
                    }
                    else
                    {
                        result = await validator.ValidateAsync(validationContext);
                    }
                    
                    if (!result.IsValid)
                        throw result.ToWebServiceException(request, feature);
                }
            }
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

        public TResponse Send<TResponse>(object requestDto)
        {
            var holdDto = req.Dto;
            var holdOp = req.OperationName;
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
                req.OperationName = holdOp;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public async Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdOp = req.OperationName;
            var holdAttrs = req.RequestAttributes;
            var holdVerb = SetVerb(requestDto);

            req.RequestAttributes |= RequestAttributes.InProcess;

            try
            {
                var response = await ExecAsync<TResponse>(requestDto);
                return response;
            }
            finally
            {
                req.Dto = holdDto;
                req.OperationName = holdOp;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
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

            var responseTask = ExecAsync<TResponse[]>(typedArray);
            return HostContext.Async.ContinueWith(req, responseTask, task => 
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
            var holdOp = req.OperationName;
            var holdAttrs = req.RequestAttributes;
            var holdVerb = SetVerb(requestDto);

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
                req.OperationName = holdOp;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public Task PublishAsync(object requestDto, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdOp = req.OperationName;
            var holdAttrs = req.RequestAttributes;
            var holdVerb = SetVerb(requestDto);
            
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;
            req.RequestAttributes |= RequestAttributes.InProcess;

            var responseTask = HostContext.ServiceController.ExecuteAsync(requestDto, req, applyFilters: false);
            return HostContext.Async.ContinueWith(req, responseTask, task => 
                {
                    req.Dto = holdDto;
                    req.OperationName = holdOp;
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

            var responseTask = HostContext.ServiceController.ExecuteAsync(typedArray, req, applyFilters: false);
            return HostContext.Async.ContinueWith(req, responseTask, task =>
                {
                    req.Dto = holdDto;
                    req.RequestAttributes = holdAttrs;
                    ResetVerb(holdVerb);
                }, token);
        }
    }
}