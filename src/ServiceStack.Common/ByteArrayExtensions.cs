namespace ServiceStack.Common
{
    public static class ByteArrayExtensions
    {
        public static bool AreEqual(this byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;

            for (var i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }

            return true;
        }

    }
}