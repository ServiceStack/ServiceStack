using System;
using System.Text;

namespace ServiceStack.NativeTypes
{
    public class StringBuilderWrapper
    {
        private StringBuilder sb;
        const int indentSize = 4;
        private int indent;
        private string tab;

        public StringBuilderWrapper(StringBuilder sb, int indent = 0)
        {
            this.sb = sb;
            this.indent = indent;

            tab = "".PadLeft(indent * indentSize, ' ');
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
            return new StringBuilderWrapper(sb, indent - 1);
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        public void Chop(char c)
        {
            var endsWithNewLine = sb.Length > 0 && sb[sb.Length - 1] == '\n';
            do
            {
                if (sb.Length == 0) return;
                sb.Length--;
            } while (sb[sb.Length - 1] != c);
            sb.Length--; //TODO why is this needed
            if (endsWithNewLine)
            {
                sb.AppendLine();
            }
        }

        public int Length => sb.Length;
    }
}