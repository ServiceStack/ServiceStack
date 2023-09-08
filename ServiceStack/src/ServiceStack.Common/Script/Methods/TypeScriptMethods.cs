using System.Collections.Generic;

namespace ServiceStack.Script;

public class TypeScriptPlugin : IScriptPlugin
{
    public void Register(ScriptContext context) => context.ScriptMethods.Add(new TypeScriptMethods());
}

public class TypeScriptMethods : ScriptMethods
{
    public RawString tsUnionStrings(IEnumerable<string> strings) => new(
        StringUtils.Join(" | ", strings.Map(x => $"'{x}'"), lineBreak:108, linePrefix:"        "));

    public RawString tsUnionTypes(IEnumerable<string> strings) => new(
        StringUtils.Join(" | ", strings, lineBreak:108, linePrefix:"        "));
}
