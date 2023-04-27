using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script;

/// <summary>
/// Parse text contents into a list of string Key/Value pairs and assign to specified identifier
/// Usage: {{#keyvalues list}}
///          Apples  2
///          Oranges 3
///        {{/keyvalues}}
///        {{#keyvalues list ':'}}
///          Grape Fruit:  2
///          Rock Melon:   3
///        {{/keyvalues}}
/// </summary>
public class KeyValuesScriptBlock : ScriptBlock
{
    public override string Name => "keyvalues";
    public override ScriptLanguage Body => ScriptVerbatim.Language;
        
    public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken ct)
    {
        var literal = block.Argument.Span.ParseVarName(out var name);

        var delimiter = " ";
        literal = literal.AdvancePastWhitespace();
        if (literal.Length > 0)
        {
            literal = literal.ParseJsToken(out var token);
            if (!(token is JsLiteral litToken))
                throw new NotSupportedException($"#keyvalues expected string delimiter but was {token.DebugToken()}");
            delimiter = litToken.Value.ToString();
        }
            
        var strFragment = (PageStringFragment)block.Body[0];
        var strDict = Context.DefaultMethods.parseKeyValues(strFragment.ValueString, delimiter);
        scope.PageResult.Args[name.ToString()] = strDict;

        return TypeConstants.EmptyTask;
    }
}