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

        private string SetVerb(object requestDto)
        {
            var hold = req.GetItem(Keywords.InvokeVerb) as string;
            if (requestDto is IVerb)
            {
                if (requestDto is IGet)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Get);
                if (requestDto is IPost)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
                if (requestDto is IPut)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Put);
                if (requestDto is IDelete)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Delete);
                if (requestDto is IPatch)
                    req.SetItem(Keywords.InvokeVerb, HttpMethods.Patch);
                if (requestDto is IOptions)
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
            foreach (var filter in HostContext.AppHost.GatewayRequestFiltersArray)
            {
                filter(req, request);
                if (req.Response.IsClosed)
                    return default;
            }
            foreach (var filter in HostContext.AppHost.GatewayRequestFiltersAsyncArray)
            {
                filter(req, request).Wait();
                if (req.Response.IsClosed)
                    return default;
            }

            ExecValidators(request).Wait();

            var response = HostContext.ServiceController.Execute(request, req);
            if (response is Task responseTask)
                response = responseTask.GetResult();
            else if (response is ValueTask<object> valueTaskResponse)
            {
                response = valueTaskResponse.GetAwaiter().GetResult();
            }
            else if (response is ValueTask valueTaskVoid)
            {
                valueTaskVoid.GetAwaiter().GetResult();
                response = null;
            }

            if (response is Task[] batchResponseTasks)
            {
                Task.WaitAll(batchResponseTasks);
                var to = new object[batchResponseTasks.Length];
                for (int i = 0; i < batchResponseTasks.Length; i++)
                {
                    to[i] = batchResponseTasks[i].GetResult();
                }
                response = to.ConvertTo<TResponse>();
            }

            var responseDto = ConvertToResponse<TResponse>(response);

            foreach (var filter in HostContext.AppHost.GatewayResponseFiltersArray)
            {
                filter(req, responseDto);
                if (req.Response.IsClosed)
                    return default;
            }
            foreach (var filter in HostContext.AppHost.GatewayResponseFiltersAsyncArray)
            {
                filter(req, responseDto).Wait();
                if (req.Response.IsClosed)
                    return default;
            }

            return responseDto;
        }

        private async Task<TResponse> ExecAsync<TResponse>(object request)
        {
            foreach (var filter in HostContext.AppHost.GatewayRequestFiltersArray)
            {
                filter(req, request);
                if (req.Response.IsClosed)
                    return default;
            }
            foreach (var filter in HostContext.AppHost.GatewayRequestFiltersAsyncArray)
            {
                await filter(req, request);
                if (req.Response.IsClosed)
                    return default;
            }

            await ExecValidators(request);

            var response = await HostContext.ServiceController.GatewayExecuteAsync(request, req, applyFilters: false);

            var responseDto = ConvertToResponse<TResponse>(response);

            foreach (var filter in HostContext.AppHost.GatewayResponseFiltersArray)
            {
                filter(req, responseDto);
                if (req.Response.IsClosed)
                    return default;
            }
            foreach (var filter in HostContext.AppHost.GatewayResponseFiltersAsyncArray)
            {
                await filter(req, responseDto);
                if (req.Response.IsClosed)
                    return default;
            }

            return responseDto;
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
                    var validationContext = new ValidationContext<object>(request, null, new MultiRuleSetValidatorSelector(ruleSet))
                    {
                        Request = req
                    };
                    
                    ValidationResult result;
                    if (!validator.HasAsyncValidators(validationContext))
                    {
                        // ReSharper disable once MethodHasAsyncOverload
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
            if (response is HttpError error)
                throw error.ToWebServiceException();

            var responseDto = response.GetResponseDto();

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
            catch (AggregateException ae)
            {
                var ex = ae.UnwrapIfSingleException();
                HostContext.RaiseGatewayException(req, requestDto, ex).Wait();
                throw ex;
            }
            catch (Exception ex)
            {
                HostContext.RaiseGatewayException(req, requestDto, ex).Wait();
                throw;
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
            var holdVerb = SetVerb(requestDto);
            var holdAttrs = req.RequestAttributes;

            req.SetInProcessRequest();

            try
            {
                var response = await ExecAsync<TResponse>(requestDto);
                return response;
            }
            catch (Exception ex)
            {
                await HostContext.RaiseGatewayException(req, requestDto, ex);
                throw;
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
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;
            var holdAttrs = req.RequestAttributes;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.SetInProcessRequest();

            try
            {
                return ExecSync<TResponse[]>(typedArray).ToList();
            }
            catch (AggregateException ae)
            {
                var ex = ae.UnwrapIfSingleException();
                HostContext.RaiseGatewayException(req, requestDtos, ex).Wait();
                throw ex;
            }
            catch (Exception ex)
            {
                HostContext.RaiseGatewayException(req, requestDtos, ex).Wait();
                throw;
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public async Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.SetInProcessRequest();

            try
            {
                var response = await ExecAsync<TResponse[]>(typedArray);
                return response.ToList();
            }
            catch (Exception ex)
            {
                await HostContext.RaiseGatewayException(req, requestDtos, ex);
                throw;
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
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
            catch (Exception ex)
            {
                HostContext.RaiseGatewayException(req, requestDto, ex).Wait();
                throw;
            }
            finally
            {
                req.Dto = holdDto;
                req.OperationName = holdOp;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public async Task PublishAsync(object requestDto, CancellationToken token = new CancellationToken())
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
                await HostContext.ServiceController.GatewayExecuteAsync(requestDto, req, applyFilters: false);
            }
            catch (Exception ex)
            {
                await HostContext.RaiseGatewayException(req, requestDto, ex);
                throw;
            }
            finally
            {
                req.Dto = holdDto;
                req.OperationName = holdOp;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
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
            catch (Exception ex)
            {
                HostContext.RaiseGatewayException(req, requestDtos, ex).Wait();
                throw;
            }
            finally
            {
                req.Dto = holdDto;
                req.RequestAttributes = holdAttrs;
                ResetVerb(holdVerb);
            }
        }

        public async Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            var holdDto = req.Dto;
            var holdAttrs = req.RequestAttributes;
            string holdVerb = req.GetItem(Keywords.InvokeVerb) as string;

            var typedArray = CreateTypedArray(requestDtos);
            req.SetItem(Keywords.InvokeVerb, HttpMethods.Post);
            req.RequestAttributes &= ~RequestAttributes.Reply;
            req.RequestAttributes |= RequestAttributes.OneWay;

            try
            {
                await HostContext.ServiceController.GatewayExecuteAsync(typedArray, req, applyFilters: false);
            }
            catch (Exception ex)
            {
                await HostContext.RaiseGatewayException(req, requestDtos, ex);
                throw;
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