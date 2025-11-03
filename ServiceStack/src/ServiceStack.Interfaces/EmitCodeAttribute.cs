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
    JavaScript = 1 << 4,
    CommonJs =   1 << 5,
    Dart =       1 << 6,
    Swift =      1 << 7,
    Java =       1 << 8,
    Kotlin =     1 << 9,
    Python =     1 << 10,
    Go =         1 << 11,
    Php =        1 << 12,
    Ruby =       1 << 13,
    Rust =       1 << 14,
    Zig =        1 << 15,
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
public class EmitPhp(params string[] statements) : EmitCodeAttribute(Lang.Php, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitGo(params string[] statements) : EmitCodeAttribute(Lang.Go, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitRuby(params string[] statements) : EmitCodeAttribute(Lang.Ruby, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitRust(params string[] statements) : EmitCodeAttribute(Lang.Rust, statements);
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitZig(params string[] statements) : EmitCodeAttribute(Lang.Zig, statements);
