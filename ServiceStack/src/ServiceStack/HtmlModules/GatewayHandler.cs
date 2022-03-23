using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Invoke gateway response and return result as JSON, e.g:
/// /*gateway:window.ARG=MyRequest({arg:1})*/
/// </summary>
public class GatewayHandler : IHtmlModulesHandler
{
    public string Name { get; }
    public GatewayHandler(string name) => Name = name;

    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string args)
    {
        return ctx.Cache($"{Name}:{args}", _ =>
        {
            if (string.IsNullOrEmpty(args) || args.IndexOf('=') == -1)
                throw new ArgumentException(@"Usage: gateway:arg=RequestDto({...})", nameof(args));
        
            var varName = args.LeftPart('=');
            var request = args.RightPart('=');
            var requestName = request.LeftPart('(');
            var dtoArgsJs = request.IndexOf('(') >= 0
                ? X.Map(request.RightPart('('), x => x.Substring(0, x.Length - 1))
                : null;
            var requestType = ctx.AppHost.Metadata.GetOperationType(requestName)
                    ?? throw new ArgumentException($"Request DTO not found: {requestName}");

            var dtoArgs = !string.IsNullOrEmpty(dtoArgsJs) ? JSON.parse(dtoArgsJs) : null;
            var requestDto = ctx.AppHost.Metadata.CreateRequestDto(requestType, dtoArgs);
            var responseType = ctx.AppHost.Metadata.GetResponseTypeByRequest(requestType);
            var gateway = ctx.AppHost.GetServiceGateway(ctx.Request);

            var jsconfig = (dtoArgs as Dictionary<string, object>)?.TryGetValue(Keywords.JsConfig, out var oJsconfig) == true
                ? oJsconfig as string
                : null;

            using (jsconfig != null ? JsConfig.CreateScope(jsconfig) : null)
            {
                object response = null;
                try
                {
                    response = gateway.Send(responseType, requestDto);
                }
                catch (WebServiceException e)
                {
                    response = e.ResponseDto;
                }
                var sb = StringBuilderCache.Allocate();
                var value = HostContext.ContentTypes.SerializeToString(ctx.Request, response, MimeTypes.Json);
                sb.Append(varName).Append('=').Append(value).Append(';').AppendLine();
                return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
            }
        });
    }
}