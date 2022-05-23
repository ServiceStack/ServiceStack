using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public interface ITypeValidator 
    {
        string ErrorCode { get; set; }
        string Message { get; set; }
        int? StatusCode { get; set; }
        Task<bool> IsValidAsync(object dto, IRequest request);
        Task ThrowIfNotValidAsync(object dto, IRequest request);
    }

    public interface IHasTypeValidators
    {
        List<ITypeValidator> TypeValidators { get; }
    }
    
    public interface IAuthTypeValidator {}

    public class IsAuthenticatedValidator : TypeValidator, IAuthTypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = ErrorMessages.NotAuthenticated;
        public static IsAuthenticatedValidator Instance { get; } = new();
        public string Provider { get; }

        public IsAuthenticatedValidator()
            : base(nameof(HttpStatusCode.Unauthorized), DefaultErrorMessage, 401) {}
        
        public IsAuthenticatedValidator(string provider) : this() => Provider = provider;

        public override async Task<bool> IsValidAsync(object dto, IRequest request)
        {
            return request != null && await AuthenticateAttribute.AuthenticateAsync(request, requestDto:dto, 
                authProviders:AuthenticateService.GetAuthProviders(this.Provider)).ConfigAwait();
        }
    }

    public class HasRolesValidator : TypeValidator, IAuthTypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = "`${Roles.join(', ')} Role${Roles.length > 1 ? 's' : ''} Required`";

        public string[] Roles { get; }
        public HasRolesValidator(string role) 
            : this(new []{ role ?? throw new ArgumentNullException(nameof(role)) }) {}
        public HasRolesValidator(string[] roles)
            : base(nameof(HttpStatusCode.Forbidden), DefaultErrorMessage, 403)
        {
            this.Roles = roles ?? throw new ArgumentNullException(nameof(roles));
            this.ContextArgs = new Dictionary<string, object> {
                [nameof(Roles)] = roles
            };
        }

        public override async Task<bool> IsValidAsync(object dto, IRequest request)
        {
            return request != null && await IsAuthenticatedValidator.Instance.IsValidAsync(dto, request).ConfigAwait() 
                                   && await RequiredRoleAttribute.HasRequiredRolesAsync(request, Roles).ConfigAwait();
        }

        public override async Task ThrowIfNotValidAsync(object dto, IRequest request)
        {
            await IsAuthenticatedValidator.Instance.ThrowIfNotValidAsync(dto, request).ConfigAwait();
            
            if (await RequiredRoleAttribute.HasRequiredRolesAsync(request, Roles).ConfigAwait())
                return;

            throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        }
    }

    public class HasPermissionsValidator : TypeValidator, IAuthTypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = "`${Permissions.join(', ')} Permission${Permissions.length > 1 ? 's' : ''} Required`";

        public string[] Permissions { get; }
        public HasPermissionsValidator(string permission) 
            : this(new []{ permission ?? throw new ArgumentNullException(nameof(permission)) }) {}
        public HasPermissionsValidator(string[] permissions)
            : base(nameof(HttpStatusCode.Forbidden), DefaultErrorMessage, 403)
        {
            this.Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            this.ContextArgs = new Dictionary<string, object> {
                [nameof(Permissions)] = Permissions
            };
        }

        public override async Task<bool> IsValidAsync(object dto, IRequest request)
        {
            return request != null && await IsAuthenticatedValidator.Instance.IsValidAsync(dto, request).ConfigAwait() 
                                   && await RequiredPermissionAttribute.HasRequiredPermissionsAsync(request, Permissions).ConfigAwait();
        }

        public override async Task ThrowIfNotValidAsync(object dto, IRequest request)
        {
            await IsAuthenticatedValidator.Instance.ThrowIfNotValidAsync(dto, request).ConfigAwait();
            
            if (await RequiredPermissionAttribute.HasRequiredPermissionsAsync(request, Permissions).ConfigAwait())
                return;

            throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        }
    }

    public class ScriptValidator : TypeValidator
    {
        public SharpPage Code { get; }
        public string Condition { get; }
        
        public ScriptValidator(SharpPage code, string condition)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override async Task<bool> IsValidAsync(object dto, IRequest request)
        {
            var pageResult = new PageResult(Code) {
                Args = {
                    [ScriptConstants.It] = dto,
                }
            };
            var ret = await HostContext.AppHost.EvalScriptAsync(pageResult, request).ConfigAwait();
            return DefaultScripts.isTruthy(ret);
        }
    }
    
    public abstract class TypeValidator : ITypeValidator
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public int? StatusCode { get; set; }

        public string DefaultErrorCode { get; set; } = "InvalidRequest";
        public string DefaultMessage { get; set; } = "`The specified condition was not met for '${TypeName}'.`";
        public int? DefaultStatusCode { get; set; }
        
        public Dictionary<string,object> ContextArgs { get; set; }

        protected TypeValidator(string errorCode=null, string message=null, int? statusCode = null)
        {
            if (!string.IsNullOrEmpty(errorCode))
                DefaultErrorCode = errorCode;
            if (!string.IsNullOrEmpty(message))
                DefaultMessage = message;
            if (statusCode != null)
                DefaultStatusCode = statusCode;
        }

        protected string ResolveErrorMessage(IRequest request, object dto)
        {
            var appHost = HostContext.AppHost;
            var errorCode = ErrorCode ?? DefaultErrorCode;
            var messageExpr = Message != null
                ? Message.Localize(request)
                : Validators.ErrorCodeMessages.TryGetValue(errorCode, out var msg)
                    ? msg.Localize(request)
                    : (DefaultMessage ?? errorCode.SplitPascalCase()).Localize(request);

            string errorMsg = messageExpr;
            if (messageExpr.IndexOf('`') >= 0)
            {
                var msgToken = JS.expressionCached(appHost.ScriptContext, messageExpr);
                var args = new Dictionary<string, object> {
                    [ScriptConstants.It] = dto,
                    [ScriptConstants.Request] = request,
                    ["TypeName"] = dto.GetType().Name,
                };
                if (ContextArgs != null)
                {
                    foreach (var entry in ContextArgs)
                    {
                        args[entry.Key] = entry.Value;
                    }
                }
                errorMsg = (string) msgToken.Evaluate(JS.CreateScope(args)) ?? Message ?? DefaultMessage;
            }

            return errorMsg;
        }

        protected int ResolveStatusCode()
        {
            var statusCode = StatusCode >= 400
                ? StatusCode
                : DefaultStatusCode;
            return statusCode ?? 400; //BadRequest;
        }

        protected string ResolveErrorCode() => ErrorCode ?? DefaultErrorCode;

        public virtual bool IsValid(object dto, IRequest request) =>
            throw new NotImplementedException();

        public virtual Task<bool> IsValidAsync(object dto, IRequest request) 
            => Task.FromResult(IsValid(dto, request)); 

        public virtual async Task ThrowIfNotValidAsync(object dto, IRequest request)
        {
            if (await IsValidAsync(dto, request).ConfigAwait())
                return;

            throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        }
    }
}