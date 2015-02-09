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

        public int Length
        {
            get { return sb.Length; }
        }
    }
}