namespace ServiceStack.Common.Extensions
{
	public static class ByteArrayExtensions
	{
		public static bool AreEqual(this byte[] a1, byte[] a2)
		{
			if (a1.Length != a2.Length)
				return false;

			for (var i=0; i < a1.Length; i++)
				if (a1[i] != a2[i])
					return false;

			return true;
		}
	}
}