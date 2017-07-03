using System.Collections.Generic;

namespace ServiceStack
{
    public abstract class ServerHtmlFragment
    {
    }

    public class ServerHtmlVariableFragment : ServerHtmlFragment
    {
        public string Name { get; set; }
        public List<Command> FilterCommands { get; set; }

        public ServerHtmlVariableFragment(string name, List<Command> filterCommands)
        {
            Name = name;
            FilterCommands = filterCommands;
        }
    }

    public class ServerHtmlStringFragment : ServerHtmlFragment
    {
        public string Value { get; set; }

        public ServerHtmlStringFragment(string value)
        {
            Value = value;
        }
    }
}