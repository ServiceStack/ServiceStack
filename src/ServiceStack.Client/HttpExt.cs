namespace ServiceStack
{
    public static class HttpExt
    {
        public static bool HasNonAscii(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var c in s)
                {
                    if (c > 127)
                        return true;
                }
            }
            return false;
        }

        public static string GetDispositionFileName(string fileName) => !HasNonAscii(fileName)
            ? $"filename=\"{fileName}\""
            : $"filename=\"{fileName.UrlEncode()}\"; filename*=UTF-8''{fileName.UrlEncode()}";
    }
}