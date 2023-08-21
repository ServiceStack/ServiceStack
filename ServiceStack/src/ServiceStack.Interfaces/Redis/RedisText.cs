using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Redis;

public class RedisText
{
    public string Text { get; set; }

    public List<RedisText> Children { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Text");
        if (Children is { Count: > 0 })
        {
            sb.Append('[');
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (i > 0)
                    sb.Append(',');
                sb.Append(child.Text ?? child.ToString());
            }
            sb.AppendLine("]");
        }
        return sb.ToString();
    }
}