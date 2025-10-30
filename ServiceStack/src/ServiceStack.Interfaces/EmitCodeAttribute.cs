#nullable enable

using System;

namespace ServiceStack;

[Flags]
public enum Lang
{
    CSharp =     1 << 0,
    FSharp =     1 << 1,
    Vb =         1 << 2,
    TypeScript = 1 << 3,
    Dart =       1 << 4,
    Swift =      1 << 5,
    Java =       1 << 6,
    Kotlin =     1 << 7,
    Python =     1 << 8,
    Go =         1 << 9,
    Php =        1 << 10,
}
    
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitCodeAttribute : AttributeBase
{
    public Lang Lang { get; set; }
    public string[] Statements { get; set; }
    public EmitCodeAttribute(Lang lang, string statement) : this(lang, [statement]) {}
    public EmitCodeAttribute(Lang lang, string[] statements)
    {
        Lang = lang;
        Statements = statements ?? throw new ArgumentNullException(nameof(Statements));
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitCSharp(params string[] statements) : EmitCodeAttribute(Lang.CSharp, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitFSharp(params string[] statements) : EmitCodeAttribute(Lang.FSharp, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitVb(params string[] statements) : EmitCodeAttribute(Lang.Vb, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitTypeScript(params string[] statements) : EmitCodeAttribute(Lang.TypeScript, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitDart(params string[] statements) : EmitCodeAttribute(Lang.Dart, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitSwift(params string[] statements) : EmitCodeAttribute(Lang.Swift, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitJava(params string[] statements) : EmitCodeAttribute(Lang.Java, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitKotlin(params string[] statements) : EmitCodeAttribute(Lang.Kotlin, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitPython(params string[] statements) : EmitCodeAttribute(Lang.Python, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitPhp(params string[] statements) : EmitCodeAttribute(Lang.Php, statements);[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitGo(params string[] statements) : EmitCodeAttribute(Lang.Go, statements);
