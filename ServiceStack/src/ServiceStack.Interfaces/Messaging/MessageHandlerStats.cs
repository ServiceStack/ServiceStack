using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IMessageHandlerStats
    {
        string Name { get; }
        int TotalMessagesProcessed { get; }
        int TotalMessagesFailed { get; }
        int TotalRetries { get; }
        int TotalNormalMessagesReceived { get; }
        int TotalPriorityMessagesReceived { get; }
        DateTime? LastMessageProcessed { get; }
        void Add(IMessageHandlerStats stats);
    }

    public class MessageHandlerStats : IMessageHandlerStats
    {
        public MessageHandlerStats(string name)
        {
            Name = name;
        }

        public MessageHandlerStats(string name, int totalMessagesProcessed, int totalMessagesFailed, int totalRetries,
            int totalNormalMessagesReceived, int totalPriorityMessagesReceived, DateTime? lastMessageProcessed)
        {
            Name = name;
            TotalMessagesProcessed = totalMessagesProcessed;
            TotalMessagesFailed = totalMessagesFailed;
            TotalRetries = totalRetries;
            TotalNormalMessagesReceived = totalNormalMessagesReceived;
            TotalPriorityMessagesReceived = totalPriorityMessagesReceived;
            LastMessageProcessed = lastMessageProcessed;
        }

        public string Name { get; private set; }
        public DateTime? LastMessageProcessed { get; private set; }
        public int TotalMessagesProcessed { get; private set; }
        public int TotalMessagesFailed { get; private set; }
        public int TotalRetries { get; private set; }
        public int TotalNormalMessagesReceived { get; private set; }
        public int TotalPriorityMessagesReceived { get; private set; }

        public virtual void Add(IMessageHandlerStats stats)
        {
            TotalMessagesProcessed += stats.TotalMessagesProcessed;
            TotalMessagesFailed += stats.TotalMessagesFailed;
            TotalRetries += stats.TotalRetries;
            TotalNormalMessagesReceived += stats.TotalNormalMessagesReceived;
            TotalPriorityMessagesReceived += stats.TotalPriorityMessagesReceived;
            if (LastMessageProcessed == null || stats.LastMessageProcessed > LastMessageProcessed)
                LastMessageProcessed = stats.LastMessageProcessed;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"STATS for {Name}:").AppendLine();            
            sb.AppendLine($"  TotalNormalMessagesReceived:    {TotalNormalMessagesReceived}");
            sb.AppendLine($"  TotalPriorityMessagesReceived:  {TotalPriorityMessagesReceived}");
            sb.AppendLine($"  TotalProcessed:                 {TotalMessagesProcessed}");
            sb.AppendLine($"  TotalRetries:                   {TotalRetries}");
            sb.AppendLine($"  TotalFailed:                    {TotalMessagesFailed}");
            sb.AppendLine($"  LastMessageProcessed:           {LastMessageProcessed?.ToString() ?? ""}");
            return sb.ToString();
        }
    }

    public static class MessageHandlerStatsExtensions
    {
        public static IMessageHandlerStats CombineStats(this IEnumerable<IMessageHandlerStats> stats)
        {
            IMessageHandlerStats to = null;

            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    if (to == null)
                        to = new MessageHandlerStats(stat.Name);

                    to.Add(stat);
                }
            }

            return to;
        }
    }
}