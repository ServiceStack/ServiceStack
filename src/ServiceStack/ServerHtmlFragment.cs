using System.Collections.Generic;
using System.IO;
using ServiceStack.Web;
using ServiceStack.Text;

#if NETSTANDARD1_6
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public abstract class ServerHtmlFragment
    {
    }

    public class ServerHtmlVariableFragment : ServerHtmlFragment
    {
        public StringSegment OriginalText { get; set; }
        public StringSegment Name { get; set; }
        public List<Command> FilterCommands { get; set; }

        public ServerHtmlVariableFragment(StringSegment originalText, StringSegment name, List<Command> filterCommands)
        {
            OriginalText = originalText;
            Name = name;
            FilterCommands = filterCommands;
        }
    }

    public class ServerHtmlStringFragment : ServerHtmlFragment
    {
        public StringSegment Value { get; set; }

        public ServerHtmlStringFragment(StringSegment value)
        {
            Value = value;
        }
    }
}