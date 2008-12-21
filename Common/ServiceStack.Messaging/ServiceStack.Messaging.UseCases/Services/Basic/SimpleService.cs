using System;

namespace ServiceStack.Messaging.UseCases.Services.Basic
{
    public class SimpleService
    {
        public static string Reverse(string text)
        {
            char[] chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        public static string Greet(string text)
        {
            return string.Format("Hello, {0}!", text);
        }
    }
}