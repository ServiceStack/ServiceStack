using System;
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
            var sb = new StringBuilder("Stats for " + Name);
            sb.AppendLine("\n---------------");
            sb.AppendFormat("\nTotalNormalMessagesReceived: {0}", TotalNormalMessagesReceived);
            sb.AppendFormat("\nTotalPriorityMessagesReceived: {0}", TotalPriorityMessagesReceived);
            sb.AppendFormat("\nTotalProcessed: {0}", TotalMessagesProcessed);
            sb.AppendFormat("\nTotalRetries: {0}", TotalRetries);
            sb.AppendFormat("\nTotalFailed: {0}", TotalMessagesFailed);
            sb.AppendFormat("\nLastMessageProcessed: {0}", LastMessageProcessed.HasValue ? LastMessageProcessed.Value.ToString() : "");
            return sb.ToString();
        }
    }
}