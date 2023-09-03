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
    public EmitCodeAttribute(Lang lang, string statement) : this(lang, new[] {statement}) {}
    public EmitCodeAttribute(Lang lang, string[] statements)
    {
        Lang = lang;
        Statements = statements ?? throw new ArgumentNullException(nameof(Statements));
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitCSharp : EmitCodeAttribute
{
    public EmitCSharp(params string[] statements) : base(Lang.CSharp, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitFSharp : EmitCodeAttribute
{
    public EmitFSharp(params string[] statements) : base(Lang.FSharp, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitVb : EmitCodeAttribute
{
    public EmitVb(params string[] statements) : base(Lang.Vb, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitTypeScript : EmitCodeAttribute
{
    public EmitTypeScript(params string[] statements) : base(Lang.TypeScript, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitDart : EmitCodeAttribute
{
    public EmitDart(params string[] statements) : base(Lang.Dart, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitSwift : EmitCodeAttribute
{
    public EmitSwift(params string[] statements) : base(Lang.Swift, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitJava : EmitCodeAttribute
{
    public EmitJava(params string[] statements) : base(Lang.Java, statements) { }
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class EmitKotlin : EmitCodeAttribute
{
    public EmitKotlin(params string[] statements) : base(Lang.Kotlin, statements) { }
}