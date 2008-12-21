using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Messaging.Tests.Services.Basic
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
