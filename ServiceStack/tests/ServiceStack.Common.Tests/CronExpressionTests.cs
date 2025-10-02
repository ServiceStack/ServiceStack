#if NET6_0_OR_GREATER
// The MIT License(MIT)
// 
// Copyright (c) 2017 Hangfire OÜ
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ServiceStack.Cronos;

namespace ServiceStack.Common.Tests;

public class CronExpressionTests
{
    private static readonly bool IsUnix =
        Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
    
    private static readonly string EasternTimeZoneId = IsUnix ? "America/New_York" : "Eastern Standard Time";
    private static readonly string JordanTimeZoneId = IsUnix ? "Asia/Amman" : "Jordan Standard Time";
    private static readonly string LordHoweTimeZoneId = IsUnix ? "Australia/Lord_Howe" : "Lord Howe Standard Time";
    private static readonly string PacificTimeZoneId = IsUnix ? "America/Santiago" : "Pacific SA Standard Time";

    private static readonly TimeZoneInfo EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);
    private static readonly TimeZoneInfo JordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(JordanTimeZoneId);
    private static readonly TimeZoneInfo LordHoweTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LordHoweTimeZoneId);
    private static readonly TimeZoneInfo PacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById(PacificTimeZoneId);

    private static readonly DateTime Today = new DateTime(2016, 12, 09, 00, 00, 00, DateTimeKind.Utc);

    private static readonly CronExpression MinutelyExpression = CronExpression.Parse("* * * * *");

    // Handle tabs.
    [TestCase("*	*	* * * *")]
    // Handle white spaces at the beginning and end of expression.
    [TestCase(" 	*	*	* * * *    ")]
    // Handle white spaces for macros.
    [TestCase("  @every_second ")]
    public void HandleWhiteSpaces(string cronExpression)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var from = new DateTime(2016, 03, 18, 12, 0, 0, DateTimeKind.Utc);

        var result = expression.GetNextOccurrence(from, inclusive: true);

        Assert.AreEqual(from, result);
    }

    [Test]
    public void Parse_ThrowAnException_WhenCronExpressionIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(null!));

        Assert.AreEqual("expression", exception.ParamName);
    }

    [Test]
    public void Parse_ThrowCronFormatException_WhenCronExpressionIsEmpty()
    {
        Assert.Throws<CronFormatException>(() => CronExpression.Parse(""));
    }

    // Second field is invalid.

    [TestCase("-1   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("-    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("5-   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase(",    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase(",1   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("/    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("*/   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("1/   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("1/0  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("1/60 * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("1/k  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("1k   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("#    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("*#1  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("0#2  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("L    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("l    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("W    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("w    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("LW   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
    [TestCase("lw   * * * * *", CronFormat.IncludeSeconds, "Seconds")]

    // 2147483648 = Int32.MaxValue + 1

    [TestCase("1/2147483648 * * * * *", CronFormat.IncludeSeconds, "Seconds")]

    // Minute field is invalid.
    [TestCase("60    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("-1    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("-     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("7-    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase(",     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase(",1    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("*/    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("/     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("1/    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("1/0   * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("1/60  * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("1/k   * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("1k    * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("#     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("*#1   * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("5#3   * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("L     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("l     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("W     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("w     * * * *", CronFormat.Standard, "Minutes")]
    [TestCase("lw    * * * *", CronFormat.Standard, "Minutes")]

    [TestCase("* 60    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* -1    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* -     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 7-    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* ,     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* ,1    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* */    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* /     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 1/    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 1/0   * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 1/60  * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 1/k   * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 1k    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* #     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* *#1   * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* 5#3   * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* L     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* l     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* W     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* w     * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* LW    * * * *", CronFormat.IncludeSeconds, "Minutes")]
    [TestCase("* lw    * * * *", CronFormat.IncludeSeconds, "Minutes")]

    // Hour field is invalid.
    [TestCase("* 25   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* -1   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* -    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 0-   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* ,    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* ,1   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* /    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 1/   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 1/0  * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 1/24 * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 1/k  * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 1k   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* #    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* *#2  * * *", CronFormat.Standard, "Hours")]
    [TestCase("* 10#1 * * *", CronFormat.Standard, "Hours")]
    [TestCase("* L    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* l    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* W    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* w    * * *", CronFormat.Standard, "Hours")]
    [TestCase("* LW   * * *", CronFormat.Standard, "Hours")]
    [TestCase("* lw   * * *", CronFormat.Standard, "Hours")]

    [TestCase("* * 25   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * -1   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * -    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 0-   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * ,    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * ,1   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * /    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 1/   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 1/0  * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 1/24 * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 1/k  * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 1k   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * #    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * *#2  * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * 10#1 * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * L    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * l    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * W    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * w    * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * LW   * * *", CronFormat.IncludeSeconds, "Hours")]
    [TestCase("* * lw   * * *", CronFormat.IncludeSeconds, "Hours")]

    // Day of month field is invalid.
    [TestCase("* * 32     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 10-32  *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 31-32  *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * -1     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * -      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 8-     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * ,      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * ,1     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * /      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/0    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/32   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/k    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1m     *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * T      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * MON    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * mon    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * #      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * *#3    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 4#1    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * W      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * w      *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1-2W   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1-2w   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1,2W   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1,2w   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/2W   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1/2w   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1-2/2W *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1-2/2w *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1LW    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * 1lw    *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * L-31   *  *", CronFormat.Standard, "Days of month")]
    [TestCase("* * l-31   *  *", CronFormat.Standard, "Days of month")]

    [TestCase("* * * 32     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 10-32  *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 31-32  *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * -1     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * -      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 8-     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * ,      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * ,1     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * /      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/0    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/32   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/k    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1m     *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * T      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * MON    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * mon    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * #      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * *#3    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 4#1    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * W      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * w      *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1-2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1-2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1,2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1,2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1/2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1-2/2W *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1-2/2w *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1LW    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * 1lw    *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * L-31   *  *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * l-31   *  *", CronFormat.IncludeSeconds, "Days of month")]

    // Month field is invalid.
    [TestCase("* * * 13   *", CronFormat.Standard, "Months")]
    [TestCase("* * * -1   *", CronFormat.Standard, "Months")]
    [TestCase("* * * -    *", CronFormat.Standard, "Months")]
    [TestCase("* * * 2-   *", CronFormat.Standard, "Months")]
    [TestCase("* * * ,    *", CronFormat.Standard, "Months")]
    [TestCase("* * * ,1   *", CronFormat.Standard, "Months")]
    [TestCase("* * * /    *", CronFormat.Standard, "Months")]
    [TestCase("* * * */   *", CronFormat.Standard, "Months")]
    [TestCase("* * * 1/   *", CronFormat.Standard, "Months")]
    [TestCase("* * * 1/0  *", CronFormat.Standard, "Months")]
    [TestCase("* * * 1/13 *", CronFormat.Standard, "Months")]
    [TestCase("* * * 1/k  *", CronFormat.Standard, "Months")]
    [TestCase("* * * 1k   *", CronFormat.Standard, "Months")]
    [TestCase("* * * #    *", CronFormat.Standard, "Months")]
    [TestCase("* * * *#1  *", CronFormat.Standard, "Months")]
    [TestCase("* * * */2# *", CronFormat.Standard, "Months")]
    [TestCase("* * * 2#2  *", CronFormat.Standard, "Months")]
    [TestCase("* * * L    *", CronFormat.Standard, "Months")]
    [TestCase("* * * l    *", CronFormat.Standard, "Months")]
    [TestCase("* * * W    *", CronFormat.Standard, "Months")]
    [TestCase("* * * w    *", CronFormat.Standard, "Months")]
    [TestCase("* * * LW   *", CronFormat.Standard, "Months")]
    [TestCase("* * * lw   *", CronFormat.Standard, "Months")]

    [TestCase("* * * * 13   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * -1   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * -    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 2-   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * ,    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * ,1   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * /    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * */   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 1/   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 1/0  *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 1/13 *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 1/k  *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 1k   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * #    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * *#1  *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * */2# *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * 2#2  *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * L    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * l    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * W    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * w    *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * LW   *", CronFormat.IncludeSeconds, "Months")]
    [TestCase("* * * * lw   *", CronFormat.IncludeSeconds, "Months")]

    // Day of week field is invalid.
    [TestCase("* * * * 8      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * -1     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * -      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 3-     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * ,      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * ,1     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * /      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * */     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 1/     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 1/0    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 1/8    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * #      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 0#     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 5#6    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * SUN#6  ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * sun#6  ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * SUN#050", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * sun#050", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * 0#0    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * SUT    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * sut    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * SU0    ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * SUNDAY ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * L      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * l      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * W      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * w      ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * LW     ", CronFormat.Standard, "Days of week")]
    [TestCase("* * * * lw     ", CronFormat.Standard, "Days of week")]

    [TestCase("* * * * * 8      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * -1     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * -      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 3-     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * ,      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * ,1     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * /      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * */     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 1/     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 1/0    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 1/8    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * #      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 0#     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 5#6    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * SUN#6  ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * sun#6  ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * SUN#050", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * sun#050", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * 0#0    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * SUT    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * sut    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * SU0    ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * SUNDAY ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * L      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * l      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * W      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * w      ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * LW     ", CronFormat.IncludeSeconds, "Days of week")]
    [TestCase("* * * * * lw     ", CronFormat.IncludeSeconds, "Days of week")]

    // Fields count is invalid.
    [TestCase("* * *        ", CronFormat.Standard, "Months")]
    [TestCase("* * * * * * *", CronFormat.Standard, "")]

    [TestCase("* * * *", CronFormat.IncludeSeconds, "Days of month")]
    [TestCase("* * * * * * *", CronFormat.IncludeSeconds, "")]

    // Macro is invalid.
    [TestCase("@", CronFormat.Standard, "")]

    // ReSharper disable StringLiteralTypo
    [TestCase("@invalid        ", CronFormat.Standard, "")]
    [TestCase("          @yearl", CronFormat.Standard, "")]
    [TestCase("@yearl          ", CronFormat.Standard, "")]
    [TestCase("@yearly !       ", CronFormat.Standard, "")]
    [TestCase("@every_hour     ", CronFormat.Standard, "")]
    [TestCase("@@daily         ", CronFormat.Standard, "")]
    [TestCase("@yeannually     ", CronFormat.Standard, "")]
    [TestCase("@yweekly        ", CronFormat.Standard, "")]
    [TestCase("@ymonthly       ", CronFormat.Standard, "")]
    [TestCase("@ydaily         ", CronFormat.Standard, "")]
    [TestCase("@ymidnight      ", CronFormat.Standard, "")]
    [TestCase("@yhourly        ", CronFormat.Standard, "")]
    [TestCase("@yevery_second  ", CronFormat.Standard, "")]
    [TestCase("@yevery_minute  ", CronFormat.Standard, "")]
    [TestCase("@every_minsecond", CronFormat.Standard, "")]
    [TestCase("@annuall        ", CronFormat.Standard, "")]
    [TestCase("@dail           ", CronFormat.Standard, "")]
    [TestCase("@hour           ", CronFormat.Standard, "")]
    [TestCase("@midn           ", CronFormat.Standard, "")]
    [TestCase("@week           ", CronFormat.Standard, "")]

    [TestCase("@", CronFormat.IncludeSeconds, "")]

    [TestCase("@invalid        ", CronFormat.IncludeSeconds, "")]
    [TestCase("          @yearl", CronFormat.IncludeSeconds, "")]
    [TestCase("@yearl          ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yearly !       ", CronFormat.IncludeSeconds, "")]
    [TestCase("@dai            ", CronFormat.IncludeSeconds, "")]
    [TestCase("@a              ", CronFormat.IncludeSeconds, "")]
    [TestCase("@every_hour     ", CronFormat.IncludeSeconds, "")]
    [TestCase("@everysecond    ", CronFormat.IncludeSeconds, "")]
    [TestCase("@@daily         ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yeannually     ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yweekly        ", CronFormat.IncludeSeconds, "")]
    [TestCase("@ymonthly       ", CronFormat.IncludeSeconds, "")]
    [TestCase("@ydaily         ", CronFormat.IncludeSeconds, "")]
    [TestCase("@ymidnight      ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yhourly        ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yevery_second  ", CronFormat.IncludeSeconds, "")]
    [TestCase("@yevery_minute  ", CronFormat.IncludeSeconds, "")]
    [TestCase("@every_minsecond", CronFormat.IncludeSeconds, "")]
    [TestCase("@annuall        ", CronFormat.IncludeSeconds, "")]
    [TestCase("@dail           ", CronFormat.IncludeSeconds, "")]
    [TestCase("@hour           ", CronFormat.IncludeSeconds, "")]
    [TestCase("@midn           ", CronFormat.IncludeSeconds, "")]
    [TestCase("@week           ", CronFormat.IncludeSeconds, "")]
    
    [TestCase("60 * * * *", CronFormat.Standard, "between 0 and 59")]
    [TestCase("*/60 * * * *", CronFormat.Standard, "between 1 and 59")]
    // ReSharper restore StringLiteralTypo
    public void Parse_ThrowsCronFormatException_WhenCronExpressionIsInvalid(string cronExpression, CronFormat format, string invalidField)
    {
        var exception = Assert.Throws<CronFormatException>(() => CronExpression.Parse(cronExpression, format));

        StringAssert.Contains(invalidField, exception.Message);
    }


    [TestCase("  @yearly      ", CronFormat.Standard)]
    [TestCase("  @YEARLY      ", CronFormat.Standard)]
    [TestCase("  @annually    ", CronFormat.Standard)]
    [TestCase("  @ANNUALLY    ", CronFormat.Standard)]
    [TestCase("  @monthly     ", CronFormat.Standard)]
    [TestCase("  @MONTHLY     ", CronFormat.Standard)]
    [TestCase("  @weekly      ", CronFormat.Standard)]
    [TestCase("  @WEEKLY      ", CronFormat.Standard)]
    [TestCase("  @daily       ", CronFormat.Standard)]
    [TestCase("  @DAILY       ", CronFormat.Standard)]
    [TestCase("  @midnight    ", CronFormat.Standard)]
    [TestCase("  @MIDNIGHT    ", CronFormat.Standard)]
    [TestCase("  @every_minute", CronFormat.Standard)]
    [TestCase("  @EVERY_MINUTE", CronFormat.Standard)]
    [TestCase("  @every_second", CronFormat.Standard)]
    [TestCase("  @EVERY_SECOND", CronFormat.Standard)]

    [TestCase("  @yearly      ", CronFormat.IncludeSeconds)]
    [TestCase("  @YEARLY      ", CronFormat.IncludeSeconds)]
    [TestCase("  @annually    ", CronFormat.IncludeSeconds)]
    [TestCase("  @ANNUALLY    ", CronFormat.IncludeSeconds)]
    [TestCase("  @monthly     ", CronFormat.IncludeSeconds)]
    [TestCase("  @MONTHLY     ", CronFormat.IncludeSeconds)]
    [TestCase("  @weekly      ", CronFormat.IncludeSeconds)]
    [TestCase("  @WEEKLY      ", CronFormat.IncludeSeconds)]
    [TestCase("  @daily       ", CronFormat.IncludeSeconds)]
    [TestCase("  @DAILY       ", CronFormat.IncludeSeconds)]
    [TestCase("  @midnight    ", CronFormat.IncludeSeconds)]
    [TestCase("  @MIDNIGHT    ", CronFormat.IncludeSeconds)]
    [TestCase("  @every_minute", CronFormat.IncludeSeconds)]
    [TestCase("  @EVERY_MINUTE", CronFormat.IncludeSeconds)]
    [TestCase("  @every_second", CronFormat.IncludeSeconds)]
    [TestCase("  @EVERY_SECOND", CronFormat.IncludeSeconds)]
    public void Parse_DoesNotThrowAnException_WhenExpressionIsMacro(string cronExpression, CronFormat format)
    {
        CronExpression.Parse(cronExpression, format);
    }

    [Test]
    public void TryParse_ThrowsAnException_WhenExpressionIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.TryParse(null!, out _));
        Assert.AreEqual("expression", exception.ParamName);
    }

    [Test]
    public void TryParse_DoesNotThrowAnException_WhenExpressionIsEmpty()
    {
        var result = CronExpression.TryParse("", out var cron);

        Assert.IsFalse(result);
        Assert.IsNull(cron);
    }

    [Test]
    public void TryParse_ReturnsTrue_WhenAbleToParseTheGivenExpression_WithCorrectCronExpressionInstance()
    {
        var result = CronExpression.TryParse("* * * * *", out var cron);

        Assert.IsTrue(result);
        Assert.AreEqual(Today.AddMinutes(1), cron!.GetNextOccurrence(Today));
    }

    [Test]
    public void TryParse_ResultsFalse_WhenNotAbleToParseTheGivenExpression_WithNullCronExpressionInstance()
    {
        var result = CronExpression.TryParse("SomeG@rbage", out var cron);
        
        Assert.IsFalse(result);
        Assert.IsNull(cron);
    }

    [Test]
    public void TryParse_ReturnsFalse_WhenTheNumberOfFieldsIsNotExpected()
    {
        var result = CronExpression.TryParse("* * * * * *", out _);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryParse_WithSecondsSpecified_ReturnsTrue_AndGivesSecondBasedCronExpressionInstance()
    {
        var result = CronExpression.TryParse("* * * * * *", CronFormat.IncludeSeconds, out var cron);

        Assert.IsTrue(result);
        Assert.AreEqual(Today.AddSeconds(1), cron!.GetNextOccurrence(Today));
    }


    [TestCase(DateTimeKind.Unspecified, false)]
    [TestCase(DateTimeKind.Unspecified, true)]
    [TestCase(DateTimeKind.Local,       false)]
    [TestCase(DateTimeKind.Local,       true)]
    public void GetNextOccurrence_ThrowsAnException_WhenFromHasAWrongKind(DateTimeKind kind, bool inclusive)
    {
        var from = new DateTime(2017, 03, 22, 0, 0, 0, kind);
        
        var exception = Assert.Throws<ArgumentException>(() => MinutelyExpression.GetNextOccurrence(from, TimeZoneInfo.Local, inclusive));

        Assert.AreEqual("fromUtc", exception.ParamName);
    }


    [TestCase(DateTimeKind.Unspecified, false)]
    [TestCase(DateTimeKind.Unspecified, true)]
    [TestCase(DateTimeKind.Local, false)]
    [TestCase(DateTimeKind.Local, true)]
    public void GetNextOccurrence_ThrowsAnException_WhenFromDoesNotHaveUtcKind(DateTimeKind kind, bool inclusive)
    {
        var from = new DateTime(2017, 03, 15, 0, 0, 0, kind);
        var exception = Assert.Throws<ArgumentException>(() => MinutelyExpression.GetNextOccurrence(from, inclusive));

        Assert.AreEqual("fromUtc", exception.ParamName);
    }

    [Test]
    public void GetNextOccurrence_DateTimeTimeZone_ThrowsAnException_WhenZoneIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => CronExpression.EveryMinute.GetNextOccurrence(Today, null!));

        Assert.AreEqual("zone", exception.ParamName);
    }

    [Test]
    public void GetNextOccurrence_DateTimeOffsetTimeZone_ThrowsAnException_WhenZoneIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => CronExpression.EveryMinute.GetNextOccurrence(new DateTimeOffset(Today), null!));

        Assert.AreEqual("zone", exception.ParamName);
    }


    [TestCase(false)]
    [TestCase(true)]
    public void GetNextOccurrence_ReturnsDateTimeWithUtcKind(bool inclusive)
    {
        var from = new DateTime(2017, 03, 22, 9, 32, 0, DateTimeKind.Utc);
        var occurrence = MinutelyExpression.GetNextOccurrence(from, inclusive);

        Assert.AreEqual(DateTimeKind.Utc, occurrence?.Kind);
    }
    // Basic facts.

    [TestCase("* * * * * *", "17:35:00", "17:35:00")]

    // Second specified.

    [TestCase("20    * * * * *", "17:35:00", "17:35:20")]
    [TestCase("20    * * * * *", "17:35:20", "17:35:20")]
    [TestCase("20    * * * * *", "17:35:40", "17:36:20")]
    [TestCase("10-30 * * * * *", "17:35:09", "17:35:10")]
    [TestCase("10-30 * * * * *", "17:35:10", "17:35:10")]
    [TestCase("10-30 * * * * *", "17:35:20", "17:35:20")]
    [TestCase("10-30 * * * * *", "17:35:30", "17:35:30")]
    [TestCase("10-30 * * * * *", "17:35:31", "17:36:10")]
    [TestCase("*/20  * * * * *", "17:35:00", "17:35:00")]
    [TestCase("*/20  * * * * *", "17:35:11", "17:35:20")]
    [TestCase("*/20  * * * * *", "17:35:20", "17:35:20")]
    [TestCase("*/20  * * * * *", "17:35:25", "17:35:40")]
    [TestCase("*/20  * * * * *", "17:35:59", "17:36:00")]
    [TestCase("10/5  * * * * *", "17:35:00", "17:35:10")]
    [TestCase("10/5  * * * * *", "17:35:12", "17:35:15")]
    [TestCase("10/5  * * * * *", "17:35:59", "17:36:10")]
    [TestCase("0     * * * * *", "17:35:59", "17:36:00")]
    [TestCase("0     * * * * *", "17:59:59", "18:00:00")]

    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:01", "17:35:05")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:06", "17:35:06")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:18", "17:35:19")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:19", "17:35:19")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:20", "17:35:20")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:21", "17:35:35")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:36", "17:35:36")]
    [TestCase("5-8,19,20,35-41 * * * * *", "17:35:42", "17:36:05")]

    [TestCase("55-5 * * * * ?", "17:35:42", "17:35:55")]
    [TestCase("55-5 * * * * ?", "17:35:57", "17:35:57")]
    [TestCase("55-5 * * * * ?", "17:35:59", "17:35:59")]
    [TestCase("55-5 * * * * ?", "17:36:00", "17:36:00")]
    [TestCase("55-5 * * * * ?", "17:36:05", "17:36:05")]
    [TestCase("55-5 * * * * ?", "17:36:06", "17:36:55")]

    [TestCase("57-5/3 * * * * ?", "17:36:06", "17:36:57")]
    [TestCase("57-5/3 * * * * ?", "17:36:58", "17:37:00")]
    [TestCase("57-5/3 * * * * ?", "17:37:01", "17:37:03")]
    [TestCase("57-5/3 * * * * ?", "17:37:04", "17:37:57")]

    [TestCase("59-58 * * * * ?", "17:37:04", "17:37:04")]
    [TestCase("59-58 * * * * ?", "17:37:58", "17:37:58")]
    [TestCase("59-58 * * * * ?", "17:37:59", "17:37:59")]
    [TestCase("59-58 * * * * ?", "17:38:00", "17:38:00")]

    // Minute specified.

    [TestCase("* 12    * * * *", "15:05", "15:12")]
    [TestCase("* 12    * * * *", "15:12", "15:12")]
    [TestCase("* 12    * * * *", "15:59", "16:12")]
    [TestCase("* 31-39 * * * *", "15:00", "15:31")]
    [TestCase("* 31-39 * * * *", "15:30", "15:31")]
    [TestCase("* 31-39 * * * *", "15:31", "15:31")]
    [TestCase("* 31-39 * * * *", "15:39", "15:39")]
    [TestCase("* 31-39 * * * *", "15:59", "16:31")]
    [TestCase("* */20  * * * *", "15:00", "15:00")]
    [TestCase("* */20  * * * *", "15:10", "15:20")]
    [TestCase("* */20  * * * *", "15:59", "16:00")]
    [TestCase("* 10/5  * * * *", "15:00", "15:10")]
    [TestCase("* 10/5  * * * *", "15:14", "15:15")]
    [TestCase("* 10/5  * * * *", "15:59", "16:10")]
    [TestCase("* 0     * * * *", "15:59", "16:00")]

    [TestCase("* 5-8,19,20,35-41 * * * *", "15:01", "15:05")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:06", "15:06")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:18", "15:19")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:19", "15:19")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:20", "15:20")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:21", "15:35")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:36", "15:36")]
    [TestCase("* 5-8,19,20,35-41 * * * *", "15:42", "16:05")]

    [TestCase("* 51-4 * * * *", "17:35", "17:51")]
    [TestCase("* 51-4 * * * *", "17:51", "17:51")]
    [TestCase("* 51-4 * * * *", "17:55", "17:55")]
    [TestCase("* 51-4 * * * *", "17:59", "17:59")]
    [TestCase("* 51-4 * * * *", "18:00", "18:00")]
    [TestCase("* 51-4 * * * *", "18:04", "18:04")]
    [TestCase("* 51-4 * * * *", "18:05", "18:51")]

    [TestCase("* 56-4/4 * * * *", "17:55", "17:56")]
    [TestCase("* 56-4/4 * * * *", "17:57", "18:00")]
    [TestCase("* 56-4/4 * * * *", "18:01", "18:04")]
    [TestCase("* 56-4/4 * * * *", "18:05", "18:56")]

    [TestCase("* 45-44 * * * *", "18:45", "18:45")]
    [TestCase("* 45-44 * * * *", "18:55", "18:55")]
    [TestCase("* 45-44 * * * *", "18:59", "18:59")]
    [TestCase("* 45-44 * * * *", "19:00", "19:00")]
    [TestCase("* 45-44 * * * *", "19:44", "19:44")]

    // Hour specified.

    [TestCase("* * 11   * * *", "10:59", "11:00")]
    [TestCase("* * 11   * * *", "11:30", "11:30")]
    [TestCase("* * 3-22 * * *", "01:40", "03:00")]
    [TestCase("* * 3-22 * * *", "11:40", "11:40")]
    [TestCase("* * */2  * * *", "00:00", "00:00")]
    [TestCase("* * */2  * * *", "01:00", "02:00")]
    [TestCase("* * 4/5  * * *", "00:45", "04:00")]
    [TestCase("* * 4/5  * * *", "04:14", "04:14")]
    [TestCase("* * 4/5  * * *", "05:00", "09:00")]

    [TestCase("* * 3-5,10,11,13-17 * * *", "01:55", "03:00")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "04:55", "04:55")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "06:10", "10:00")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "10:55", "10:55")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "11:25", "11:25")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "12:30", "13:00")]
    [TestCase("* * 3-5,10,11,13-17 * * *", "17:30", "17:30")]

    [TestCase("* * 23-3/2 * * *", "17:30", "23:00")]
    [TestCase("* * 23-3/2 * * *", "00:30", "01:00")]
    [TestCase("* * 23-3/2 * * *", "02:00", "03:00")]
    [TestCase("* * 23-3/2 * * *", "04:00", "23:00")]

    [TestCase("* * 23-22 * * *", "22:10", "22:10")]
    [TestCase("* * 23-22 * * *", "23:10", "23:10")]
    [TestCase("* * 23-22 * * *", "00:10", "00:10")]
    [TestCase("* * 23-22 * * *", "07:10", "07:10")]

    // Day of month specified.

    [TestCase("* * * 9     * *", "2016-11-01", "2016-11-09")]
    [TestCase("* * * 9     * *", "2016-11-09", "2016-11-09")]
    [TestCase("* * * 09    * *", "2016-11-10", "2016-12-09")]
    [TestCase("* * * */4   * *", "2016-12-01", "2016-12-01")]
    [TestCase("* * * */4   * *", "2016-12-02", "2016-12-05")]
    [TestCase("* * * */4   * *", "2016-12-06", "2016-12-09")]
    [TestCase("* * * */3   * *", "2016-12-02", "2016-12-04")]
    [TestCase("* * * 10,20 * *", "2016-12-09", "2016-12-10")]
    [TestCase("* * * 10,20 * *", "2016-12-12", "2016-12-20")]
    [TestCase("* * * 16-23 * *", "2016-12-01", "2016-12-16")]
    [TestCase("* * * 16-23 * *", "2016-12-16", "2016-12-16")]
    [TestCase("* * * 16-23 * *", "2016-12-18", "2016-12-18")]
    [TestCase("* * * 16-23 * *", "2016-12-23", "2016-12-23")]
    [TestCase("* * * 16-23 * *", "2016-12-24", "2017-01-16")]

    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-01", "2016-12-05")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-05", "2016-12-05")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-06", "2016-12-06")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-08", "2016-12-08")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-09", "2016-12-19")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-20", "2016-12-20")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-21", "2016-12-28")]
    [TestCase("* * * 5-8,19,20,28-29 * *", "2016-12-30", "2017-01-05")]
    [TestCase("* * * 5-8,19,20,29-30 * *", "2017-02-27", "2017-03-05")]

    [TestCase("* * * 30-31 * *", "2016-02-27", "2016-03-30")]
    [TestCase("* * * 30-31 * *", "2017-02-27", "2017-03-30")]
    [TestCase("* * * 31    * *", "2017-04-27", "2017-05-31")]

    [TestCase("* * * 20-5/5 * *", "2017-05-19", "2017-05-20")]
    [TestCase("* * * 20-5/5 * *", "2017-05-21", "2017-05-25")]
    [TestCase("* * * 20-5/5 * *", "2017-05-26", "2017-05-30")]
    [TestCase("* * * 20-5/5 * *", "2017-06-01", "2017-06-04")]
    [TestCase("* * * 20-5/5 * *", "2017-06-05", "2017-06-20")]

    [TestCase("* * * 20-5/5 * *", "2017-07-01", "2017-07-04")]

    [TestCase("* * * 20-5/5 * *", "2018-02-26", "2018-03-04")]
    
    // Month specified.

    [TestCase("* * * * 11      *", "2016-10-09", "2016-11-01")]
    [TestCase("* * * * 11      *", "2016-11-02", "2016-11-02")]
    [TestCase("* * * * 11      *", "2016-12-02", "2017-11-01")]
    [TestCase("* * * * 3,9     *", "2016-01-09", "2016-03-01")]
    [TestCase("* * * * 3,9     *", "2016-06-09", "2016-09-01")]
    [TestCase("* * * * 3,9     *", "2016-10-09", "2017-03-01")]
    [TestCase("* * * * 5-11    *", "2016-01-01", "2016-05-01")]
    [TestCase("* * * * 5-11    *", "2016-05-07", "2016-05-07")]
    [TestCase("* * * * 5-11    *", "2016-07-12", "2016-07-12")]
    [TestCase("* * * * 05-11   *", "2016-12-13", "2017-05-01")]
    [TestCase("* * * * DEC     *", "2016-08-09", "2016-12-01")]
    [TestCase("* * * * mar-dec *", "2016-02-09", "2016-03-01")]
    [TestCase("* * * * mar-dec *", "2016-04-09", "2016-04-09")]
    [TestCase("* * * * mar-dec *", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * */4     *", "2016-01-09", "2016-01-09")]
    [TestCase("* * * * */4     *", "2016-02-09", "2016-05-01")]
    [TestCase("* * * * */3     *", "2016-12-09", "2017-01-01")]
    [TestCase("* * * * */5     *", "2016-12-09", "2017-01-01")]
    [TestCase("* * * * APR-NOV *", "2016-12-09", "2017-04-01")]    

    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-01-01", "2016-02-01")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-02-10", "2016-02-10")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-03-01", "2016-03-01")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-05-20", "2016-06-01")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-06-10", "2016-06-10")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-07-05", "2016-07-05")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-08-15", "2016-09-01")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-11-25", "2016-11-25")]
    [TestCase("* * * * 2-4,JUN,7,SEP-nov *", "2016-12-01", "2017-02-01")]

    [TestCase("* * * * 12-2 *", "2016-05-19", "2016-12-01")]
    [TestCase("* * * * 12-2 *", "2017-01-19", "2017-01-19")]
    [TestCase("* * * * 12-2 *", "2017-02-19", "2017-02-19")]
    [TestCase("* * * * 12-2 *", "2017-03-19", "2017-12-01")]

    [TestCase("* * * * 9-8/3 *", "2016-07-19", "2016-09-01")]
    [TestCase("* * * * 9-8/3 *", "2016-10-19", "2016-12-01")]
    [TestCase("* * * * 9-8/3 *", "2017-01-19", "2017-03-01")]
    [TestCase("* * * * 9-8/3 *", "2017-04-19", "2017-06-01")]

    // Day of week specified.

    // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
    //                                           2016-12-01    2016-12-02    2016-12-03    2016-12-04
    // 2016-12-05    2016-12-06    2016-12-07    2016-12-08    2016-12-09    2016-12-10    2016-12-11
    // 2016-12-12    2016-12-13    2016-12-14    2016-12-15    2016-12-16    2016-12-17    2016-12-18

    [TestCase("* * * * * 5      ", "2016-12-07", "2016-12-09")]
    [TestCase("* * * * * 5      ", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * * 05     ", "2016-12-10", "2016-12-16")]
    [TestCase("* * * * * 3,5,7  ", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * * 3,5,7  ", "2016-12-10", "2016-12-11")]
    [TestCase("* * * * * 3,5,7  ", "2016-12-12", "2016-12-14")]
    [TestCase("* * * * * 4-7    ", "2016-12-08", "2016-12-08")]
    [TestCase("* * * * * 4-7    ", "2016-12-10", "2016-12-10")]
    [TestCase("* * * * * 4-7    ", "2016-12-11", "2016-12-11")]
    [TestCase("* * * * * 4-07   ", "2016-12-12", "2016-12-15")]
    [TestCase("* * * * * FRI    ", "2016-12-08", "2016-12-09")]
    [TestCase("* * * * * tue/2  ", "2016-12-09", "2016-12-10")]
    [TestCase("* * * * * tue/2  ", "2016-12-11", "2016-12-13")]
    [TestCase("* * * * * FRI/3  ", "2016-12-03", "2016-12-09")]
    [TestCase("* * * * * thu-sat", "2016-12-04", "2016-12-08")]
    [TestCase("* * * * * thu-sat", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * * thu-sat", "2016-12-10", "2016-12-10")]
    [TestCase("* * * * * thu-sat", "2016-12-12", "2016-12-15")]
    [TestCase("* * * * * */5    ", "2016-12-08", "2016-12-09")]
    [TestCase("* * * * * */5    ", "2016-12-10", "2016-12-11")]
    [TestCase("* * * * * */5    ", "2016-12-12", "2016-12-16")]
    [TestCase("* * * ? * thu-sun", "2016-12-09", "2016-12-09")]

    [TestCase("* * * ? * sat-tue", "2016-12-10", "2016-12-10")]
    [TestCase("* * * ? * sat-tue", "2016-12-11", "2016-12-11")]
    [TestCase("* * * ? * sat-tue", "2016-12-12", "2016-12-12")]
    [TestCase("* * * ? * sat-tue", "2016-12-13", "2016-12-13")]
    [TestCase("* * * ? * sat-tue", "2016-12-14", "2016-12-17")]

    [TestCase("* * * ? * sat-tue/2", "2016-12-10", "2016-12-10")]
    [TestCase("* * * ? * sat-tue/2", "2016-12-11", "2016-12-12")]
    [TestCase("* * * ? * sat-tue/2", "2016-12-12", "2016-12-12")]
    [TestCase("* * * ? * sat-tue/2", "2016-12-13", "2016-12-17")]

    [TestCase("00 00 00 11 12 0  ", "2016-12-07", "2016-12-11")]
    [TestCase("00 00 00 11 12 7  ", "2016-12-09", "2016-12-11")]
    [TestCase("00 00 00 11 12 SUN", "2016-12-10", "2016-12-11")]
    [TestCase("00 00 00 11 12 sun", "2016-12-09", "2016-12-11")]

    // All fields are specified.

    [TestCase("54    47    17    09   12    5    ", "2016-10-01 00:00:00", "2016-12-09 17:47:54")]
    [TestCase("54    47    17    09   DEC   FRI  ", "2016-07-05 00:00:00", "2016-12-09 17:47:54")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-01 00:00:00", "2016-12-09 15:40:50")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40:53", "2016-12-09 15:40:53")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40:57", "2016-12-09 15:41:50")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:45:56", "2016-12-09 15:45:56")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:51:56", "2016-12-09 16:40:50")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 21:50:56", "2016-12-10 15:40:50")]
    [TestCase("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-11 21:50:56", "2017-11-05 15:40:50")]

    // Friday the thirteenth.

    [TestCase("00    05    18    13   01    05   ", "2016-01-01 00:00:00", "2017-01-13 18:05:00")]
    [TestCase("00    05    18    13   *     05   ", "2016-01-01 00:00:00", "2016-05-13 18:05:00")]
    [TestCase("00    05    18    13   *     05   ", "2016-09-01 00:00:00", "2017-01-13 18:05:00")]
    [TestCase("00    05    18    13   *     05   ", "2017-02-01 00:00:00", "2017-10-13 18:05:00")]

    // Handle moving to next second, minute, hour, month, year.

    [TestCase("0 * * * * *", "2017-01-14 12:58:59", "2017-01-14 12:59:00")]

    [TestCase("0 0 * * * *", "2017-01-14 12:59", "2017-01-14 13:00")]
    [TestCase("0 0 0 * * *", "2017-01-14 23:00", "2017-01-15 00:00")]

    [TestCase("0 0 0 1 * *", "2016-02-10 00:00", "2016-03-01 00:00")]
    [TestCase("0 0 0 1 * *", "2017-02-10 00:00", "2017-03-01 00:00")]
    [TestCase("0 0 0 1 * *", "2017-04-10 00:00", "2017-05-01 00:00")]
    [TestCase("0 0 0 1 * *", "2017-01-30 00:00", "2017-02-01 00:00")]
    [TestCase("0 0 0 * * *", "2017-12-31 23:59", "2018-01-01 00:00")]

    // Skip month if day of month is specified and month has less days.

    [TestCase("0 0 0 30 * *", "2017-02-25 00:00", "2017-03-30 00:00")]
    [TestCase("0 0 0 31 * *", "2017-02-25 00:00", "2017-03-31 00:00")]
    [TestCase("0 0 0 31 * *", "2017-04-01 00:00", "2017-05-31 00:00")]

    // Leap year.

    [TestCase("0 0 0 29 2 *", "2016-03-10 00:00", "2020-02-29 00:00")]

    // Support 'L' character in day of month field.

    [TestCase("* * * L * *","2016-01-05", "2016-01-31")]
    [TestCase("* * * L * *","2016-01-31", "2016-01-31")]
    [TestCase("* * * L * *","2016-02-05", "2016-02-29")]
    [TestCase("* * * L * *","2016-02-29", "2016-02-29")]
    [TestCase("* * * L 2 *","2016-02-29", "2016-02-29")]
    [TestCase("* * * L * *","2017-02-28", "2017-02-28")]
    [TestCase("* * * L * *","2016-03-05", "2016-03-31")]
    [TestCase("* * * L * *","2016-03-31", "2016-03-31")]
    [TestCase("* * * L * *","2016-04-05", "2016-04-30")]
    [TestCase("* * * L * *","2016-04-30", "2016-04-30")]
    [TestCase("* * * L * *","2016-05-05", "2016-05-31")]
    [TestCase("* * * L * *","2016-05-31", "2016-05-31")]
    [TestCase("* * * L * *","2016-06-05", "2016-06-30")]
    [TestCase("* * * L * *","2016-06-30", "2016-06-30")]
    [TestCase("* * * L * *","2016-07-05", "2016-07-31")]
    [TestCase("* * * L * *","2016-07-31", "2016-07-31")]
    [TestCase("* * * L * *","2016-08-05", "2016-08-31")]
    [TestCase("* * * L * *","2016-08-31", "2016-08-31")]
    [TestCase("* * * L * *","2016-09-05", "2016-09-30")]
    [TestCase("* * * L * *","2016-09-30", "2016-09-30")]
    [TestCase("* * * L * *","2016-10-05", "2016-10-31")]
    [TestCase("* * * L * *","2016-10-31", "2016-10-31")]
    [TestCase("* * * L * *","2016-11-05", "2016-11-30")]
    [TestCase("* * * L * *","2016-12-05", "2016-12-31")]
    [TestCase("* * * L * *","2016-12-31", "2016-12-31")]
    [TestCase("* * * L * *","2099-12-05", "2099-12-31")]
    [TestCase("* * * L * *","2099-12-31", "2099-12-31")]

    [TestCase("* * * L-1 * *","2016-01-01", "2016-01-30")]
    [TestCase("* * * L-1 * *","2016-01-29", "2016-01-30")]
    [TestCase("* * * L-1 * *","2016-01-30", "2016-01-30")]
    [TestCase("* * * L-1 * *","2016-01-31", "2016-02-28")]
    [TestCase("* * * L-1 * *","2016-02-01", "2016-02-28")]
    [TestCase("* * * L-1 * *","2016-02-28", "2016-02-28")]
    [TestCase("* * * L-1 * *","2017-02-01", "2017-02-27")]
    [TestCase("* * * L-1 * *","2017-02-27", "2017-02-27")]
    [TestCase("* * * L-1 * *","2016-04-01", "2016-04-29")]
    [TestCase("* * * L-1 * *","2016-04-29", "2016-04-29")]
    [TestCase("* * * L-1 * *","2016-12-01", "2016-12-30")]

    [TestCase("* * * L-2 * *", "2016-01-05", "2016-01-29")]
    [TestCase("* * * L-2 * *", "2016-01-30", "2016-02-27")]
    [TestCase("* * * L-2 * *", "2016-02-01", "2016-02-27")]
    [TestCase("* * * L-2 * *", "2017-02-01", "2017-02-26")]
    [TestCase("* * * L-2 * *", "2016-04-01", "2016-04-28")]
    [TestCase("* * * L-2 * *", "2016-12-01", "2016-12-29")]
    [TestCase("* * * L-2 * *", "2016-12-29", "2016-12-29")]
    [TestCase("* * * L-2 * *", "2016-12-30", "2017-01-29")]

    [TestCase("* * * L-28 * *", "2016-01-01", "2016-01-03")]
    [TestCase("* * * L-28 * *", "2016-04-01", "2016-04-02")]
    [TestCase("* * * L-28 * *", "2016-02-01", "2016-02-01")]
    [TestCase("* * * L-28 * *", "2017-02-01", "2017-03-03")]

    [TestCase("* * * L-29 * *", "2016-01-01", "2016-01-02")]
    [TestCase("* * * L-29 * *", "2016-04-01", "2016-04-01")]
    [TestCase("* * * L-29 * *", "2016-02-01", "2016-03-02")]
    [TestCase("* * * L-29 * *", "2017-02-01", "2017-03-02")]

    [TestCase("* * * L-30 * *", "2016-01-01", "2016-01-01")]
    [TestCase("* * * L-30 * *", "2016-04-01", "2016-05-01")]
    [TestCase("* * * L-30 * *", "2016-02-01", "2016-03-01")]
    [TestCase("* * * L-30 * *", "2017-02-01", "2017-03-01")]

    // Support 'L' character in day of week field.

    // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
    // 2016-01-23    2016-01-24    2016-01-25    2016-01-26    2016-01-27    2016-01-28    2016-01-29
    // 2016-01-30    2016-01-31

    // ReSharper disable StringLiteralTypo
    [TestCase("* * * * * 0L  ", "2017-01-29", "2017-01-29")]
    [TestCase("* * * * * 0L  ", "2017-01-01", "2017-01-29")]
    [TestCase("* * * * * SUNL", "2017-01-01", "2017-01-29")]
    [TestCase("* * * * * 1L  ", "2017-01-30", "2017-01-30")]
    [TestCase("* * * * * 1L  ", "2017-01-01", "2017-01-30")]
    [TestCase("* * * * * MONL", "2017-01-01", "2017-01-30")]
    [TestCase("* * * * * 2L  ", "2017-01-31", "2017-01-31")]
    [TestCase("* * * * * 2L  ", "2017-01-01", "2017-01-31")]
    [TestCase("* * * * * TUEL", "2017-01-01", "2017-01-31")]
    [TestCase("* * * * * 3L  ", "2017-01-25", "2017-01-25")]
    [TestCase("* * * * * 3L  ", "2017-01-01", "2017-01-25")]
    [TestCase("* * * * * WEDL", "2017-01-01", "2017-01-25")]
    [TestCase("* * * * * 4L  ", "2017-01-26", "2017-01-26")]
    [TestCase("* * * * * 4L  ", "2017-01-01", "2017-01-26")]
    [TestCase("* * * * * THUL", "2017-01-01", "2017-01-26")]
    [TestCase("* * * * * 5L  ", "2017-01-27", "2017-01-27")]
    [TestCase("* * * * * 5L  ", "2017-01-01", "2017-01-27")]
    [TestCase("* * * * * FRIL", "2017-01-01", "2017-01-27")]
    [TestCase("* * * * * 6L  ", "2017-01-28", "2017-01-28")]
    [TestCase("* * * * * 6L  ", "2017-01-01", "2017-01-28")]
    [TestCase("* * * * * SATL", "2017-01-01", "2017-01-28")]
    [TestCase("* * * * * 7L  ", "2017-01-29", "2017-01-29")]
    [TestCase("* * * * * 7L  ", "2016-12-31", "2017-01-29")]
    // ReSharper restore StringLiteralTypo

    // Support '#' in day of week field.

    [TestCase("* * * * * SUN#1", "2017-01-01", "2017-01-01")]
    [TestCase("* * * * * 0#1  ", "2017-01-01", "2017-01-01")]
    [TestCase("* * * * * 0#1  ", "2016-12-10", "2017-01-01")]
    [TestCase("* * * * * 0#1  ", "2017-02-01", "2017-02-05")]
    [TestCase("* * * * * 0#2  ", "2017-01-01", "2017-01-08")]
    [TestCase("* * * * * 0#2  ", "2017-01-08", "2017-01-08")]
    [TestCase("* * * * * 5#3  ", "2017-01-01", "2017-01-20")]
    [TestCase("* * * * * 5#3  ", "2017-01-21", "2017-02-17")]
    [TestCase("* * * * * 3#2  ", "2017-01-01", "2017-01-11")]
    [TestCase("* * * * * 2#5  ", "2017-02-01", "2017-05-30")]

    // Support 'W' in day of month field.

    [TestCase("* * * 1W * *", "2017-01-01", "2017-01-02")]
    [TestCase("* * * 2W * *", "2017-01-02", "2017-01-02")]
    [TestCase("* * * 6W * *", "2017-01-02", "2017-01-06")]
    [TestCase("* * * 7W * *", "2017-01-02", "2017-01-06")]
    [TestCase("* * * 7W * *", "2017-01-07", "2017-02-07")]
    [TestCase("* * * 8W * *", "2017-01-02", "2017-01-09")]

    [TestCase("* * * 30W * *", "2017-04-27", "2017-04-28")]
    [TestCase("* * * 30W * *", "2017-04-28", "2017-04-28")]
    [TestCase("* * * 30W * *", "2017-04-29", "2017-05-30")]

    [TestCase("* * * 1W * *", "2017-04-01", "2017-04-03")]

    [TestCase("0 30    10 1W * *", "2017-04-01 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 1W * *", "2017-04-01 12:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 1W * *", "2017-04-02 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 1W * *", "2017-04-02 12:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 1W * *", "2017-04-03 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 1W * *", "2017-04-03 12:00", "2017-05-01 10:30")]

    [TestCase("0 30    10 2W * *", "2017-04-01 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 2W * *", "2017-04-01 12:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 2W * *", "2017-04-02 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 2W * *", "2017-04-02 12:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 2W * *", "2017-04-03 00:00", "2017-04-03 10:30")]
    [TestCase("0 30    10 2W * *", "2017-04-03 12:00", "2017-05-02 10:30")]

    [TestCase("0 30    17 7W * *", "2017-01-06 17:45", "2017-02-07 17:30")]
    [TestCase("0 30,45 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:45")]
    [TestCase("0 30,55 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:55")]

    [TestCase("0 30    17 8W * *", "2017-01-08 19:45", "2017-01-09 17:30")]

    [TestCase("0 30    17 30W * *", "2017-04-28 17:45", "2017-05-30 17:30")]
    [TestCase("0 30,45 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:45")]
    [TestCase("0 30,55 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:55")]

    [TestCase("0 30    17 30W * *", "2017-02-06 00:00", "2017-03-30 17:30")]

    [TestCase("0 30    17 31W * *", "2018-03-30 17:45", "2018-05-31 17:30")]
    [TestCase("0 30    17 15W * *", "2016-12-30 17:45", "2017-01-16 17:30")]

    [TestCase("0 30    17 27W * 1L ", "2017-03-10 17:45", "2017-03-27 17:30")]
    [TestCase("0 30    17 27W * 1#4", "2017-03-10 17:45", "2017-03-27 17:30")]

    // Support 'LW' in day of month field.

    [TestCase("* * * LW * *", "2017-01-01", "2017-01-31")]
    [TestCase("* * * LW * *", "2017-09-01", "2017-09-29")]
    [TestCase("* * * LW * *", "2017-09-29", "2017-09-29")]
    [TestCase("* * * LW * *", "2017-09-30", "2017-10-31")]
    [TestCase("* * * LW * *", "2017-04-01", "2017-04-28")]
    [TestCase("* * * LW * *", "2017-04-28", "2017-04-28")]
    [TestCase("* * * LW * *", "2017-04-29", "2017-05-31")]
    [TestCase("* * * LW * *", "2017-05-30", "2017-05-31")]

    [TestCase("0 30 17 LW * *", "2017-09-29 17:45", "2017-10-31 17:30")]

    [TestCase("* * * L-1W * *", "2017-01-01", "2017-01-30")]
    [TestCase("* * * L-2W * *", "2017-01-01", "2017-01-30")]
    [TestCase("* * * L-3W * *", "2017-01-01", "2017-01-27")]
    [TestCase("* * * L-4W * *", "2017-01-01", "2017-01-27")]

    [TestCase("* * * L-0W * *", "2016-02-01", "2016-02-29")]
    [TestCase("* * * L-0W * *", "2017-02-01", "2017-02-28")]
    [TestCase("* * * L-1W * *", "2016-02-01", "2016-02-29")]
    [TestCase("* * * L-1W * *", "2017-02-01", "2017-02-27")]
    [TestCase("* * * L-2W * *", "2016-02-01", "2016-02-26")]
    [TestCase("* * * L-2W * *", "2017-02-01", "2017-02-27")]
    [TestCase("* * * L-3W * *", "2016-02-01", "2016-02-26")]
    [TestCase("* * * L-3W * *", "2017-02-01", "2017-02-24")]

    // Support '?'.

    [TestCase("* * * ? 11 *", "2016-10-09", "2016-11-01")]

    [TestCase("? ? ? ? ? ?", "2016-12-09 16:46", "2016-12-09 16:46")]
    [TestCase("* * * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
    [TestCase("* * * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
    [TestCase("* * * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
    [TestCase("* * * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
    [TestCase("* * * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
    [TestCase("* * * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]

    // Day of 100-year and not 400-year.
    [TestCase("* * * * * *", "1900-02-20 16:46", "1900-02-20 16:46")]

    // Day of 400-year
    [TestCase("* * * * * *", "2000-02-28 16:46", "2000-02-28 16:46")]

    // Last day of 400-year.
    [TestCase("* * * * * *", "2000-12-31 16:46", "2000-12-31 16:46")]

    // Case insensitive.
    [TestCase("* *  *  lw   * *   ", "2017-05-30", "2017-05-31")]
    [TestCase("* *  *  l-0w * *   ", "2016-02-01", "2016-02-29")]
    [TestCase("0 30 17 27w  * 1l  ", "2017-03-10 17:45", "2017-03-27 17:30")]
    [TestCase("0 30 17 27w  * mOnL", "2017-03-10 17:45", "2017-03-27 17:30")]

    // Complex expressions
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:00", "2017-04-17 17:20")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:21", "2017-04-17 17:30")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:31", "2017-04-17 17:32")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:33", "2017-04-17 17:34")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:35", "2017-04-17 17:40")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:41", "2017-04-17 17:50")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:51", "2017-04-17 17:57")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:58", "2017-04-17 17:58")]
    [TestCase("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:59", "2017-04-17 18:20")]
    public void GetNextOccurrence_ReturnsCorrectDate(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

        var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

        Assert.AreEqual(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
    }


    [TestCase(true, 00001)]
    [TestCase(true, 09999)]
    [TestCase(false, 0001)]
    [TestCase(false, 9999)]
    public void GetNextOccurrence_RoundsFromUtcUpToTheSecond(bool inclusiveFrom, int extraTicks)
    {
        var expression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);
        var fromUtc = new DateTime(2017, 07, 20, 11, 59, 59, DateTimeKind.Utc).AddTicks(extraTicks);

        var occurrence = expression.GetNextOccurrence(fromUtc, inclusive: inclusiveFrom);

        Assert.AreEqual(new DateTime(2017, 07, 20, 12, 0, 0, DateTimeKind.Utc), occurrence);
    }
    // 2016-03-13 is date when the clock jumps forward from 1:59 am -05:00 standard time (ST) to 3:00 am -04:00 DST in Eastern Time Zone.
    // ________1:59 ST///invalid///3:00 DST________

    // Run missed.

    [TestCase("0 */30 *      *  *  *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 */30 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 1-58 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 0,30 0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 */30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 0,30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 */30 2      13 03 *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 0,30 02     13 03 *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 30   2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 0    */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
    [TestCase("0 30   0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]

    [TestCase("0 0,59 *      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 01:59 -05:00", true)]
    [TestCase("0 0,59 *      *  *  *    ", "2016-03-13 03:00 -04:00", "2016-03-13 03:00 -04:00", true)]
                                                                                           
    [TestCase("0 30   *      *  3  SUN#2", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]

    [TestCase("0 */30 *      *  *  *    ", "2016-03-13 01:30 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 */30 */2    *  *  *    ", "2016-03-13 01:30 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 1-58 */2    *  *  *    ", "2016-03-13 01:58 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 0,30 0-23/2 *  *  *    ", "2016-03-13 00:30 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 0,30 2      *  *  *    ", "2016-03-12 02:30 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 */30 2      13 03 *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 0,30 02     13 03 *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 30   2      *  *  *    ", "2016-03-12 02:30 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 0    */2    *  *  *    ", "2016-03-13 00:00 -05:00", "2016-03-13 03:00 -04:00", false)]
    [TestCase("0 30   0-23/2 *  *  *    ", "2016-03-13 00:30 -05:00", "2016-03-13 03:00 -04:00", false)]

    [TestCase("0 0,59 *      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]

    [TestCase("0 30   *      *  3  SUN#2", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]
    public void GetNextOccurrence_HandleDST_WhenTheClockJumpsForward_And_TimeZoneIsEst(string cronExpression, string fromString, string expectedString, bool inclusive)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }
    // 2017-03-31 00:00 is time in Jordan Time Zone when the clock jumps forward
    // from 2017-03-30 23:59:59.9999999 +02:00 standard time (ST) to 01:00:00.0000000 am +03:00 DST.
    // ________23:59:59.9999999 ST///invalid///01:00:00.0000000 DST________

    // Run missed.

    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.9999999 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.9999000 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.9990000 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.9900000 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.9000000 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-30 23:59:59.0000000 +02:00", "2017-03-31 01:00:00 +03:00", false)]
    // [TestCase("30 0 L  * *", "2017-03-31 01:00:00.0000001 +02:00", "2017-04-30 00:30:00 +03:00", true)]
    // public void GetNextOccurrence_HandleDST_WhenTheClockJumpsForwardAndFromIsAroundDST(string cronExpression, string fromString, string expectedString, bool inclusive)
    // {
    //     var expression = CronExpression.Parse(cronExpression);
    //
    //     var fromInstant = GetInstant(fromString);
    //     var expectedInstant = GetInstant(expectedString);
    //
    //     var executed = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive);
    //
    //     Assert.AreEqual(expectedInstant, executed);
    //     Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    // }
    
    // 2017-05-14 00:00 is time in Chile Time Zone when the clock jumps backward
    // from 2017-05-13 23:59:59.9999999 -03:00 standard time (ST) to 2017-05-13 23:00:00.0000000 am -04:00 DST .
    // ________23:59:59.9999999 -03:00 ST -> 23:00:00.0000000 -04:00 DST

    [TestCase("30 23 * * *", "2017-05-13 23:59:59.9999999 -03:00", "2017-05-14 23:30:00 -04:00", false)]
    [TestCase("30 23 * * *", "2017-05-13 23:59:59.9999000 -03:00", "2017-05-14 23:30:00 -04:00", false)]
    [TestCase("30 23 * * *", "2017-05-13 23:59:59.9990000 -03:00", "2017-05-14 23:30:00 -04:00", false)]
    [TestCase("30 23 * * *", "2017-05-13 23:59:59.9900000 -03:00", "2017-05-14 23:30:00 -04:00", false)]
    [TestCase("30 23 * * *", "2017-05-13 23:59:59.9000000 -03:00", "2017-05-14 23:30:00 -04:00", false)]
    [TestCase("30 23 * * *", "2017-05-13 23:59:59.0000000 -03:00", "2017-05-14 23:30:00 -04:00", false)]

    [TestCase("30 23 * * *", "2017-05-14 00:00:00.0000001 -04:00", "2017-05-14 23:30:00 -04:00", true)]
    public void GetNextOccurrence_HandleDST_WhenTheClockJumpsBackwardAndFromIsAroundDST(string cronExpression, string fromString, string expectedString, bool inclusive)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, PacificTimeZone, inclusive);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }


    [TestCase("0  7 * * *", "2020-05-20 07:00:00.0000001 -04:00", "2020-05-21 07:00:00 -04:00", true)]
    [TestCase("0  7 * * *", "2020-05-20 07:00:00.0000001 -04:00", "2020-05-21 07:00:00 -04:00", false)]

    [TestCase("0  7 * * *", "2023-08-12 07:00:00.9999999 -04:00", "2023-08-13 07:00:00 -04:00", true)]
    [TestCase("0  7 * * *", "2023-08-12 07:00:00.9999999 -04:00", "2023-08-13 07:00:00 -04:00", false)]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsNotRoundAndZoneIsSpecified(string cronExpression, string fromString, string expectedString, bool inclusive)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }
    // 2017-10-01 is date when the clock jumps forward from 1:59 am +10:30 standard time (ST) to 2:30 am +11:00 DST on Lord Howe.
    // ________1:59 ST///invalid///2:30 DST________

    // Run missed.

    [TestCase("0 */30 *      *  *  *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 */30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 1-58 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 0,30 0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 */30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 0,30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 */30 2      01 10 *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 0,30 02     01 10 *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 30   2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 0,30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    [TestCase("0 30   0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]

    [TestCase("0 0,30,59 *      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 01:59 +10:30")]
    [TestCase("0 0,30,59 *      *  *  *    ", "2017-10-01 02:30 +11:00", "2017-10-01 02:30 +11:00")]

    [TestCase("0 30   *      *  10 SUN#1", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
    public void GetNextOccurrence_HandleDST_WhenTheClockTurnForwardHalfHour(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, LordHoweTimeZone, inclusive: true);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }
    // 2016-11-06 is date when the clock jumps backward from 2:00 am -04:00 DST to 1:00 am -05:00 ST in Eastern Time Zone.
    // _______1:00 DST____1:59 DST -> 1:00 ST____2:00 ST_______

    // Run at 2:00 ST because 2:00 DST is invalid.
    [TestCase("0 */30 */2 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 02:00 -05:00", true)]
    [TestCase("0 0    */2 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]
    [TestCase("0 0    0/2 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]
    [TestCase("0 0    2-3 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]

    // Run twice due to intervals.
    [TestCase("0 */30 *   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:15 -05:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:45 -05:00", "2016-11-06 02:00 -05:00", true)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:00 -05:00", "2016-11-06 01:30 -05:00", false)]
    [TestCase("0 */30 *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 02:00 -05:00", false)]

    [TestCase("0 30   *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 30   *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 30   *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]
    [TestCase("0 30   *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 02:30 -05:00", false)]

    [TestCase("0 30   */1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 30   */1  * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 30   0/1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 30   0/1  * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 30   */1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]
    [TestCase("0 30   0/1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]

    [TestCase("0 30   1-9 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 30   1-9 * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 30   1-9 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]

    [TestCase("0 */30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 */30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 */30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
    [TestCase("0 */30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 */30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 */30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]

    [TestCase("0 0/30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -05:00", true)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]
    [TestCase("0 0/30 1   * * *", "2016-11-06 01:00 -05:00", "2016-11-06 01:30 -05:00", false)]

    [TestCase("0 0-30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:20 -04:00", true)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:20 -05:00", true)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:01 -04:00", false)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:21 -04:00", false)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", false)]
    [TestCase("0 0-30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:21 -05:00", false)]

    [TestCase("*/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:30 -04:00", true)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:30 -05:00", true)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:30 -04:00", false)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:30 -05:00", false)]
    [TestCase("*/30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

    [TestCase("0/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:30 -04:00", true)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:30 -05:00", true)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:30 -04:00", false)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:30 -05:00", false)]
    [TestCase("0/30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

    [TestCase("0-30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:01 -04:00", true)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:01 -05:00", true)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:01 -04:00", false)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:01 -05:00", false)]
    [TestCase("0-30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

    // Duplicates skipped due to certain time.
    [TestCase("0 0,30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0,30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 0,30 1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
    [TestCase("0 0,30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 0,30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

    [TestCase("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0,30 1   * 1/2 *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
    [TestCase("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 0,30 1   * 1/2 *", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

    [TestCase("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
    [TestCase("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
    [TestCase("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
    [TestCase("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

    [TestCase("0 0    1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0    1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
    [TestCase("0 0    1   * * *", "2016-11-06 01:00 -04:00", "2016-11-07 01:00 -05:00", false)]

    [TestCase("0 0    1   6 11 *", "2015-11-07 01:00 -05:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0    1   6 11 *", "2015-11-07 01:00 -05:00", "2016-11-06 01:00 -04:00", false)]

    [TestCase("0 0    1   * 11 SUN#1", "2015-11-01 01:00 -05:00", "2016-11-06 01:00 -04:00", true)]
    [TestCase("0 0    1   * 11 SUN#1", "2015-11-01 01:00 -05:00", "2016-11-06 01:00 -04:00", false)]

    // Run at 02:00 ST because 02:00 doesn't exist in DST.

    [TestCase("0 0 2 * * *", "2016-11-06 01:45 -04:00", "2016-11-06 02:00 -05:00", false)]
    [TestCase("0 0 2 * * *", "2016-11-06 01:45 -05:00", "2016-11-06 02:00 -05:00", false)]
    public void GetNextOccurrence_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string fromString, string expectedString, bool inclusive)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }

    [Test]
    public void GetNextOccurrence_HandlesBorderConditions_WhenDSTEnds()
    {
        var expression = CronExpression.Parse("59 59 01 * * *", CronFormat.IncludeSeconds);

        var from = new DateTimeOffset(2016, 11, 06, 02, 00, 00, 00, TimeSpan.FromHours(-5)).AddTicks(-1);

        var executed = expression.GetNextOccurrence(from, EasternTimeZone, inclusive: true);

        Assert.AreEqual(new DateTimeOffset(2016, 11, 07, 01, 59, 59, 00, TimeSpan.FromHours(-5)), executed);
        Assert.AreEqual(TimeSpan.FromHours(-5), executed?.Offset);
    }
    // 2017-04-02 is date when the clock jumps backward from 2:00 am -+11:00 DST to 1:30 am +10:30 ST on Lord Howe.
    // _______1:30 DST____1:59 DST -> 1:30 ST____2:00 ST_______

    // Run at 2:00 ST because 2:00 DST is invalid.
    [TestCase("0 */30 */2 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 02:00 +10:30")]
    [TestCase("0 0    */2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
    [TestCase("0 0    0/2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
    [TestCase("0 0    2-3 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]

    // Run twice due to intervals.
    [TestCase("0 */30 *   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 */30 *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]
    [TestCase("0 */30 *   * * *", "2017-04-02 01:15 +10:30", "2017-04-02 01:30 +10:30")]

    [TestCase("0 30   *   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 30   *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("0 30   */1 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 30   */1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]
    [TestCase("0 30   0/1 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 30   0/1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("0 30   1-9 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 30   1-9 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("0 */30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 */30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 */30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("0 0/30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 0/30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 0/30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("0 0-30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 0-30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:20 +11:00")]
    [TestCase("0 0-30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

    [TestCase("*/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
    [TestCase("*/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
    [TestCase("*/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
    [TestCase("*/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
    [TestCase("*/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

    [TestCase("0/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
    [TestCase("0/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
    [TestCase("0/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
    [TestCase("0/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
    [TestCase("0/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

    [TestCase("0-30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
    [TestCase("0-30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:01 +11:00")]
    [TestCase("0-30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
    [TestCase("0-30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:01 +10:30")]
    [TestCase("0-30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

    // Duplicates skipped due to certain time.
    [TestCase("0 0,30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 0,30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 0,30 1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

    [TestCase("0 0,30 1   * 2/2 *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 0,30 1   * 2/2 *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 0,30 1   * 2/2 *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

    [TestCase("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
    [TestCase("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

    [TestCase("0 30    1   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
    [TestCase("0 30    1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:30 +10:30")]
    public void GetNextOccurrence_HandleDST_WhenTheClockJumpsBackwardAndDeltaIsNotHour(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var executed = expression.GetNextOccurrence(fromInstant, LordHoweTimeZone, inclusive: true);

        Assert.AreEqual(expectedInstant, executed);
        Assert.AreEqual(expectedInstant.Offset, executed?.Offset);
    }


    [TestCase("* * * * * *", "15:30", "15:30")]
    [TestCase("0 5 * * * *", "00:00", "00:05")]

    // Dst doesn't affect result.

    [TestCase("0 */30 * * * *", "2016-03-12 23:15", "2016-03-12 23:30")]
    [TestCase("0 */30 * * * *", "2016-03-12 23:45", "2016-03-13 00:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 00:15", "2016-03-13 00:30")]
    [TestCase("0 */30 * * * *", "2016-03-13 00:45", "2016-03-13 01:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 01:45", "2016-03-13 02:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 02:15", "2016-03-13 02:30")]
    [TestCase("0 */30 * * * *", "2016-03-13 02:45", "2016-03-13 03:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 03:15", "2016-03-13 03:30")]
    [TestCase("0 */30 * * * *", "2016-03-13 03:45", "2016-03-13 04:00")]

    [TestCase("0 */30 * * * *", "2016-11-05 23:10", "2016-11-05 23:30")]
    [TestCase("0 */30 * * * *", "2016-11-05 23:50", "2016-11-06 00:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 00:10", "2016-11-06 00:30")]
    [TestCase("0 */30 * * * *", "2016-11-06 00:50", "2016-11-06 01:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:10", "2016-11-06 01:30")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:50", "2016-11-06 02:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 02:10", "2016-11-06 02:30")]
    [TestCase("0 */30 * * * *", "2016-11-06 02:50", "2016-11-06 03:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 03:10", "2016-11-06 03:30")]
    [TestCase("0 */30 * * * *", "2016-11-06 03:50", "2016-11-06 04:00")]
    public void GetNextOccurrence_ReturnsCorrectUtcDateTimeOffset(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);
        var expectedInstant = GetInstantFromLocalTime(expectedString, TimeZoneInfo.Utc);

        var occurrence = expression.GetNextOccurrence(fromInstant, TimeZoneInfo.Utc, inclusive: true);

        Assert.AreEqual(expectedInstant, occurrence);
        Assert.AreEqual(expectedInstant.Offset, occurrence?.Offset);
    }
    // Dst doesn't affect result.

    [TestCase("0 */30 * * * *", "2016-03-12 23:15 -05:00", "2016-03-12 23:30 -05:00")]
    [TestCase("0 */30 * * * *", "2016-03-12 23:45 -05:00", "2016-03-13 00:00 -05:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 00:15 -05:00", "2016-03-13 00:30 -05:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 00:45 -05:00", "2016-03-13 01:00 -05:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 03:15 -04:00", "2016-03-13 03:30 -04:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 03:45 -04:00", "2016-03-13 04:00 -04:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 04:15 -04:00", "2016-03-13 04:30 -04:00")]
    [TestCase("0 */30 * * * *", "2016-03-13 04:45 -04:00", "2016-03-13 05:00 -04:00")]

    [TestCase("0 */30 * * * *", "2016-11-05 23:10 -04:00", "2016-11-05 23:30 -04:00")]
    [TestCase("0 */30 * * * *", "2016-11-05 23:50 -04:00", "2016-11-06 00:00 -04:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 00:10 -04:00", "2016-11-06 00:30 -04:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 00:50 -04:00", "2016-11-06 01:00 -04:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:10 -04:00", "2016-11-06 01:30 -04:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:50 -04:00", "2016-11-06 01:00 -05:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:10 -05:00", "2016-11-06 01:30 -05:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 01:50 -05:00", "2016-11-06 02:00 -05:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 02:10 -05:00", "2016-11-06 02:30 -05:00")]
    [TestCase("0 */30 * * * *", "2016-11-06 02:50 -05:00", "2016-11-06 03:00 -05:00")]
    public void GetNextOccurrence_ReturnsCorrectDateTimeOffset(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstant(fromString);
        var expectedInstant = GetInstant(expectedString);

        var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

        Assert.AreEqual(expectedInstant, occurrence);
        Assert.AreEqual(expectedInstant.Offset, occurrence?.Offset);
    }


    // [TestCase("30 0 L  * *", "2017-03-30 23:59 +02:00", "2017-03-31 01:00 +03:00")]
    // [TestCase("30 0 L  * *", "2017-03-31 01:00 +03:00", "2017-04-30 00:30 +03:00")]
    // [TestCase("30 0 LW * *", "2018-03-29 23:59 +02:00", "2018-03-30 01:00 +03:00")]
    // [TestCase("30 0 LW * *", "2018-03-30 01:00 +03:00", "2018-04-30 00:30 +03:00")]
    // public void GetNextOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsForwardOnFriday(string cronExpression, string fromString, string expectedString)
    // {
    //     var expression = CronExpression.Parse(cronExpression);
    //
    //     var fromInstant = GetInstant(fromString);
    //     var expectedInstant = GetInstant(expectedString);
    //
    //     var occurrence = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive: true);
    //
    //     // TODO: Rounding error.
    //     if (occurrence?.Millisecond == 999)
    //     {
    //         occurrence = occurrence.Value.AddMilliseconds(1);
    //     }
    //
    //     Assert.AreEqual(expectedInstant, occurrence);
    //     Assert.AreEqual(expectedInstant.Offset, occurrence?.Offset);
    // }
    
    // [TestCase("30 0 L  * *", "2014-10-31 00:30 +02:00", "2014-11-30 00:30 +02:00")]
    // [TestCase("30 0 L  * *", "2014-10-31 00:30 +03:00", "2014-10-31 00:30 +03:00")]
    // [TestCase("30 0 LW * *", "2015-10-30 00:30 +02:00", "2015-11-30 00:30 +02:00")]
    // [TestCase("30 0 LW * *", "2015-10-30 00:30 +03:00", "2015-10-30 00:30 +03:00")]
    // [TestCase("30 0 29 * *", "2019-03-28 23:59 +02:00", "2019-03-29 01:00 +03:00")]
    // public void GetNextOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsBackwardOnFriday(string cronExpression, string fromString, string expectedString)
    // {
    //     var expression = CronExpression.Parse(cronExpression);
    //
    //     var fromInstant = GetInstant(fromString);
    //     var expectedInstant = GetInstant(expectedString);
    //
    //     var occurrence = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive: true);
    //
    //     // TODO: Rounding error.
    //     if (occurrence?.Millisecond == 999)
    //     {
    //         occurrence = occurrence.Value.AddMilliseconds(1);
    //     }
    //
    //     Assert.AreEqual(expectedInstant, occurrence);
    //     Assert.AreEqual(expectedInstant.Offset, occurrence?.Offset);
    // }


    [TestCaseSource(nameof(GetTimeZones))]
    public void GetNextOccurrence_ReturnsTheSameDateTimeWithGivenTimeZoneOffset(string zoneId)
    {
        var fromInstant = new DateTimeOffset(2017, 03, 04, 00, 00, 00, new TimeSpan(12, 30, 00));
        var expectedInstant = fromInstant;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var expectedOffset = zone.GetUtcOffset(expectedInstant);

        var occurrence = MinutelyExpression.GetNextOccurrence(fromInstant, zone, inclusive: true);

        Assert.AreEqual(expectedInstant, occurrence);
        Assert.AreEqual(expectedOffset, occurrence?.Offset);
    }

    [TestCaseSource(nameof(GetTimeZones))]
    public void GetNextOccurrence_ReturnsUtcDateTime(string zoneId)
    {
        var from = new DateTime(2017, 03, 06, 00, 00, 00, DateTimeKind.Utc);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var occurrence = MinutelyExpression.GetNextOccurrence(from, zone, inclusive: true);

        Assert.AreEqual(from, occurrence);
        Assert.AreEqual(DateTimeKind.Utc, occurrence?.Kind);
    }
    [TestCase("* * 30    2    *    ", "1970-01-01")]
    [TestCase("* * 30-31 2    *    ", "1970-01-01")]
    [TestCase("* * 31    2    *    ", "1970-01-01")]
    [TestCase("* * 31    4    *    ", "1970-01-01")]
    [TestCase("* * 31    6    *    ", "1970-01-01")]
    [TestCase("* * 31    9    *    ", "1970-01-01")]
    [TestCase("* * 31    11   *    ", "1970-01-01")]
    [TestCase("* * L-30  11   *    ", "1970-01-01")]
    [TestCase("* * L-29  2    *    ", "1970-01-01")]
    [TestCase("* * L-30  2    *    ", "1970-01-01")]

    [TestCase("* * 1     *    SUN#2", "1970-01-01")]
    [TestCase("* * 7     *    SUN#2", "1970-01-01")]
    [TestCase("* * 1     *    SUN#3", "1970-01-01")]
    [TestCase("* * 14    *    SUN#3", "1970-01-01")]
    [TestCase("* * 1     *    SUN#4", "1970-01-01")]
    [TestCase("* * 21    *    SUN#4", "1970-01-01")]
    [TestCase("* * 1     *    SUN#5", "1970-01-01")]
    [TestCase("* * 28    *    SUN#5", "1970-01-01")]
    [TestCase("* * 1-28  *    SUN#5", "1970-01-01")]

    [TestCase("* * 8     *    MON#1", "1970-01-01")]
    [TestCase("* * 31    *    MON#1", "1970-01-01")]
    [TestCase("* * 15    *    TUE#2", "1970-01-01")]
    [TestCase("* * 31    *    TUE#2", "1970-01-01")]
    [TestCase("* * 22    *    WED#3", "1970-01-01")]
    [TestCase("* * 31    *    WED#3", "1970-01-01")]
    [TestCase("* * 29    *    THU#4", "1970-01-01")]
    [TestCase("* * 31    *    THU#4", "1970-01-01")]
                                
    [TestCase("* * 21    *    7L   ", "1970-01-01")]
    [TestCase("* * 21    *    0L   ", "1970-01-01")]
    [TestCase("* * 11    *    0L   ", "1970-01-01")]
    [TestCase("* * 1     *    0L   ", "1970-01-01")]
                                
    [TestCase("* * L     *    SUN#1", "1970-01-01")]
    [TestCase("* * L     *    SUN#2", "1970-01-01")]
    [TestCase("* * L     *    SUN#3", "1970-01-01")]
    [TestCase("* * L     1    SUN#4", "1970-01-01")]
    [TestCase("* * L     3-12 SUN#4", "1970-01-01")]
                           
    [TestCase("* * L-1   2    SUN#5", "1970-01-01")]
    [TestCase("* * L-2   4    SUN#5", "1970-01-01")]
    [TestCase("* * L-3   *    SUN#5", "1970-01-01")]
    [TestCase("* * L-10  *    SUN#4", "1970-01-01")]
                           
    [TestCase("* * 1W    *    SUN  ", "1970-01-01")]
    [TestCase("* * 4W    *    0    ", "1970-01-01")]
    [TestCase("* * 7W    *    7    ", "1970-01-01")]
    [TestCase("* * 5W    *    SAT  ", "1970-01-01")]
                           
    [TestCase("* * 14W   *    6#2  ", "1970-01-01")]
                           
    [TestCase("* * 7W    *    FRI#2", "1970-01-01")]
    [TestCase("* * 14W   *    TUE#3", "1970-01-01")]
    [TestCase("* * 11W   *    MON#3", "1970-01-01")]
    [TestCase("* * 21W   *    TUE#4", "1970-01-01")]
    [TestCase("* * 28W   *    SAT#5", "1970-01-01")]
                           
    [TestCase("* * 21W   *    0L   ", "1970-01-01")]
    [TestCase("* * 19W   *    1L   ", "1970-01-01")]
    [TestCase("* * 1W    *    1L   ", "1970-01-01")]
    [TestCase("* * 21W   *    2L   ", "1970-01-01")]
    [TestCase("* * 2W    *    2L   ", "1970-01-01")]
    [TestCase("* * 21W   *    3L   ", "1970-01-01")]
    [TestCase("* * 3W    *    3L   ", "1970-01-01")]
    [TestCase("* * 21W   *    4L   ", "1970-01-01")]
    [TestCase("* * 4W    *    4L   ", "1970-01-01")]
    [TestCase("* * 21W   *    5L   ", "1970-01-01")]
    [TestCase("* * 5W    *    5L   ", "1970-01-01")]
    [TestCase("* * 21W   *    6L   ", "1970-01-01")]
    [TestCase("* * 21W   *    7L   ", "1970-01-01")]
                           
    [TestCase("* * LW    *    SUN  ", "1970-01-01")]
    [TestCase("* * LW    *    0    ", "1970-01-01")]
    [TestCase("* * LW    *    0L   ", "1970-01-01")]
    [TestCase("* * LW    *    SAT  ", "1970-01-01")]
    [TestCase("* * LW    *    6    ", "1970-01-01")]
    [TestCase("* * LW    *    6L   ", "1970-01-01")]
                           
    [TestCase("* * LW    *    1#1  ", "1970-01-01")]
    [TestCase("* * LW    *    2#2  ", "1970-01-01")]
    [TestCase("* * LW    *    3#3  ", "1970-01-01")]
    [TestCase("* * LW    1    4#4  ", "1970-01-01")]
    [TestCase("* * LW    3-12 4#4  ", "1970-01-01")]
    public void GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachable(string cronExpression, string fromString)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

        var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

        Assert.IsNull(occurrence);
    }


    [TestCase("* * 30   2  *", "2080-01-01")]
    [TestCase("* * L-30 11 *", "2080-01-01")]
    public void GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachableAndFromIsDateTime(string cronExpression, string fromString)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);

        var occurrence = expression.GetNextOccurrence(fromInstant.UtcDateTime);

        Assert.IsNull(occurrence);
    }

    [Test]
    public void GetNextOccurrence_ReturnsNull_WhenOutOfMaxRange()
    {
        var expression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);

        var lastFullSecondInDateTime = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var occurrence = expression.GetNextOccurrence(lastFullSecondInDateTime);

        Assert.IsNull(occurrence);
    }

    [Test]
    public void GetNextOccurrence_VeryLastResult()
    {
        var expression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);

        var lastFullSecondInDateTime = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var oneTickBeforeLastSecond = lastFullSecondInDateTime - TimeSpan.FromTicks(1);

        var occurrence = expression.GetNextOccurrence(oneTickBeforeLastSecond);

        Assert.AreEqual(lastFullSecondInDateTime, occurrence);
    }
    // Basic facts.

    [TestCase("* * * * *", "17:35", "17:35")]

    [TestCase("* * * * *", "17:35:01", "17:36:00")]
    [TestCase("* * * * *", "17:35:59", "17:36:00")]
    [TestCase("* * * * *", "17:36:00", "17:36:00")]

    // Minute specified.

    [TestCase("12    * * * *", "15:05", "15:12")]
    [TestCase("12    * * * *", "15:12", "15:12")]
    [TestCase("12    * * * *", "15:59", "16:12")]
    [TestCase("31-39 * * * *", "15:00", "15:31")]
    [TestCase("31-39 * * * *", "15:30", "15:31")]
    [TestCase("31-39 * * * *", "15:31", "15:31")]
    [TestCase("31-39 * * * *", "15:39", "15:39")]
    [TestCase("31-39 * * * *", "15:59", "16:31")]
    [TestCase("*/20  * * * *", "15:00", "15:00")]
    [TestCase("*/20  * * * *", "15:10", "15:20")]
    [TestCase("*/20  * * * *", "15:59", "16:00")]
    [TestCase("10/5  * * * *", "15:00", "15:10")]
    [TestCase("10/5  * * * *", "15:14", "15:15")]
    [TestCase("10/5  * * * *", "15:59", "16:10")]
    [TestCase("0     * * * *", "15:59", "16:00")]

    [TestCase("44 * * * *", "19:44:01", "20:44:00")]
    [TestCase("44 * * * *", "19:44:30", "20:44:00")]
    [TestCase("44 * * * *", "19:44:59", "20:44:00")]
    [TestCase("44 * * * *", "19:45:00", "20:44:00")]

    [TestCase("5-8,19,20,35-41 * * * *", "15:01", "15:05")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:06", "15:06")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:18", "15:19")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:19", "15:19")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:20", "15:20")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:21", "15:35")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:36", "15:36")]
    [TestCase("5-8,19,20,35-41 * * * *", "15:42", "16:05")]

    [TestCase("51-4 * * * *", "17:35", "17:51")]
    [TestCase("51-4 * * * *", "17:51", "17:51")]
    [TestCase("51-4 * * * *", "17:55", "17:55")]
    [TestCase("51-4 * * * *", "17:59", "17:59")]
    [TestCase("51-4 * * * *", "18:00", "18:00")]
    [TestCase("51-4 * * * *", "18:04", "18:04")]
    [TestCase("51-4 * * * *", "18:05", "18:51")]

    [TestCase("56-4/4 * * * *", "17:55", "17:56")]
    [TestCase("56-4/4 * * * *", "17:57", "18:00")]
    [TestCase("56-4/4 * * * *", "18:01", "18:04")]
    [TestCase("56-4/4 * * * *", "18:05", "18:56")]

    [TestCase("45-44 * * * *", "18:45", "18:45")]
    [TestCase("45-44 * * * *", "18:55", "18:55")]
    [TestCase("45-44 * * * *", "18:59", "18:59")]
    [TestCase("45-44 * * * *", "19:00", "19:00")]
    [TestCase("45-44 * * * *", "19:44", "19:44")]

    // Hour specified.

    [TestCase("* 11   * * *", "10:59", "11:00")]
    [TestCase("* 11   * * *", "11:30", "11:30")]
    [TestCase("* 3-22 * * *", "01:40", "03:00")]
    [TestCase("* 3-22 * * *", "11:40", "11:40")]
    [TestCase("* */2  * * *", "00:00", "00:00")]
    [TestCase("* */2  * * *", "01:00", "02:00")]
    [TestCase("* 4/5  * * *", "00:45", "04:00")]
    [TestCase("* 4/5  * * *", "04:14", "04:14")]
    [TestCase("* 4/5  * * *", "05:00", "09:00")]

    [TestCase("* 3-5,10,11,13-17 * * *", "01:55", "03:00")]
    [TestCase("* 3-5,10,11,13-17 * * *", "04:55", "04:55")]
    [TestCase("* 3-5,10,11,13-17 * * *", "06:10", "10:00")]
    [TestCase("* 3-5,10,11,13-17 * * *", "10:55", "10:55")]
    [TestCase("* 3-5,10,11,13-17 * * *", "11:25", "11:25")]
    [TestCase("* 3-5,10,11,13-17 * * *", "12:30", "13:00")]
    [TestCase("* 3-5,10,11,13-17 * * *", "17:30", "17:30")]

    [TestCase("* 23-3/2 * * *", "17:30", "23:00")]
    [TestCase("* 23-3/2 * * *", "00:30", "01:00")]
    [TestCase("* 23-3/2 * * *", "02:00", "03:00")]
    [TestCase("* 23-3/2 * * *", "04:00", "23:00")]

    [TestCase("* 23-22 * * *", "22:10", "22:10")]
    [TestCase("* 23-22 * * *", "23:10", "23:10")]
    [TestCase("* 23-22 * * *", "00:10", "00:10")]
    [TestCase("* 23-22 * * *", "07:10", "07:10")]

    // Day of month specified.

    [TestCase("* * 9     * *", "2016-11-01", "2016-11-09")]
    [TestCase("* * 9     * *", "2016-11-09", "2016-11-09")]
    [TestCase("* * 09    * *", "2016-11-10", "2016-12-09")]
    [TestCase("* * */4   * *", "2016-12-01", "2016-12-01")]
    [TestCase("* * */4   * *", "2016-12-02", "2016-12-05")]
    [TestCase("* * */4   * *", "2016-12-06", "2016-12-09")]
    [TestCase("* * */3   * *", "2016-12-02", "2016-12-04")]
    [TestCase("* * 10,20 * *", "2016-12-09", "2016-12-10")]
    [TestCase("* * 10,20 * *", "2016-12-12", "2016-12-20")]
    [TestCase("* * 16-23 * *", "2016-12-01", "2016-12-16")]
    [TestCase("* * 16-23 * *", "2016-12-16", "2016-12-16")]
    [TestCase("* * 16-23 * *", "2016-12-18", "2016-12-18")]
    [TestCase("* * 16-23 * *", "2016-12-23", "2016-12-23")]
    [TestCase("* * 16-23 * *", "2016-12-24", "2017-01-16")]

    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-01", "2016-12-05")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-05", "2016-12-05")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-06", "2016-12-06")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-08", "2016-12-08")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-09", "2016-12-19")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-20", "2016-12-20")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-21", "2016-12-28")]
    [TestCase("* * 5-8,19,20,28-29 * *", "2016-12-30", "2017-01-05")]
    [TestCase("* * 5-8,19,20,29-30 * *", "2017-02-27", "2017-03-05")]

    [TestCase("* * 30-31 * *", "2016-02-27", "2016-03-30")]
    [TestCase("* * 30-31 * *", "2017-02-27", "2017-03-30")]
    [TestCase("* * 31    * *", "2017-04-27", "2017-05-31")]

    [TestCase("* * 20-5/5 * *", "2017-05-19", "2017-05-20")]
    [TestCase("* * 20-5/5 * *", "2017-05-21", "2017-05-25")]
    [TestCase("* * 20-5/5 * *", "2017-05-26", "2017-05-30")]
    [TestCase("* * 20-5/5 * *", "2017-06-01", "2017-06-04")]
    [TestCase("* * 20-5/5 * *", "2017-06-05", "2017-06-20")]

    [TestCase("* * 20-5/5 * *", "2017-07-01", "2017-07-04")]

    [TestCase("* * 20-5/5 * *", "2018-02-26", "2018-03-04")]

    // Month specified.

    [TestCase("* * * 11      *", "2016-10-09", "2016-11-01")]
    [TestCase("* * * 11      *", "2016-11-02", "2016-11-02")]
    [TestCase("* * * 11      *", "2016-12-02", "2017-11-01")]
    [TestCase("* * * 3,9     *", "2016-01-09", "2016-03-01")]
    [TestCase("* * * 3,9     *", "2016-06-09", "2016-09-01")]
    [TestCase("* * * 3,9     *", "2016-10-09", "2017-03-01")]
    [TestCase("* * * 5-11    *", "2016-01-01", "2016-05-01")]
    [TestCase("* * * 5-11    *", "2016-05-07", "2016-05-07")]
    [TestCase("* * * 5-11    *", "2016-07-12", "2016-07-12")]
    [TestCase("* * * 05-11   *", "2016-12-13", "2017-05-01")]
    [TestCase("* * * DEC     *", "2016-08-09", "2016-12-01")]
    [TestCase("* * * mar-dec *", "2016-02-09", "2016-03-01")]
    [TestCase("* * * mar-dec *", "2016-04-09", "2016-04-09")]
    [TestCase("* * * mar-dec *", "2016-12-09", "2016-12-09")]
    [TestCase("* * * */4     *", "2016-01-09", "2016-01-09")]
    [TestCase("* * * */4     *", "2016-02-09", "2016-05-01")]
    [TestCase("* * * */3     *", "2016-12-09", "2017-01-01")]
    [TestCase("* * * */5     *", "2016-12-09", "2017-01-01")]
    [TestCase("* * * APR-NOV *", "2016-12-09", "2017-04-01")]

    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-01-01", "2016-02-01")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-02-10", "2016-02-10")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-03-01", "2016-03-01")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-05-20", "2016-06-01")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-06-10", "2016-06-10")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-07-05", "2016-07-05")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-08-15", "2016-09-01")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-11-25", "2016-11-25")]
    [TestCase("* * * 2-4,JUN,7,SEP-nov *", "2016-12-01", "2017-02-01")]

    [TestCase("* * * 12-2 *", "2016-05-19", "2016-12-01")]
    [TestCase("* * * 12-2 *", "2017-01-19", "2017-01-19")]
    [TestCase("* * * 12-2 *", "2017-02-19", "2017-02-19")]
    [TestCase("* * * 12-2 *", "2017-03-19", "2017-12-01")]

    [TestCase("* * * 9-8/3 *", "2016-07-19", "2016-09-01")]
    [TestCase("* * * 9-8/3 *", "2016-10-19", "2016-12-01")]
    [TestCase("* * * 9-8/3 *", "2017-01-19", "2017-03-01")]
    [TestCase("* * * 9-8/3 *", "2017-04-19", "2017-06-01")]

    // Day of week specified.

    // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
    //                                           2016-12-01    2016-12-02    2016-12-03    2016-12-04
    // 2016-12-05    2016-12-06    2016-12-07    2016-12-08    2016-12-09    2016-12-10    2016-12-11
    // 2016-12-12    2016-12-13    2016-12-14    2016-12-15    2016-12-16    2016-12-17    2016-12-18

    [TestCase("* * * * 5      ", "2016-12-07", "2016-12-09")]
    [TestCase("* * * * 5      ", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * 05     ", "2016-12-10", "2016-12-16")]
    [TestCase("* * * * 3,5,7  ", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * 3,5,7  ", "2016-12-10", "2016-12-11")]
    [TestCase("* * * * 3,5,7  ", "2016-12-12", "2016-12-14")]
    [TestCase("* * * * 4-7    ", "2016-12-08", "2016-12-08")]
    [TestCase("* * * * 4-7    ", "2016-12-10", "2016-12-10")]
    [TestCase("* * * * 4-7    ", "2016-12-11", "2016-12-11")]
    [TestCase("* * * * 4-07   ", "2016-12-12", "2016-12-15")]
    [TestCase("* * * * FRI    ", "2016-12-08", "2016-12-09")]
    [TestCase("* * * * tue/2  ", "2016-12-09", "2016-12-10")]
    [TestCase("* * * * tue/2  ", "2016-12-11", "2016-12-13")]
    [TestCase("* * * * FRI/3  ", "2016-12-03", "2016-12-09")]
    [TestCase("* * * * thu-sat", "2016-12-04", "2016-12-08")]
    [TestCase("* * * * thu-sat", "2016-12-09", "2016-12-09")]
    [TestCase("* * * * thu-sat", "2016-12-10", "2016-12-10")]
    [TestCase("* * * * thu-sat", "2016-12-12", "2016-12-15")]
    [TestCase("* * * * */5    ", "2016-12-08", "2016-12-09")]
    [TestCase("* * * * */5    ", "2016-12-10", "2016-12-11")]
    [TestCase("* * * * */5    ", "2016-12-12", "2016-12-16")]
    [TestCase("* * ? * thu-sun", "2016-12-09", "2016-12-09")]

    [TestCase("* * ? * sat-tue", "2016-12-10", "2016-12-10")]
    [TestCase("* * ? * sat-tue", "2016-12-11", "2016-12-11")]
    [TestCase("* * ? * sat-tue", "2016-12-12", "2016-12-12")]
    [TestCase("* * ? * sat-tue", "2016-12-13", "2016-12-13")]
    [TestCase("* * ? * sat-tue", "2016-12-14", "2016-12-17")]

    [TestCase("* * ? * sat-tue/2", "2016-12-10", "2016-12-10")]
    [TestCase("* * ? * sat-tue/2", "2016-12-11", "2016-12-12")]
    [TestCase("* * ? * sat-tue/2", "2016-12-12", "2016-12-12")]
    [TestCase("* * ? * sat-tue/2", "2016-12-13", "2016-12-17")]

    [TestCase("00 00 11 12 0  ", "2016-12-07", "2016-12-11")]
    [TestCase("00 00 11 12 7  ", "2016-12-09", "2016-12-11")]
    [TestCase("00 00 11 12 SUN", "2016-12-10", "2016-12-11")]
    [TestCase("00 00 11 12 sun", "2016-12-09", "2016-12-11")]

    // All fields are specified.

    [TestCase("47    17    09   12    5    ", "2016-10-01 00:00", "2016-12-09 17:47")]
    [TestCase("47    17    09   DEC   FRI  ", "2016-07-05 00:00", "2016-12-09 17:47")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-01 00:00", "2016-12-09 15:40")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40", "2016-12-09 15:40")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:45", "2016-12-09 15:45")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:51", "2016-12-09 16:40")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 21:50", "2016-12-10 15:40")]
    [TestCase("40-50 15-20 5-10 11,12 5,6,7", "2016-12-11 21:50", "2017-11-05 15:40")]

    // Friday the thirteenth.

    [TestCase("05    18    13   01    05   ", "2016-01-01 00:00", "2017-01-13 18:05")]
    [TestCase("05    18    13   *     05   ", "2016-01-01 00:00", "2016-05-13 18:05")]
    [TestCase("05    18    13   *     05   ", "2016-09-01 00:00", "2017-01-13 18:05")]
    [TestCase("05    18    13   *     05   ", "2017-02-01 00:00", "2017-10-13 18:05")]

    // Handle moving to next second, minute, hour, month, year.

    [TestCase("0 * * * *", "2017-01-14 12:59", "2017-01-14 13:00")]
    [TestCase("0 0 * * *", "2017-01-14 23:00", "2017-01-15 00:00")]

    [TestCase("0 0 1 * *", "2016-02-10 00:00", "2016-03-01 00:00")]
    [TestCase("0 0 1 * *", "2017-02-10 00:00", "2017-03-01 00:00")]
    [TestCase("0 0 1 * *", "2017-04-10 00:00", "2017-05-01 00:00")]
    [TestCase("0 0 1 * *", "2017-01-30 00:00", "2017-02-01 00:00")]
    [TestCase("0 0 * * *", "2017-12-31 23:59", "2018-01-01 00:00")]

    // Skip month if day of month is specified and month has less days.

    [TestCase("0 0 30 * *", "2017-02-25 00:00", "2017-03-30 00:00")]
    [TestCase("0 0 31 * *", "2017-02-25 00:00", "2017-03-31 00:00")]
    [TestCase("0 0 31 * *", "2017-04-01 00:00", "2017-05-31 00:00")]

    // Leap year.

    [TestCase("0 0 29 2 *", "2016-03-10 00:00", "2020-02-29 00:00")]

    // Support 'L' character in day of month field.

    [TestCase("* * L * *", "2016-01-05", "2016-01-31")]
    [TestCase("* * L * *", "2016-01-31", "2016-01-31")]
    [TestCase("* * L * *", "2016-02-05", "2016-02-29")]
    [TestCase("* * L * *", "2016-02-29", "2016-02-29")]
    [TestCase("* * L 2 *", "2016-02-29", "2016-02-29")]
    [TestCase("* * L * *", "2017-02-28", "2017-02-28")]
    [TestCase("* * L * *", "2016-03-05", "2016-03-31")]
    [TestCase("* * L * *", "2016-03-31", "2016-03-31")]
    [TestCase("* * L * *", "2016-04-05", "2016-04-30")]
    [TestCase("* * L * *", "2016-04-30", "2016-04-30")]
    [TestCase("* * L * *", "2016-05-05", "2016-05-31")]
    [TestCase("* * L * *", "2016-05-31", "2016-05-31")]
    [TestCase("* * L * *", "2016-06-05", "2016-06-30")]
    [TestCase("* * L * *", "2016-06-30", "2016-06-30")]
    [TestCase("* * L * *", "2016-07-05", "2016-07-31")]
    [TestCase("* * L * *", "2016-07-31", "2016-07-31")]
    [TestCase("* * L * *", "2016-08-05", "2016-08-31")]
    [TestCase("* * L * *", "2016-08-31", "2016-08-31")]
    [TestCase("* * L * *", "2016-09-05", "2016-09-30")]
    [TestCase("* * L * *", "2016-09-30", "2016-09-30")]
    [TestCase("* * L * *", "2016-10-05", "2016-10-31")]
    [TestCase("* * L * *", "2016-10-31", "2016-10-31")]
    [TestCase("* * L * *", "2016-11-05", "2016-11-30")]
    [TestCase("* * L * *", "2016-12-05", "2016-12-31")]
    [TestCase("* * L * *", "2016-12-31", "2016-12-31")]
    [TestCase("* * L * *", "2099-12-05", "2099-12-31")]
    [TestCase("* * L * *", "2099-12-31", "2099-12-31")]

    [TestCase("* * L-1 * *", "2016-01-01", "2016-01-30")]
    [TestCase("* * L-1 * *", "2016-01-29", "2016-01-30")]
    [TestCase("* * L-1 * *", "2016-01-30", "2016-01-30")]
    [TestCase("* * L-1 * *", "2016-01-31", "2016-02-28")]
    [TestCase("* * L-1 * *", "2016-02-01", "2016-02-28")]
    [TestCase("* * L-1 * *", "2016-02-28", "2016-02-28")]
    [TestCase("* * L-1 * *", "2017-02-01", "2017-02-27")]
    [TestCase("* * L-1 * *", "2017-02-27", "2017-02-27")]
    [TestCase("* * L-1 * *", "2016-04-01", "2016-04-29")]
    [TestCase("* * L-1 * *", "2016-04-29", "2016-04-29")]
    [TestCase("* * L-1 * *", "2016-12-01", "2016-12-30")]

    [TestCase("* * L-2 * *", "2016-01-05", "2016-01-29")]
    [TestCase("* * L-2 * *", "2016-01-30", "2016-02-27")]
    [TestCase("* * L-2 * *", "2016-02-01", "2016-02-27")]
    [TestCase("* * L-2 * *", "2017-02-01", "2017-02-26")]
    [TestCase("* * L-2 * *", "2016-04-01", "2016-04-28")]
    [TestCase("* * L-2 * *", "2016-12-01", "2016-12-29")]
    [TestCase("* * L-2 * *", "2016-12-29", "2016-12-29")]
    [TestCase("* * L-2 * *", "2016-12-30", "2017-01-29")]

    [TestCase("* * L-28 * *", "2016-01-01", "2016-01-03")]
    [TestCase("* * L-28 * *", "2016-04-01", "2016-04-02")]
    [TestCase("* * L-28 * *", "2016-02-01", "2016-02-01")]
    [TestCase("* * L-28 * *", "2017-02-01", "2017-03-03")]

    [TestCase("* * L-29 * *", "2016-01-01", "2016-01-02")]
    [TestCase("* * L-29 * *", "2016-04-01", "2016-04-01")]
    [TestCase("* * L-29 * *", "2016-02-01", "2016-03-02")]
    [TestCase("* * L-29 * *", "2017-02-01", "2017-03-02")]

    [TestCase("* * L-30 * *", "2016-01-01", "2016-01-01")]
    [TestCase("* * L-30 * *", "2016-04-01", "2016-05-01")]
    [TestCase("* * L-30 * *", "2016-02-01", "2016-03-01")]
    [TestCase("* * L-30 * *", "2017-02-01", "2017-03-01")]

    // Support 'L' character in day of week field.

    // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
    // 2016-01-23    2016-01-24    2016-01-25    2016-01-26    2016-01-27    2016-01-28    2016-01-29
    // 2016-01-30    2016-01-31

    [TestCase("* * * * 0L", "2017-01-29", "2017-01-29")]
    [TestCase("* * * * 0L", "2017-01-01", "2017-01-29")]
    [TestCase("* * * * 1L", "2017-01-30", "2017-01-30")]
    [TestCase("* * * * 1L", "2017-01-01", "2017-01-30")]
    [TestCase("* * * * 2L", "2017-01-31", "2017-01-31")]
    [TestCase("* * * * 2L", "2017-01-01", "2017-01-31")]
    [TestCase("* * * * 3L", "2017-01-25", "2017-01-25")]
    [TestCase("* * * * 3L", "2017-01-01", "2017-01-25")]
    [TestCase("* * * * 4L", "2017-01-26", "2017-01-26")]
    [TestCase("* * * * 4L", "2017-01-01", "2017-01-26")]
    [TestCase("* * * * 5L", "2017-01-27", "2017-01-27")]
    [TestCase("* * * * 5L", "2017-01-01", "2017-01-27")]
    [TestCase("* * * * 6L", "2017-01-28", "2017-01-28")]
    [TestCase("* * * * 6L", "2017-01-01", "2017-01-28")]
    [TestCase("* * * * 7L", "2017-01-29", "2017-01-29")]
    [TestCase("* * * * 7L", "2016-12-31", "2017-01-29")]

    // Support '#' in day of week field.

    [TestCase("* * * * SUN#1", "2017-01-01", "2017-01-01")]
    [TestCase("* * * * 0#1  ", "2017-01-01", "2017-01-01")]
    [TestCase("* * * * 0#1  ", "2016-12-10", "2017-01-01")]
    [TestCase("* * * * 0#1  ", "2017-02-01", "2017-02-05")]
    [TestCase("* * * * 0#2  ", "2017-01-01", "2017-01-08")]
    [TestCase("* * * * 0#2  ", "2017-01-08", "2017-01-08")]
    [TestCase("* * * * 5#3  ", "2017-01-01", "2017-01-20")]
    [TestCase("* * * * 5#3  ", "2017-01-21", "2017-02-17")]
    [TestCase("* * * * 3#2  ", "2017-01-01", "2017-01-11")]
    [TestCase("* * * * 2#5  ", "2017-02-01", "2017-05-30")]

    // Support 'W' in day of month field.

    [TestCase("* * 1W * *", "2017-01-01", "2017-01-02")]
    [TestCase("* * 2W * *", "2017-01-02", "2017-01-02")]
    [TestCase("* * 6W * *", "2017-01-02", "2017-01-06")]
    [TestCase("* * 7W * *", "2017-01-02", "2017-01-06")]
    [TestCase("* * 7W * *", "2017-01-07", "2017-02-07")]
    [TestCase("* * 8W * *", "2017-01-02", "2017-01-09")]

    [TestCase("* * 30W * *", "2017-04-27", "2017-04-28")]
    [TestCase("* * 30W * *", "2017-04-28", "2017-04-28")]
    [TestCase("* * 30W * *", "2017-04-29", "2017-05-30")]

    [TestCase("* * 1W * *", "2017-04-01", "2017-04-03")]

    [TestCase("30    17 7W * *", "2017-01-06 17:45", "2017-02-07 17:30")]
    [TestCase("30,45 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:45")]
    [TestCase("30,55 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:55")]

    [TestCase("30    17 30W * *", "2017-04-28 17:45", "2017-05-30 17:30")]
    [TestCase("30,45 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:45")]
    [TestCase("30,55 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:55")]

    [TestCase("30    17 30W * *", "2017-02-06 00:00", "2017-03-30 17:30")]

    [TestCase("30    17 31W * *", "2018-03-30 17:45", "2018-05-31 17:30")]
    [TestCase("30    17 15W * *", "2016-12-30 17:45", "2017-01-16 17:30")]

    // Support 'LW' in day of month field.

    [TestCase("* * LW * *", "2017-01-01", "2017-01-31")]
    [TestCase("* * LW * *", "2017-09-01", "2017-09-29")]
    [TestCase("* * LW * *", "2017-09-29", "2017-09-29")]
    [TestCase("* * LW * *", "2017-09-30", "2017-10-31")]
    [TestCase("* * LW * *", "2017-04-01", "2017-04-28")]
    [TestCase("* * LW * *", "2017-04-28", "2017-04-28")]
    [TestCase("* * LW * *", "2017-04-29", "2017-05-31")]
    [TestCase("* * LW * *", "2017-05-30", "2017-05-31")]

    [TestCase("30 17 LW * *", "2017-09-29 17:45", "2017-10-31 17:30")]

    [TestCase("* * L-1W * *", "2017-01-01", "2017-01-30")]
    [TestCase("* * L-2W * *", "2017-01-01", "2017-01-30")]
    [TestCase("* * L-3W * *", "2017-01-01", "2017-01-27")]
    [TestCase("* * L-4W * *", "2017-01-01", "2017-01-27")]

    [TestCase("* * L-0W * *", "2016-02-01", "2016-02-29")]
    [TestCase("* * L-0W * *", "2017-02-01", "2017-02-28")]
    [TestCase("* * L-1W * *", "2016-02-01", "2016-02-29")]
    [TestCase("* * L-1W * *", "2017-02-01", "2017-02-27")]
    [TestCase("* * L-2W * *", "2016-02-01", "2016-02-26")]
    [TestCase("* * L-2W * *", "2017-02-01", "2017-02-27")]
    [TestCase("* * L-3W * *", "2016-02-01", "2016-02-26")]
    [TestCase("* * L-3W * *", "2017-02-01", "2017-02-24")]

    // Support '?'.

    [TestCase("* * ? 11 *", "2016-10-09", "2016-11-01")]

    [TestCase("? ? ? ? ?", "2016-12-09 16:46", "2016-12-09 16:46")]
    [TestCase("* * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
    [TestCase("* * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
    [TestCase("* * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
    [TestCase("* * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
    [TestCase("* * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
    [TestCase("* * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenExpressionContains5FieldsAndInclusiveIsTrue(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

        var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

        Assert.AreEqual(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
    }
    [TestCase("@every_second", "2017-03-23 16:46:05", "2017-03-23 16:46:05")]

    [TestCase("@every_minute", "2017-03-23 16:46", "2017-03-23 16:46")]
    [TestCase("@hourly      ", "2017-03-23 16:46", "2017-03-23 17:00")]
    [TestCase("@daily       ", "2017-03-23 16:46", "2017-03-24 00:00")]
    [TestCase("@midnight    ", "2017-03-23 16:46", "2017-03-24 00:00")]
    [TestCase("@monthly     ", "2017-03-23 16:46", "2017-04-01 00:00")]
    [TestCase("@yearly      ", "2017-03-23 16:46", "2018-01-01 00:00")]
    [TestCase("@annually    ", "2017-03-23 16:46", "2018-01-01 00:00")]

    // Case-insensitive.
    [TestCase("@EVERY_SECOND", "2017-03-23 16:46:05", "2017-03-23 16:46:05")]

    [TestCase("@EVERY_MINUTE", "2017-03-23 16:46", "2017-03-23 16:46")]
    [TestCase("@HOURLY      ", "2017-03-23 16:46", "2017-03-23 17:00")]
    [TestCase("@DAILY       ", "2017-03-23 16:46", "2017-03-24 00:00")]
    [TestCase("@MIDNIGHT    ", "2017-03-23 16:46", "2017-03-24 00:00")]
    [TestCase("@MONTHLY     ", "2017-03-23 16:46", "2017-04-01 00:00")]
    [TestCase("@YEARLY      ", "2017-03-23 16:46", "2018-01-01 00:00")]
    [TestCase("@ANNUALLY    ", "2017-03-23 16:46", "2018-01-01 00:00")]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenExpressionIsMacros(string cronExpression, string fromString, string expectedString)
    {
        var expression = CronExpression.Parse(cronExpression);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

        var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

        Assert.AreEqual(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
    }


    [TestCase("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
    [TestCase("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
    [TestCase("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
    [TestCase("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
    [TestCase("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
    [TestCase("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
    [TestCase("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
    [TestCase("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
    [TestCase("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
    [TestCase("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsDateTimeOffsetAndInclusiveIsFalse(string expression, string from, string expectedString)
    {
        var cronExpression = CronExpression.Parse(expression);

        var fromInstant = GetInstantFromLocalTime(from, EasternTimeZone);

        var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant, EasternTimeZone);

        Assert.AreEqual(GetInstantFromLocalTime(expectedString, EasternTimeZone), nextOccurrence);
    }


    [TestCase("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
    [TestCase("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
    [TestCase("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
    [TestCase("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
    [TestCase("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
    [TestCase("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
    [TestCase("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
    [TestCase("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
    [TestCase("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
    [TestCase("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsDateTimeAndZoneIsSpecifiedAndInclusiveIsFalse(string expression, string fromString, string expectedString)
    {
        var cronExpression = CronExpression.Parse(expression);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);
        var expectedInstant = GetInstantFromLocalTime(expectedString, EasternTimeZone);

        var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant.UtcDateTime, EasternTimeZone);

        Assert.AreEqual(expectedInstant.UtcDateTime, nextOccurrence);
    }
    [TestCase("* * * * * *", "1991-01-01 00:00")]
    [TestCase("0 * * * * *", "1991-03-02 00:00")]
    [TestCase("* 0 * * * *", "1991-03-15 00:00")]
    [TestCase("* * 0 * * *", "1991-03-31 00:00")]
    [TestCase("* * * 1 * *", "1991-04-15 00:00")]
    [TestCase("* * * * 1 *", "1991-05-25 00:00")]
    [TestCase("* * * * * 0", "1991-06-27 00:00")]
    [TestCase("0 0 0 * * *", "1991-07-16 00:00")]
    [TestCase("0 0 0 1 * *", "1991-10-30 00:00")]
    [TestCase("0 0 0 1 1 *", "1991-12-31 00:00")]
    [TestCase("0 0 0 1 * 1", "1991-12-31 00:00")]
    public void GetNextOccurrence_MakesProgressInsideLoop(string expression, string fromString)
    {
        var cronExpression = CronExpression.Parse(expression, CronFormat.IncludeSeconds);

        var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

        for (var i = 0; i < 100; i++)
        {
            var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant.AddTicks(1), EasternTimeZone, inclusive: true);

            Assert.IsTrue(nextOccurrence > fromInstant);

            fromInstant = nextOccurrence.Value;
        }
    }

    // [Test]
    // public void GetNextOccurrence_ReturnsAGreaterValue_EvenWhenMillisecondTruncationRuleIsAppliedDueToDST()
    // {
    //     var expression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);
    //     var fromInstant = DateTimeOffset.Parse("2021-03-25 23:59:59.9999999 +02:00");
    //
    //     var nextInstant = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive: true);
    //
    //     Assert.IsTrue(nextInstant > fromInstant);
    // }


    [TestCase("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
    [TestCase("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
    [TestCase("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
    [TestCase("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
    [TestCase("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
    [TestCase("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
    [TestCase("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
    [TestCase("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
    [TestCase("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
    [TestCase("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
    public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsUtcDateTimeAndInclusiveIsFalse(string expression, string fromString, string expectedString)
    {
        var cronExpression = CronExpression.Parse(expression);

        var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);
        var expectedInstant = GetInstantFromLocalTime(expectedString, TimeZoneInfo.Utc);

        var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant.UtcDateTime);

        Assert.AreEqual(expectedInstant.UtcDateTime, nextOccurrence);
    }


    [TestCase("* * * * * *", "2017-03-16 16:00:00", "2017-03-16 16:00:01")]
    [TestCase("5 * * * * *", "2017-03-16 16:00:05", "2017-03-16 16:01:05")]
    [TestCase("* 5 * * * *", "2017-03-16 16:05:00", "2017-03-16 16:05:01")]
    [TestCase("* * 5 * * *", "2017-03-16 05:00:00", "2017-03-16 05:00:01")]
    [TestCase("* * * 5 * *", "2017-03-05 16:00:00", "2017-03-05 16:00:01")]
    [TestCase("* * * * 5 *", "2017-05-16 16:00:00", "2017-05-16 16:00:01")]
    [TestCase("* * * * * 5", "2017-03-17 16:00:00", "2017-03-17 16:00:01")]
    [TestCase("5 5 * * * *", "2017-03-16 16:05:05", "2017-03-16 17:05:05")]
    [TestCase("5 5 5 * * *", "2017-03-16 05:05:05", "2017-03-17 05:05:05")]
    [TestCase("5 5 5 5 * *", "2017-03-05 05:05:05", "2017-04-05 05:05:05")]
    [TestCase("5 5 5 5 5 *", "2017-05-05 05:05:05", "2018-05-05 05:05:05")]
    [TestCase("5 5 5 5 5 5", "2017-05-05 05:05:05", "2023-05-05 05:05:05")]
    public void GetNextOccurrence_ReturnsCorrectDate_When6fieldsExpressionIsUsedAndInclusiveIsFalse(string expression, string fromString, string expectedString)
    {
        var cronExpression = CronExpression.Parse(expression, CronFormat.IncludeSeconds);

        var from = GetInstantFromLocalTime(fromString, EasternTimeZone);

        var nextOccurrence = cronExpression.GetNextOccurrence(from, EasternTimeZone);

        Assert.AreEqual(GetInstantFromLocalTime(expectedString, EasternTimeZone), nextOccurrence);
    }

    [Test]
    public void GetNextOccurrence_FromDateTimeMinValueInclusive_SuccessfullyReturned()
    {
        var expression = CronExpression.Parse("* * * * *");
        var from = new DateTime(0, DateTimeKind.Utc);

        var occurrence = expression
            .GetNextOccurrence(from, inclusive: true);

        Assert.AreEqual(from, occurrence);
    }

    [Test]
    public void GetOccurrence_PassesTests_DefinedInTheGitHubIssue53()
    {
        var jan1St2000 = new DateTimeOffset(2000, 01, 01, 00, 00, 00, new TimeSpan(-5, 0, 0));
        var expression = CronExpression.Parse("0 0 * * SAT");

        // Asking just BEFORE midnight should yield the 1st
        Assert.AreEqual(jan1St2000, expression.GetNextOccurrence(jan1St2000.AddMilliseconds(-1), EasternTimeZone));

        // Asking exactly AT midnight with inclusive `true` should should yield 1st
        Assert.AreEqual(jan1St2000, expression.GetNextOccurrence(jan1St2000, EasternTimeZone, inclusive: true));

        // Asking exactly AT midnight should skip 1st by default as non-inclusive
        Assert.AreEqual(jan1St2000.AddDays(7), expression.GetNextOccurrence(jan1St2000, EasternTimeZone));

        // Asking just AFTER midnight should yield the 8th
        Assert.AreEqual(jan1St2000.AddDays(7), expression.GetNextOccurrence(jan1St2000.AddHours(1), EasternTimeZone));

        // Asking just AFTER midnight should yield the 8th
        Assert.AreEqual(jan1St2000.AddDays(7), expression.GetNextOccurrence(jan1St2000.AddSeconds(61), EasternTimeZone));
    }

    [Test]
    public void GetOccurrences_DateTime_ThrowsAnException_WhenFromGreaterThanTo()
    {
        var expression = CronExpression.Parse("* * * * *");
        Assert.Throws<ArgumentException>(
            () => expression.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddHours(-5)).ToArray());
    }

    [Test]
    public void GetOccurrences_DateTime_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
    {
        var expression = CronExpression.Parse("* * 30 FEB *");

        var occurrences = expression.GetOccurrences(
            DateTime.UtcNow, 
            DateTime.UtcNow.AddYears(1));

        Assert.IsEmpty(occurrences);
    }

    [Test]
    public void GetOccurrences_DateTime_ReturnsCollectionThatDoesNotIncludeToByDefault()
    {
        var expression = CronExpression.Parse("* 00 26 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2))
            .ToArray();

        Assert.AreEqual(2, occurrences.Length);
        Assert.AreEqual(from, occurrences[0]);
        Assert.AreEqual(from.AddMinutes(1), occurrences[1]);
    }

    [Test]
    public void GetOccurrences_DateTime_HandlesFromExclusiveArgument()
    {
        var expression = CronExpression.Parse("* 00 26 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), fromInclusive: false)
            .ToArray();

        Assert.AreEqual(1, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(1), occurrences[0]);
    }

    [Test]
    public void GetOccurrences_DateTime_HandlesToInclusiveArgument()
    {
        var expression = CronExpression.Parse("* 00 26 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), toInclusive: true)
            .ToArray();

        Assert.AreEqual(3, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(2), occurrences[2]);
    }

    [Test]
    public void GetOccurrences_DateTimeTimeZone_ThrowsAnException_WhenFromGreaterThanTo()
    {
        var expression = CronExpression.Parse("* * * * *");
        Assert.Throws<ArgumentException>(
            () => expression.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddHours(-5), EasternTimeZone).ToArray());
    }

    [Test]
    public void GetOccurrences_DateTimeTimeZone_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
    {
        var expression = CronExpression.Parse("* * 30 FEB *");

        var occurrences = expression.GetOccurrences(
            DateTime.UtcNow, 
            DateTime.UtcNow.AddYears(1), 
            EasternTimeZone);

        Assert.IsEmpty(occurrences);
    }

    [Test]
    public void GetOccurrences_DateTimeTimeZone_ReturnsCollectionThatDoesNotIncludeToByDefault()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone)
            .ToArray();

        Assert.AreEqual(2, occurrences.Length);
        Assert.AreEqual(from, occurrences[0]);
        Assert.AreEqual(from.AddMinutes(1), occurrences[1]);
    }

    [Test]
    public void GetOccurrences_DateTimeTimeZone_HandlesFromExclusiveArgument()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, fromInclusive: false)
            .ToArray();

        Assert.AreEqual(1, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(1), occurrences[0]);
    }

    [Test]
    public void GetOccurrences_DateTimeTimeZone_HandlesToInclusiveArgument()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, toInclusive: true)
            .ToArray();

        Assert.AreEqual(3, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(2), occurrences[2]);
    }

    [Test]
    public void GetOccurrences_DateTimeOffset_ThrowsAnException_WhenFromGreaterThanTo()
    {
        var expression = CronExpression.Parse("* * * * *");
        Assert.Throws<ArgumentException>(
            () => expression.GetOccurrences(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(-5), EasternTimeZone).ToArray());
    }

    [Test]
    public void GetOccurrences_DateTimeOffset_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
    {
        var expression = CronExpression.Parse("* * 30 FEB *");

        var occurrences = expression.GetOccurrences(
            DateTimeOffset.Now, 
            DateTimeOffset.Now.AddYears(1), 
            EasternTimeZone);

        Assert.IsEmpty(occurrences);
    }

    [Test]
    public void GetOccurrences_DateTimeOffset_ReturnsCollectionThatDoesNotIncludeToByDefault()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone)
            .ToArray();

        Assert.AreEqual(2, occurrences.Length);
        Assert.AreEqual(from, occurrences[0]);
        Assert.AreEqual(from.AddMinutes(1).DateTime, occurrences[1].UtcDateTime);
    }

    [Test]
    public void GetOccurrences_DateTimeOffset_HandlesFromExclusiveArgument()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, fromInclusive: false)
            .ToArray();

        Assert.AreEqual(1, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(1).DateTime, occurrences[0].UtcDateTime);
    }

    [Test]
    public void GetOccurrences_DateTimeOffset_HandlesToInclusiveArgument()
    {
        var expression = CronExpression.Parse("* 20 25 04 *");
        var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

        var occurrences = expression
            .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, toInclusive: true)
            .ToArray();

        Assert.AreEqual(3, occurrences.Length);
        Assert.AreEqual(from.AddMinutes(2).DateTime, occurrences[2].UtcDateTime);
    }

    [Test]
    public void GetOccurrences_OccurrenceAtDateTimeMinValue_SuccessfullyReturned()
    {
        var expression = CronExpression.Parse("* * * * *");
        var from = new DateTime(0, DateTimeKind.Utc);

        var occurrences = expression
            .GetOccurrences(from, from.AddSeconds(1), fromInclusive: true)
            .ToArray();

        Assert.AreEqual(1, occurrences.Length);
        Assert.AreEqual(from, occurrences[0]);
    }


    [TestCase("* * * * *", "* * * * *")]

    [TestCase("* * * * *", "0/2,1/2    * * * *")]
    [TestCase("* * * * *", "1/2,0-59/2 * * * *")]
    [TestCase("* * * * *", "0-59       * * * *")]
    [TestCase("* * * * *", "0,1,2-59   * * * *")]
    [TestCase("* * * * *", "0-59/1     * * * *")]
    [TestCase("* * * * *", "50-49      * * * *")]

    [TestCase("* * * * *", "* 0/3,2/3,1/3 * * *")]
    [TestCase("* * * * *", "* 0-23/2,1/2  * * *")]
    [TestCase("* * * * *", "* 0-23        * * *")]
    [TestCase("* * * * *", "* 0-23/1      * * *")]
    [TestCase("* * * * *", "* 12-11       * * *")]

    [TestCase("* * * * *", "* * 1/2,2/2     * *")]
    [TestCase("* * * * *", "* * 1-31/2,2/2  * *")]
    [TestCase("* * * * *", "* * 1-31        * *")]
    [TestCase("* * * * *", "* * 1-31/1      * *")]
    [TestCase("* * * * *", "* * 5-4         * *")]

    [TestCase("* * * * *", "* * * 1/2,2/2    *")]
    [TestCase("* * * * *", "* * * 1-12/2,2/2 *")]
    [TestCase("* * * * *", "* * * 1-12       *")]
    [TestCase("* * * * *", "* * * 1-12/1     *")]
    [TestCase("* * * * *", "* * * 12-11      *")]

    [TestCase("* * * * *", "* * * * 0/2,1/2    ")]
    [TestCase("* * * * *", "* * * * SUN/2,MON/2")]
    [TestCase("* * * * *", "* * * * 0-6/2,1/2  ")]
    [TestCase("* * * * *", "* * * * 0-7/2,1/2  ")]
    [TestCase("* * * * *", "* * * * 0-7/2,MON/2")]
    [TestCase("* * * * *", "* * * * 0-6        ")]
    [TestCase("* * * * *", "* * * * 0-7        ")]
    [TestCase("* * * * *", "* * * * SUN-SAT    ")]
    [TestCase("* * * * *", "* * * * 0-6/1      ")]
    [TestCase("* * * * *", "* * * * 0-7/1      ")]
    [TestCase("* * * * *", "* * * * SUN-SAT/1  ")]
    [TestCase("* * * * *", "* * * * MON-SUN    ")]

    [TestCase("* * *     * 0  ", "* * *     * 7  ")]
    [TestCase("* * *     * 0  ", "* * *     * SUN")]
    [TestCase("* * LW    * *  ", "* * LW    * *  ")]
    [TestCase("* * L-20W * 2  ", "* * L-20W * 2  ")]
    [TestCase("* * *     * 0#1", "* * *     * 0#1")]
    [TestCase("* * *     * 0L ", "* * *     * 7L ")]
    [TestCase("* * L-3W  * 0L ", "* * L-3W  * 0L ")]
    [TestCase("1 1 1     1 1  ", "1 1 1     1 1  ")]
    [TestCase("* * *     * *  ", "* * ?     * *  ")]
    [TestCase("* * *     * *  ", "* * *     * ?  ")]

    [TestCase("1-5 * * * *", "1-5 * * * *")]
    [TestCase("1-5 * * * *", "1-5/1 * * * *")]
    [TestCase("* * * * *", "0/1 * * * *")]
    [TestCase("1 * * * *", "1-1 * * * *")]
    [TestCase("*/4 * * * *", "0-59/4 * * * *")]

    [TestCase("1-5 1-5 1-5 1-5 1-5", "1-5 1-5 1-5 1-5 1-5")]

    [TestCase("50-15 * * * *", "50-15      * * * *")]
    [TestCase("50-15 * * * *", "0-15,50-59 * * * *")]

    [TestCase("* 20-15 * * *", "* 20-15      * * *")]
    [TestCase("* 20-15 * * *", "* 0-15,20-23 * * *")]

    [TestCase("* * 20-15 * *", "* * 20-15      * *")]
    [TestCase("* * 20-15 * *", "* * 1-15,20-31 * *")]

    [TestCase("* * * 10-3 *", "* * * 10-3      *")]
    [TestCase("* * * 10-3 *", "* * * 1-3,10-12 *")]

    [TestCase("* * * * 5-2", "* * * * 5-2    ")]
    [TestCase("* * * * 5-2", "* * * * 0-2,5-7")]
    [TestCase("* * * * 5-2", "* * * * 0-2,5-6")]
    [TestCase("* * * * 5-2", "* * * * 1-2,5-7")]

    [TestCase("* * * * FRI-TUE", "* * * * FRI-TUE        ")]
    [TestCase("* * * * FRI-TUE", "* * * * SUN-TUE,FRI-SUN")]
    [TestCase("* * * * FRI-TUE", "* * * * SUN-TUE,FRI-SAT")]
    [TestCase("* * * * FRI-TUE", "* * * * MON-TUE,FRI-SUN")]
    public void Equals_ReturnsTrue_WhenCronExpressionsAreEqual(string leftExpression, string rightExpression)
    {
        var leftCronExpression = CronExpression.Parse(leftExpression);
        var rightCronExpression = CronExpression.Parse(rightExpression);

        Assert.IsTrue(leftCronExpression.Equals(rightCronExpression));
        Assert.IsTrue(leftCronExpression == rightCronExpression);
        Assert.IsFalse(leftCronExpression != rightCronExpression);
        Assert.IsTrue(leftCronExpression.GetHashCode() == rightCronExpression.GetHashCode());
    }

    [Test]
    public void Equals_ReturnsFalse_WhenOtherIsNull()
    {
        var cronExpression = CronExpression.Parse("* * * * *");

        Assert.IsFalse(cronExpression.Equals(null));
        Assert.IsFalse(cronExpression == null);
        Assert.IsTrue(cronExpression != null);
    }


    [TestCase("1 1 1 1 1", "2 1 1 1 1")]
    [TestCase("1 1 1 1 1", "1 2 1 1 1")]
    [TestCase("1 1 1 1 1", "1 1 2 1 1")]
    [TestCase("1 1 1 1 1", "1 1 1 2 1")]
    [TestCase("1 1 1 1 1", "1 1 1 1 2")]
    [TestCase("* * * * *", "1 1 1 1 1")]

    [TestCase("* * 31 1 *", "* * L    1 *")]
    [TestCase("* * L  * *", "* * LW   * *")]
    [TestCase("* * LW * *", "* * L-1W * *")]
    [TestCase("* * *  * 0", "* * L-1W * 0#1")]
    public void Equals_ReturnsFalse_WhenCronExpressionsAreNotEqual(string leftExpression, string rightExpression)
    {
        var leftCronExpression = CronExpression.Parse(leftExpression);
        var rightCronExpression = CronExpression.Parse(rightExpression);

        Assert.IsFalse(leftCronExpression.Equals(rightCronExpression));
        Assert.IsTrue(leftCronExpression != rightCronExpression);
        Assert.IsTrue(leftCronExpression.GetHashCode() != rightCronExpression.GetHashCode());
    }
    // Second specified.
    
    [TestCase("*      * * * * *", "*                * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("0      * * * * *", "                 * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("1,2    * * * * *", "1,2              * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("1-3    * * * * *", "1,2,3            * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("57-3   * * * * *", "0,1,2,3,57,58,59 * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("*/10   * * * * *", "0,10,20,30,40,50 * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("0/10   * * * * *", "0,10,20,30,40,50 * * * * *", CronFormat.IncludeSeconds)]
    [TestCase("0-20/5 * * * * *", "0,5,10,15,20     * * * * *", CronFormat.IncludeSeconds)]

    [TestCase("10,56-3/2 * * * * *", "0,2,10,56,58 * * * * *", CronFormat.IncludeSeconds)]

    // Minute specified.
    
    [TestCase("*      * * * *", "*                * * * *", CronFormat.Standard)]
    [TestCase("0      * * * *", "0                * * * *", CronFormat.Standard)]
    [TestCase("1,2    * * * *", "1,2              * * * *", CronFormat.Standard)]
    [TestCase("1-3    * * * *", "1,2,3            * * * *", CronFormat.Standard)]
    [TestCase("57-3   * * * *", "0,1,2,3,57,58,59 * * * *", CronFormat.Standard)]
    [TestCase("*/10   * * * *", "0,10,20,30,40,50 * * * *", CronFormat.Standard)]
    [TestCase("0/10   * * * *", "0,10,20,30,40,50 * * * *", CronFormat.Standard)]
    [TestCase("0-20/5 * * * *", "0,5,10,15,20     * * * *", CronFormat.Standard)]

    [TestCase("10,56-3/2 * * * *", "0,2,10,56,58 * * * *", CronFormat.Standard)]

    [TestCase("* *      * * * *", "* *                * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 0      * * * *", "* 0                * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 1,2    * * * *", "* 1,2              * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 1-3    * * * *", "* 1,2,3            * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 57-3   * * * *", "* 0,1,2,3,57,58,59 * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* */10   * * * *", "* 0,10,20,30,40,50 * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 0/10   * * * *", "* 0,10,20,30,40,50 * * * *", CronFormat.IncludeSeconds)]
    [TestCase("* 0-20/5 * * * *", "* 0,5,10,15,20     * * * *", CronFormat.IncludeSeconds)]

    [TestCase("* 10,56-3/2 * * * *", "* 0,2,10,56,58 * * * *", CronFormat.IncludeSeconds)]

    // Hour specified.
    
    [TestCase("* *      * * *", "* *             * * *", CronFormat.Standard)]
    [TestCase("* 0      * * *", "* 0             * * *", CronFormat.Standard)]
    [TestCase("* 1,2    * * *", "* 1,2           * * *", CronFormat.Standard)]
    [TestCase("* 1-3    * * *", "* 1,2,3         * * *", CronFormat.Standard)]
    [TestCase("* 22-3   * * *", "* 0,1,2,3,22,23 * * *", CronFormat.Standard)]
    [TestCase("* */10   * * *", "* 0,10,20       * * *", CronFormat.Standard)]
    [TestCase("* 0/10   * * *", "* 0,10,20       * * *", CronFormat.Standard)]
    [TestCase("* 0-20/5 * * *", "* 0,5,10,15,20  * * *", CronFormat.Standard)]

    [TestCase("* 10,22-3/2 * * *", "* 0,2,10,22    * * *", CronFormat.Standard)]

    [TestCase("* * *      * * *", "* * *             * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 0      * * *", "* * 0             * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 1,2    * * *", "* * 1,2           * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 1-3    * * *", "* * 1,2,3         * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 22-3   * * *", "* * 0,1,2,3,22,23 * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * */10   * * *", "* * 0,10,20       * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 0/10   * * *", "* * 0,10,20       * * *", CronFormat.IncludeSeconds)]
    [TestCase("* * 0-20/5 * * *", "* * 0,5,10,15,20  * * *", CronFormat.IncludeSeconds)]

    [TestCase("* * 10,22-3/2 * * *", "* * 0,2,10,22    * * *", CronFormat.IncludeSeconds)]

    // Day specified.
    
    [TestCase("* * *      * *", "* * *           * *", CronFormat.Standard)]
    [TestCase("* * 1      * *", "* * 1           * *", CronFormat.Standard)]
    [TestCase("* * 1,2    * *", "* * 1,2         * *", CronFormat.Standard)]
    [TestCase("* * 1-3    * *", "* * 1,2,3       * *", CronFormat.Standard)]
    [TestCase("* * 30-3   * *", "* * 1,2,3,30,31 * *", CronFormat.Standard)]
    [TestCase("* * */10   * *", "* * 1,11,21,31  * *", CronFormat.Standard)]
    [TestCase("* * 1/10   * *", "* * 1,11,21,31  * *", CronFormat.Standard)]
    [TestCase("* * 1-20/5 * *", "* * 1,6,11,16   * *", CronFormat.Standard)]

    [TestCase("* * L     * *", "* * L     * *", CronFormat.Standard)]
    [TestCase("* * L-0   * *", "* * L     * *", CronFormat.Standard)]
    [TestCase("* * L-10  * *", "* * L-10  * *", CronFormat.Standard)]
    [TestCase("* * LW    * *", "* * LW    * *", CronFormat.Standard)]
    [TestCase("* * L-0W  * *", "* * LW    * *", CronFormat.Standard)]
    [TestCase("* * L-10W * *", "* * L-10W * *", CronFormat.Standard)]
    [TestCase("* * 10W   * *", "* * 10W   * *", CronFormat.Standard)]

    [TestCase("* * 10,29-3/2 * *", "* * 2,10,29,31 * *", CronFormat.Standard)]

    [TestCase("* * * *      * *", "* * * *           * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 1      * *", "* * * 1           * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 1,2    * *", "* * * 1,2         * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 1-3    * *", "* * * 1,2,3       * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 30-3   * *", "* * * 1,2,3,30,31 * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * */10   * *", "* * * 1,11,21,31  * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 1/10   * *", "* * * 1,11,21,31  * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 1-20/5 * *", "* * * 1,6,11,16   * *", CronFormat.IncludeSeconds)]

    [TestCase("* * * L     * *", "* * * L     * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * L-0   * *", "* * * L     * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * L-10  * *", "* * * L-10  * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * LW    * *", "* * * LW    * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * L-0W  * *", "* * * LW    * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * L-10W * *", "* * * L-10W * *", CronFormat.IncludeSeconds)]
    [TestCase("* * * 10W   * *", "* * * 10W   * *", CronFormat.IncludeSeconds)]

    [TestCase("* * * 10,29-3/2 * *", "* * * 2,10,29,31 * *", CronFormat.IncludeSeconds)]

    // Month specified.
    
    [TestCase("* * * *      *", "* * * *           *", CronFormat.Standard)]
    [TestCase("* * * 1      *", "* * * 1           *", CronFormat.Standard)]
    [TestCase("* * * 1,2    *", "* * * 1,2         *", CronFormat.Standard)]
    [TestCase("* * * 1-3    *", "* * * 1,2,3       *", CronFormat.Standard)]
    [TestCase("* * * 11-3   *", "* * * 1,2,3,11,12 *", CronFormat.Standard)]
    [TestCase("* * * */10   *", "* * * 1,11        *", CronFormat.Standard)]
    [TestCase("* * * 1/10   *", "* * * 1,11        *", CronFormat.Standard)]
    [TestCase("* * * 1-12/5 *", "* * * 1,6,11      *", CronFormat.Standard)]
                     
    [TestCase("* * * 10,11-3/2 *", "* * * 1,3,10,11 *", CronFormat.Standard)]

    [TestCase("* * * * *      *", "* * * * *           *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 1      *", "* * * * 1           *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 1,2    *", "* * * * 1,2         *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 1-3    *", "* * * * 1,2,3       *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 11-3   *", "* * * * 1,2,3,11,12 *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * */10   *", "* * * * 1,11        *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 1/10   *", "* * * * 1,11        *", CronFormat.IncludeSeconds)]
    [TestCase("* * * * 1-12/5 *", "* * * * 1,6,11      *", CronFormat.IncludeSeconds)]

    [TestCase("* * * * 10,11-3/2 *", "* * * * 1,3,10,11 *", CronFormat.IncludeSeconds)]

    // Day of week specified.
    
    [TestCase("* * * * *    ", "* * * * *      ", CronFormat.Standard)]
    [TestCase("* * * * MON  ", "* * * * 1      ", CronFormat.Standard)]
    [TestCase("* * * * 1    ", "* * * * 1      ", CronFormat.Standard)]
    [TestCase("* * * * 1,2  ", "* * * * 1,2    ", CronFormat.Standard)]
    [TestCase("* * * * 1-3  ", "* * * * 1,2,3  ", CronFormat.Standard)]
    [TestCase("* * * * 6-1  ", "* * * * 0,1,6  ", CronFormat.Standard)]
    [TestCase("* * * * */2  ", "* * * * 0,2,4,6", CronFormat.Standard)]
    [TestCase("* * * * 0/2  ", "* * * * 0,2,4,6", CronFormat.Standard)]
    [TestCase("* * * * 1-6/5", "* * * * 1,6    ", CronFormat.Standard)]

    [TestCase("* * * * 0L ", "* * * * 0L ", CronFormat.Standard)]
    [TestCase("* * * * 5#1", "* * * * 5#1", CronFormat.Standard)]

    // ReSharper disable once StringLiteralTypo
    [TestCase("* * * * SUNL ", "* * * * 0L ", CronFormat.Standard)]
    [TestCase("* * * * FRI#1", "* * * * 5#1", CronFormat.Standard)]

    [TestCase("* * * * 3,6-2/3", "* * * * 2,3,6", CronFormat.Standard)]

    [TestCase("* * * * * *    ", "* * * * * *      ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * MON  ", "* * * * * 1      ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 1    ", "* * * * * 1      ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 1,2  ", "* * * * * 1,2    ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 1-3  ", "* * * * * 1,2,3  ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 6-1  ", "* * * * * 0,1,6  ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * */2  ", "* * * * * 0,2,4,6", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 0/2  ", "* * * * * 0,2,4,6", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 1-6/5", "* * * * * 1,6    ", CronFormat.IncludeSeconds)]

    [TestCase("* * * * * 0L ", "* * * * * 0L ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * 5#1", "* * * * * 5#1", CronFormat.IncludeSeconds)]

    // ReSharper disable once StringLiteralTypo
    [TestCase("* * * * * SUNL ", "* * * * * 0L ", CronFormat.IncludeSeconds)]
    [TestCase("* * * * * FRI#1", "* * * * * 5#1", CronFormat.IncludeSeconds)]

    [TestCase("* * * * * 3,6-2/3", "* * * * * 2,3,6", CronFormat.IncludeSeconds)]
    public void ToString_ReturnsCorrectString(string cronExpression, string expectedResult, CronFormat format)
    {
        var expression = CronExpression.Parse(cronExpression, format);

        // remove redundant spaces.
        var expectedString = Regex.Replace(expectedResult, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        
        Assert.AreEqual(expectedString, expression.ToString());
    }

    [Test]
    public void ToString_DoesNotIncludeSeconds_WhenStandardFormatPassed()
    {
        var expression = CronExpression.Parse("* * * * *");
        Assert.AreEqual("* * * * *", expression.ToString());
    }

    public static IEnumerable<string> GetTimeZones()
    {
        yield return EasternTimeZone.Id;
        yield return JordanTimeZone.Id;
        yield return TimeZoneInfo.Utc.Id;
    }

    private static DateTimeOffset GetInstantFromLocalTime(string localDateTimeString, TimeZoneInfo zone)
    {
        localDateTimeString = localDateTimeString.Trim();

        var dateTime = DateTime.ParseExact(
            localDateTimeString,
            new[]
            {
                "HH:mm:ss",
                "HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd"
            },
            CultureInfo.InvariantCulture,
            DateTimeStyles.NoCurrentDateDefault);

        var localDateTime = new DateTime(
            dateTime.Year != 1 ? dateTime.Year : Today.Year,
            dateTime.Year != 1 ? dateTime.Month : Today.Month,
            dateTime.Year != 1 ? dateTime.Day : Today.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second);

        return new DateTimeOffset(localDateTime, zone.GetUtcOffset(localDateTime));
    }

    private static DateTimeOffset GetInstant(string dateTimeOffsetString)
    {
        dateTimeOffsetString = dateTimeOffsetString.Trim();

        var dateTime = DateTimeOffset.ParseExact(
            dateTimeOffsetString,
            new[]
            {
                "yyyy-MM-dd HH:mm:ss zzz",
                "yyyy-MM-dd HH:mm zzz",
                "yyyy-MM-dd HH:mm:ss.fffffff zzz"
            },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        return dateTime;
    }
}

#endif
