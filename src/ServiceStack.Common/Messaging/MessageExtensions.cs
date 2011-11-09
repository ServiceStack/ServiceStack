using ServiceStack.Text;

namespace ServiceStack.Messaging
{
	public static class MessageExtensions
	{
		public static string ToString(byte[] bytes)
		{
			return System.Text.Encoding.UTF8.GetString(bytes);
		}

		public static Message<T> ToMessage<T>(this byte[] bytes)
		{
			var messageText = ToString(bytes);
            return JsonSerializer.DeserializeFromString<Message<T>>(messageText);
		}

		public static byte[] ToBytes(this IMessage message)
		{
            var serializedMessage = JsonSerializer.SerializeToString((object)message);
			return System.Text.Encoding.UTF8.GetBytes(serializedMessage);
		}

		public static byte[] ToBytes<T>(this IMessage<T> message)
		{
			var serializedMessage = JsonSerializer.SerializeToString(message);
			return System.Text.Encoding.UTF8.GetBytes(serializedMessage);
		}

		public static string ToInQueueName<T>(this IMessage<T> message)
		{
			return message.Priority > 0
		       	? QueueNames<T>.Priority
		       	: QueueNames<T>.In;
		}
	}
}