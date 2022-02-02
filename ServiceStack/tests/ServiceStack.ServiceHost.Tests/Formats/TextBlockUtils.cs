namespace ServiceStack.ServiceHost.Tests.Formats
{
    public static class TextBlockUtils
    {
        public static string ReplaceForeach(this string tempalte, string replaceWith)
        {
            var startPos = tempalte.IndexOf("@foreach");
            var endPos = tempalte.IndexOf("}", startPos);

            var expected = tempalte.Substring(0, startPos)
                           + replaceWith
                           + tempalte.Substring(endPos + 1);

            return expected;
        }
    }
}