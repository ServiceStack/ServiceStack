using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack
{
    public interface ITypeValidator 
    {
        string ErrorCode { get; set; }
        string Message { get; set; }
        int? StatusCode { get; set; }
        Task<bool> IsValidAsync(object dto, IRequest request = null);
        Task ThrowIfNotValidAsync(object dto, IRequest request = null);
    }

    public interface IHasTypeValidators
    {
        List<ITypeValidator> TypeValidators { get; }
    }

    public static class TypeValidatorUtils
    {
        public static ITypeValidator Init(this ITypeValidator validator, IValidateRule rule)
        {
            if (rule.ErrorCode != null)
                validator.ErrorCode = rule.ErrorCode;
            if (rule.Message != null)
                validator.Message = rule.Message;
            if (rule is ValidateRequestAttribute attr)
            {
                if (attr.StatusCode != default)
                    validator.StatusCode = attr.StatusCode;
            }
            return validator;
        }
    }

    public class IsAuthenticatedValidator : TypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = ErrorMessages.NotAuthenticated;
        public static IsAuthenticatedValidator Instance { get; } = new IsAuthenticatedValidator();
        public string Provider { get; }

        public IsAuthenticatedValidator()
            : base(nameof(HttpStatusCode.Unauthorized), DefaultErrorMessage, 401) {}
        
        public IsAuthenticatedValidator(string provider) : this() => Provider = provider;

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && AuthenticateAttribute.Authenticate(request, requestDto:dto, 
                authProviders:AuthenticateService.GetAuthProviders(this.Provider));
        }
    }

    public class HasRolesValidator : TypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = "`${roles.join(', ')} Role${roles.length > 1 ? 's' : ''} Required`";
        
        private readonly string[] roles;
        public HasRolesValidator(string role) 
            : this(new []{ role ?? throw new ArgumentNullException(nameof(role)) }) {}
        public HasRolesValidator(string[] roles)
            : base(nameof(HttpStatusCode.Forbidden), DefaultErrorMessage, 403)
        {
            this.roles = roles ?? throw new ArgumentNullException(nameof(roles));
            this.ContextArgs = new Dictionary<string, object> {
                [nameof(roles)] = roles
            };
        }

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && IsAuthenticatedValidator.Instance.IsValid(dto, request) 
                                   && RequiredRoleAttribute.HasRequiredRoles(request, roles);
        }

        public override async Task ThrowIfNotValidAsync(object dto, IRequest request = null)
        {
            await IsAuthenticatedValidator.Instance.ThrowIfNotValidAsync(dto, request);
            
            if (RequiredRoleAttribute.HasRequiredRoles(request, roles))
                return;

            throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        }
    }

    public class HasPermissionsValidator : TypeValidator
    {
        public static string DefaultErrorMessage { get; set; } = "`${permissions.join(', ')} Permission${permissions.length > 1 ? 's' : ''} Required`";

        private readonly string[] permissions;
        public HasPermissionsValidator(string permission) 
            : this(new []{ permission ?? throw new ArgumentNullException(nameof(permission)) }) {}
        public HasPermissionsValidator(string[] permissions)
            : base(nameof(HttpStatusCode.Forbidden), DefaultErrorMessage, 403)
        {
            this.permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            this.ContextArgs = new Dictionary<string, object> {
                [nameof(permissions)] = permissions
            };
        }

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && IsAuthenticatedValidator.Instance.IsValid(dto, request) 
                                   && RequiredPermissionAttribute.HasRequiredPermissions(request, permissions);
        }

        public override async Task ThrowIfNotValidAsync(object dto, IRequest request = null)
        {
            await IsAuthenticatedValidator.Instance.ThrowIfNotValidAsync(dto, request);
            
            if (RequiredPermissionAttribute.HasRequiredPermissions(request, permissions))
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

        public override async Task<bool> IsValidAsync(object dto, IRequest request = null)
        {
            var pageResult = new PageResult(Code) {
                Args = {
                    [ScriptConstants.It] = dto,
                }
            };
            var ret = await HostContext.AppHost.EvalScriptAsync(pageResult, request);
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
                ? appHost.ResolveLocalizedString(Message)
                : Validators.ErrorCodeMessages.TryGetValue(errorCode, out var msg)
                    ? appHost.ResolveLocalizedString(msg)
                    : appHost.ResolveLocalizedString(DefaultMessage ?? errorCode.SplitPascalCase());

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

        public virtual bool IsValid(object dto, IRequest request = null) =>
            throw new NotImplementedException();

        public virtual Task<bool> IsValidAsync(object dto, IRequest request = null) 
            => Task.FromResult(IsValid(dto, request)); 

        public virtual async Task ThrowIfNotValidAsync(object dto, IRequest request = null)
        {
            if (await IsValidAsync(dto, request))
                return;

            throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        }
    }
}