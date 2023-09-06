using System.Collections.Generic;

namespace ServiceStack.Script;

public class TypeScriptPlugin : IScriptPlugin
{
    public void Register(ScriptContext context) => context.ScriptMethods.Add(new TypeScriptMethods());
}

public class TypeScriptMethods : ScriptMethods
{
    public IRawString tsUnionStrings(IEnumerable<string> strings) => 
        new RawString(string.Join(" | ", strings.Map(x => $"'{x}'")));

    public IRawString tsUnionTypes(IEnumerable<string> strings) => 
        new RawString(string.Join(" | ", strings));
}
