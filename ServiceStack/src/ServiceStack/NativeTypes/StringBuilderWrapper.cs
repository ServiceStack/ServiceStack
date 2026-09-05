using System;
using System.Text;

namespace ServiceStack.NativeTypes;

public class StringBuilderWrapper
{
    private StringBuilder sb;
    const int indentSize = 4;
    private int indent;
    private string tab;

    public StringBuilderWrapper(StringBuilder sb, int indent = 0)
    {
        this.sb = sb ?? new StringBuilder();
        this.indent = Math.Max(0, indent);

        tab = "".PadLeft(this.indent * indentSize, ' ');
    }

    public void AppendLine(string str = null)
    {
        if (str == null)
        {
            sb.AppendLine();
            return;
        }

        sb.Append(tab);
        sb.AppendLine(str);
    }

    public StringBuilderWrapper Indent()
    {
        return new StringBuilderWrapper(sb, indent + 1);
    }

    public StringBuilderWrapper UnIndent()
    {
        return new StringBuilderWrapper(sb, Math.Max(0, indent - 1));
    }

    public override string ToString()
    {
        return sb.ToString();
    }

    public void Chop(char c)
    {
        if (sb.Length == 0) return;
        var i = sb.Length - 1;
        while (i >= 0 && (sb[i] == '\r' || sb[i] == '\n' || sb[i] == ' ' || sb[i] == '\t'))
        {
            i--;
        }
        if (i >= 0 && sb[i] == c)
        {
            sb.Remove(i, 1);
        }
    }

    public int Length => sb.Length;
}