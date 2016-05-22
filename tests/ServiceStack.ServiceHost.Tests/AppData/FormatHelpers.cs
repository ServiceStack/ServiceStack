using System;

namespace ServiceStack.ServiceHost.Tests.AppData
{
    public class FormatHelpers
    {
        public static FormatHelpers Instance = new FormatHelpers();

        public string Money(decimal value)
        {
            return value.ToString("C");
        }

        public string ShortDate(DateTime? dateTime)
        {
            if (dateTime == null) return "";
            return String.Format("{0:dd/MM/yyyy}", dateTime);
        }
    }
}