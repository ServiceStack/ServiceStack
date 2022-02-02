using System.IO;
#if !NETCORE
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace ServiceStack.Redis.Support
{
	/// <summary>
	/// serialize/deserialize arbitrary objects
	/// (objects must be serializable)
	/// </summary>
	public class ObjectSerializer : ISerializer
	{
#if !NETCORE
		protected readonly BinaryFormatter bf = new BinaryFormatter();
#endif 


		/// <summary>
		///  Serialize object to buffer
		/// </summary>
		/// <param name="value">serializable object</param>
		/// <returns></returns>
		public virtual byte[] Serialize(object value)
		{
#if NETCORE
			return null;
#else
			if (value == null)
				return null;
			var memoryStream = new MemoryStream();
			memoryStream.Seek(0, 0);
			bf.Serialize(memoryStream, value);
			return memoryStream.ToArray();
#endif
		}

		/// <summary>
		///     Deserialize buffer to object
		/// </summary>
		/// <param name="someBytes">byte array to deserialize</param>
		/// <returns></returns>
		public virtual object Deserialize(byte[] someBytes)
		{
#if NETCORE
			return null;
#else
			if (someBytes == null)
				return null;
			var memoryStream = new MemoryStream();
			memoryStream.Write(someBytes, 0, someBytes.Length);
			memoryStream.Seek(0, 0);
			var de = bf.Deserialize(memoryStream);
			return de;
#endif
		}
	}
}