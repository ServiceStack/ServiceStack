using System;
using System.Collections.Generic;
using System.Net;
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
        int StatusCode { get; set; }
        bool IsValid(object dto, IRequest request = null);
        void ThrowIfNotValid(object dto, IRequest request = null);
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

    public class IsAuthenticatedValidator : TypeValidatorBase
    {
        public string Provider { get; }

        public IsAuthenticatedValidator()
            : base(nameof(HttpStatusCode.Unauthorized), ErrorMessages.NotAuthenticated, 401) {}
        
        public IsAuthenticatedValidator(string provider) : this() => Provider = provider;

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && AuthenticateAttribute.Authenticate(request, requestDto:dto, 
                authProviders:AuthenticateService.GetAuthProviders(this.Provider));
        }
    }

    public class HasRolesValidator : TypeValidatorBase
    {
        private readonly string[] roles;
        public HasRolesValidator(string role) 
            : this(new []{ role ?? throw new ArgumentNullException(nameof(role)) }) {}
        public HasRolesValidator(string[] roles)
            : base(nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidRole, 403)
        {
            this.roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && AuthenticateAttribute.Authenticate(request,requestDto:dto) 
                                   && RequiredRoleAttribute.HasRequiredRoles(request, roles);
        }
    }

    public class HasPermissionsValidator : TypeValidatorBase
    {
        private readonly string[] permissions;
        public HasPermissionsValidator(string permission) 
            : this(new []{ permission ?? throw new ArgumentNullException(nameof(permission)) }) {}
        public HasPermissionsValidator(string[] permissions)
            : base(nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidPermission, 403)
        {
            this.permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }

        public override bool IsValid(object dto, IRequest request = null)
        {
            return request != null && AuthenticateAttribute.Authenticate(request,requestDto:dto) 
                                   && RequiredPermissionAttribute.HasRequiredPermissions(request, permissions);
        }
    }

    public class ScriptValidator : TypeValidatorBase
    {
        public SharpPage Code { get; }
        public string Condition { get; }
        
        public ScriptValidator(SharpPage code, string condition)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override bool IsValid(object dto, IRequest request = null)
        {
            var pageResult = new PageResult(Code) {
                Args = {
                    [ScriptConstants.It] = dto,
                }
            };
            var ret = HostContext.AppHost.EvalScript(pageResult, request);
            return DefaultScripts.isTruthy(ret);
        }
    }
    
    public abstract class TypeValidatorBase : ITypeValidator
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }

        public string DefaultErrorCode { get; set; } = "InvalidRequest";
        public string DefaultMessage { get; set; } = "`The specified condition was not met for '${TypeName}'.`";

        protected TypeValidatorBase(string errorCode=null, string message=null, int statusCode = 400)
        {
            if (!string.IsNullOrEmpty(errorCode))
                DefaultErrorCode = errorCode;
            if (!string.IsNullOrEmpty(message))
                DefaultMessage = message;
            StatusCode = statusCode;
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
                errorMsg = (string) msgToken.Evaluate(JS.CreateScope(new Dictionary<string, object> {
                    [ScriptConstants.It] = dto,
                    [ScriptConstants.Request] = request,
                    ["TypeName"] = dto.GetType().Name,
                })) ?? Message ?? DefaultMessage;
            }

            return errorMsg;
        }

        protected int ResolveStatusCode()
        {
            var statusCode = StatusCode >= 400
                ? StatusCode
                : 400; //BadRequest
            return statusCode;
        }

        public abstract bool IsValid(object dto, IRequest request = null);

        public virtual void ThrowIfNotValid(object dto, IRequest request = null)
        {
            if (IsValid(dto, request))
                return;

            var errorMsg = ResolveErrorMessage(request, dto);
            throw new HttpError(ResolveStatusCode(), ErrorCode ?? DefaultErrorCode, errorMsg);
        }
    }
}