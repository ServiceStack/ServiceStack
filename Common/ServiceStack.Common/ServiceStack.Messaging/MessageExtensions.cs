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
			return TypeSerializer.DeserializeFromString<Message<T>>(ToString(bytes));
		}

		public static byte[] ToBytes<T>(this IMessage<T> message)
		{
			var serializedMessage = TypeSerializer.SerializeToString(message);
			return System.Text.Encoding.UTF8.GetBytes(serializedMessage);
		}
	}
}