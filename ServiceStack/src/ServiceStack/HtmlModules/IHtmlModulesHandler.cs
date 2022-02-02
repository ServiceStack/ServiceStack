#nullable enable
using System;

namespace ServiceStack.HtmlModules;

public interface IHtmlModulesHandler
{
    string Name { get; }
    ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string args);
}