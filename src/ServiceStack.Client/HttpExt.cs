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

        public static string GetDispositionFileName(string fileName)
        {
            if (!HasNonAscii(fileName))
                return $"filename=\"{fileName}\"";

            var encodedFileName = ClientConfig.EncodeDispositionFileName(fileName);
            return $"filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}";
        }
    }
}