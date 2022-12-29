using System;
using System.Globalization;
using System.Text;

namespace ServiceStack.Text.Support
{
    public class TimeSpanConverter
    {
        private const string MinSerializedValue = "-P10675199DT2H48M5.4775391S";
        private const string MaxSerializedValue = "P10675199DT2H48M5.4775391S";
        
        public static string ToXsdDuration(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.MinValue)
                return MinSerializedValue;
            if (timeSpan == TimeSpan.MaxValue)
                return MaxSerializedValue;
            
            var sb = StringBuilderThreadStatic.Allocate();

            sb.Append(timeSpan.Ticks < 0 ? "-P" : "P");

            double ticks = timeSpan.Ticks;
            if (ticks < 0)
                ticks = -ticks;

            double totalSeconds = ticks / TimeSpan.TicksPerSecond;
            long wholeSeconds = (long) totalSeconds;
            long seconds = wholeSeconds;
            long sec = (seconds >= 60 ? seconds % 60 : seconds);
            long min = (seconds = (seconds / 60)) >= 60 ? seconds % 60 : seconds;
            long hours = (seconds = (seconds / 60)) >= 24 ? seconds % 24 : seconds;
            long days = seconds / 24;
            double remainingSecs = sec + (totalSeconds - wholeSeconds);

            if (days > 0)
                sb.Append(days + "D");

            if (days == 0 || hours + min + sec + remainingSecs > 0)
            {
                sb.Append("T");
                if (hours > 0)
                    sb.Append(hours + "H");

                if (min > 0)
                    sb.Append(min + "M");

                if (remainingSecs > 0)
                {
                    var secFmt = string.Format(CultureInfo.InvariantCulture, "{0:0.0000000}", remainingSecs);
                    secFmt = secFmt.TrimEnd('0').TrimEnd('.');
                    sb.Append(secFmt + "S");
                }
                else if (sb.Length == 2) //PT
                {
                    sb.Append("0S");
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static TimeSpan FromXsdDuration(string xsdDuration)
        {
            if (xsdDuration == MinSerializedValue)
                return TimeSpan.MinValue;
            if (xsdDuration == MaxSerializedValue)
                return TimeSpan.MaxValue;
            
            long days = 0;
            long hours = 0;
            long minutes = 0;
            decimal seconds = 0;
            long sign = 1;

            if (xsdDuration.StartsWith("-", StringComparison.Ordinal))
            {
                sign = -1;
                xsdDuration = xsdDuration.Substring(1); //strip sign
            }

            string[] t = xsdDuration.Substring(1).SplitOnFirst('T'); //strip P

            var hasTime = t.Length == 2;

            string[] d = t[0].SplitOnFirst('D');
            if (d.Length == 2)
            {
                if (long.TryParse(d[0], out var day))
                    days = day;
            }
            if (hasTime)
            {
                string[] h = t[1].SplitOnFirst('H');
                if (h.Length == 2)
                {
                    if (long.TryParse(h[0], out var hour))
                        hours = hour;
                }

                string[] m = h[h.Length - 1].SplitOnFirst('M');
                if (m.Length == 2)
                {
                    if (long.TryParse(m[0], out var min))
                        minutes = min;
                }

                string[] s = m[m.Length - 1].SplitOnFirst('S');
                if (s.Length == 2)
                {
                    if (decimal.TryParse(s[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var millis))
                        seconds = millis;
                }
            }

            decimal totalSecs = 0
                    + (days * 24 * 60 * 60)
                    + (hours * 60 * 60)
                    + (minutes * 60)
                    + (seconds);

            var interval = (long) (totalSecs * TimeSpan.TicksPerSecond * sign);

            return TimeSpan.FromTicks(interval);
        }
    }
}
