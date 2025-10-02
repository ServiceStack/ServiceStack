#nullable enable
#if NET6_0_OR_GREATER
// The MIT License(MIT)
//
// Copyright (c) 2023 Hangfire OÜ
// Copyright (c) 2025 ServiceStack, Inc.
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ServiceStack.Cronos;

/// <summary>
/// A port of Cronos unsafe CronParser to Span's
/// </summary>
internal static class CronParser
{
    private const int MinNthDayOfWeek = 1;
    private const int MaxNthDayOfWeek = 5;
    private const int SundayBits = 0b1000_0001;

    public static CronExpression Parse(string expression, CronFormat format)
    {
        var span = expression.AsSpan();
        var position = 0;

        SkipWhiteSpaces(span, ref position);

        if (Accept(span, ref position, '@'))
        {
            var cronExpression = ParseMacro(span, ref position);
            SkipWhiteSpaces(span, ref position);

            if (ReferenceEquals(cronExpression, null) || !IsEndOfString(span, position))
                ThrowFormatException("Macro: Unexpected character '{0}' on position {1}.", GetCharAt(span, position), position);
            return cronExpression;
        }

        ulong second = default;
        byte nthDayOfWeek = default;
        byte lastMonthOffset = default;

        CronExpressionFlag flags = default;

        if (format == CronFormat.IncludeSeconds)
        {
            second = ParseField(CronField.Seconds, span, ref position, ref flags);
            ParseWhiteSpace(CronField.Seconds, span, ref position);
        }
        else
        {
            SetBit(ref second, CronField.Seconds.First);
        }

        var minute = ParseField(CronField.Minutes, span, ref position, ref flags);
        ParseWhiteSpace(CronField.Minutes, span, ref position);

        var hour = (uint)ParseField(CronField.Hours, span, ref position, ref flags);
        ParseWhiteSpace(CronField.Hours, span, ref position);

        var dayOfMonth = (uint)ParseDayOfMonth(span, ref position, ref flags, ref lastMonthOffset);

        ParseWhiteSpace(CronField.DaysOfMonth, span, ref position);

        var month = (ushort)ParseField(CronField.Months, span, ref position, ref flags);
        ParseWhiteSpace(CronField.Months, span, ref position);

        var dayOfWeek = (byte)ParseDayOfWeek(span, ref position, ref flags, ref nthDayOfWeek);
        ParseEndOfString(span, ref position);

        // Make sundays equivalent.
        if ((dayOfWeek & SundayBits) != 0)
        {
            dayOfWeek |= SundayBits;
        }

        return new CronExpression(
            second,
            minute,
            hour,
            dayOfMonth,
            month,
            dayOfWeek,
            nthDayOfWeek,
            lastMonthOffset,
            flags);
    }

    private static void SkipWhiteSpaces(ReadOnlySpan<char> span, ref int position)
    {
        while (position < span.Length && IsWhiteSpace(span[position])) { position++; }
    }

    private static void ParseWhiteSpace(CronField prevField, ReadOnlySpan<char> span, ref int position)
    {
        if (position >= span.Length || !IsWhiteSpace(span[position]))
            ThrowFormatException(prevField, "Unexpected character '{0}'.", GetCharAt(span, position));
        SkipWhiteSpaces(span, ref position);
    }

    private static void ParseEndOfString(ReadOnlySpan<char> span, ref int position)
    {
        if (position < span.Length && !IsWhiteSpace(span[position]) && !IsEndOfString(span, position))
            ThrowFormatException(CronField.DaysOfWeek, "Unexpected character '{0}'.", GetCharAt(span, position));

        SkipWhiteSpaces(span, ref position);
        if (!IsEndOfString(span, position))
            ThrowFormatException("Unexpected character '{0}'.", GetCharAt(span, position));
    }

    [SuppressMessage("SonarLint", "S1764:IdenticalExpressionsShouldNotBeUsedOnBothSidesOfOperators", Justification = "Expected, as the AcceptCharacter method produces side effects.")]
    private static CronExpression? ParseMacro(ReadOnlySpan<char> span, ref int position)
    {
        if (position >= span.Length) return null;

        switch (ToUpper(span[position++]))
        {
            case 'A':
                if (AcceptCharacter(span, ref position, 'N') &&
                    AcceptCharacter(span, ref position, 'N') &&
                    AcceptCharacter(span, ref position, 'U') &&
                    AcceptCharacter(span, ref position, 'A') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Yearly;
                return null;
            case 'D':
                if (AcceptCharacter(span, ref position, 'A') &&
                    AcceptCharacter(span, ref position, 'I') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Daily;
                return null;
            case 'E':
                if (AcceptCharacter(span, ref position, 'V') &&
                    AcceptCharacter(span, ref position, 'E') &&
                    AcceptCharacter(span, ref position, 'R') &&
                    AcceptCharacter(span, ref position, 'Y') &&
                    Accept(span, ref position, '_'))
                {
                    if (AcceptCharacter(span, ref position, 'M') &&
                        AcceptCharacter(span, ref position, 'I') &&
                        AcceptCharacter(span, ref position, 'N') &&
                        AcceptCharacter(span, ref position, 'U') &&
                        AcceptCharacter(span, ref position, 'T') &&
                        AcceptCharacter(span, ref position, 'E'))
                        return CronExpression.EveryMinute;

                    if (position > 0 && GetCharAt(span, position - 1) != '_') return null;

                    if (AcceptCharacter(span, ref position, 'S') &&
                        AcceptCharacter(span, ref position, 'E') &&
                        AcceptCharacter(span, ref position, 'C') &&
                        AcceptCharacter(span, ref position, 'O') &&
                        AcceptCharacter(span, ref position, 'N') &&
                        AcceptCharacter(span, ref position, 'D'))
                        return CronExpression.EverySecond;
                }

                return null;
            case 'H':
                if (AcceptCharacter(span, ref position, 'O') &&
                    AcceptCharacter(span, ref position, 'U') &&
                    AcceptCharacter(span, ref position, 'R') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Hourly;
                return null;
            case 'M':
                if (AcceptCharacter(span, ref position, 'O') &&
                    AcceptCharacter(span, ref position, 'N') &&
                    AcceptCharacter(span, ref position, 'T') &&
                    AcceptCharacter(span, ref position, 'H') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Monthly;

                if (position > 0 && ToUpper(GetCharAt(span, position - 1)) == 'M' &&
                    AcceptCharacter(span, ref position, 'I') &&
                    AcceptCharacter(span, ref position, 'D') &&
                    AcceptCharacter(span, ref position, 'N') &&
                    AcceptCharacter(span, ref position, 'I') &&
                    AcceptCharacter(span, ref position, 'G') &&
                    AcceptCharacter(span, ref position, 'H') &&
                    AcceptCharacter(span, ref position, 'T'))
                    return CronExpression.Daily;

                return null;
            case 'W':
                if (AcceptCharacter(span, ref position, 'E') &&
                    AcceptCharacter(span, ref position, 'E') &&
                    AcceptCharacter(span, ref position, 'K') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Weekly;
                return null;
            case 'Y':
                if (AcceptCharacter(span, ref position, 'E') &&
                    AcceptCharacter(span, ref position, 'A') &&
                    AcceptCharacter(span, ref position, 'R') &&
                    AcceptCharacter(span, ref position, 'L') &&
                    AcceptCharacter(span, ref position, 'Y'))
                    return CronExpression.Yearly;
                return null;
            default:
                position--;
                return null;
        }
    }

    private static ulong ParseField(CronField field, ReadOnlySpan<char> span, ref int position, ref CronExpressionFlag flags)
    {
        if (Accept(span, ref position, '*') || Accept(span, ref position, '?'))
        {
            if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;
            return ParseStar(field, span, ref position);
        }

        var num = ParseValue(field, span, ref position);

        var bits = ParseRange(field, span, ref position, num, ref flags);
        if (Accept(span, ref position, ',')) bits |= ParseList(field, span, ref position, ref flags);

        return bits;
    }

    private static ulong ParseDayOfMonth(ReadOnlySpan<char> span, ref int position, ref CronExpressionFlag flags, ref byte lastDayOffset)
    {
        var field = CronField.DaysOfMonth;

        if (Accept(span, ref position, '*') || Accept(span, ref position, '?')) return ParseStar(field, span, ref position);

        if (AcceptCharacter(span, ref position, 'L')) return ParseLastDayOfMonth(field, span, ref position, ref flags, ref lastDayOffset);

        var dayOfMonth = ParseValue(field, span, ref position);

        if (AcceptCharacter(span, ref position, 'W'))
        {
            flags |= CronExpressionFlag.NearestWeekday;
            return GetBit(dayOfMonth);
        }

        var bits = ParseRange(field, span, ref position, dayOfMonth, ref flags);
        if (Accept(span, ref position, ',')) bits |= ParseList(field, span, ref position, ref flags);

        return bits;
    }

    private static ulong ParseDayOfWeek(ReadOnlySpan<char> span, ref int position, ref CronExpressionFlag flags, ref byte nthWeekDay)
    {
        var field = CronField.DaysOfWeek;
        if (Accept(span, ref position, '*') || Accept(span, ref position, '?')) return ParseStar(field, span, ref position);

        var dayOfWeek = ParseValue(field, span, ref position);

        if (AcceptCharacter(span, ref position, 'L')) return ParseLastWeekDay(dayOfWeek, ref flags);
        if (Accept(span, ref position, '#')) return ParseNthWeekDay(field, span, ref position, dayOfWeek, ref flags, out nthWeekDay);

        var bits = ParseRange(field, span, ref position, dayOfWeek, ref flags);
        if (Accept(span, ref position, ',')) bits |= ParseList(field, span, ref position, ref flags);

        return bits;
    }

    private static ulong ParseStar(CronField field, ReadOnlySpan<char> span, ref int position)
    {
        return Accept(span, ref position, '/')
            ? ParseStep(field, span, ref position, field.First, field.Last)
            : field.AllBits;
    }

    private static ulong ParseList(CronField field, ReadOnlySpan<char> span, ref int position, ref CronExpressionFlag flags)
    {
        var num = ParseValue(field, span, ref position);
        var bits = ParseRange(field, span, ref position, num, ref flags);

        do
        {
            if (!Accept(span, ref position, ',')) return bits;

            bits |= ParseList(field, span, ref position, ref flags);
        } while (true);
    }

    private static ulong ParseRange(CronField field, ReadOnlySpan<char> span, ref int position, int low, ref CronExpressionFlag flags)
    {
        if (!Accept(span, ref position, '-'))
        {
            if (!Accept(span, ref position, '/')) return GetBit(low);

            if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;
            return ParseStep(field, span, ref position, low, field.Last);
        }

        if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;

        var high = ParseValue(field, span, ref position);
        if (Accept(span, ref position, '/')) return ParseStep(field, span, ref position, low, high);
        return GetBits(field, low, high, 1);
    }

    private static ulong ParseStep(CronField field, ReadOnlySpan<char> span, ref int position, int low, int high)
    {
        // Get the step size -- note: we don't pass the
        // names here, because the number is not an
        // element id, it's a step size.  'low' is
        // sent as a 0 since there is no offset either.
        var step = ParseNumber(field, span, ref position, 1, field.Last);
        return GetBits(field, low, high, step);
    }

    private static ulong ParseLastDayOfMonth(CronField field, ReadOnlySpan<char> span, ref int position, ref CronExpressionFlag flags, ref byte lastMonthOffset)
    {
        flags |= CronExpressionFlag.DayOfMonthLast;

        if (Accept(span, ref position, '-')) lastMonthOffset = (byte)ParseNumber(field, span, ref position, 0, field.Last - 1);
        if (AcceptCharacter(span, ref position, 'W')) flags |= CronExpressionFlag.NearestWeekday;
        return field.AllBits;
    }

    private static ulong ParseNthWeekDay(CronField field, ReadOnlySpan<char> span, ref int position, int dayOfWeek, ref CronExpressionFlag flags, out byte nthDayOfWeek)
    {
        nthDayOfWeek = (byte)ParseNumber(field, span, ref position, MinNthDayOfWeek, MaxNthDayOfWeek);
        flags |= CronExpressionFlag.NthDayOfWeek;
        return GetBit(dayOfWeek);
    }

    private static ulong ParseLastWeekDay(int dayOfWeek, ref CronExpressionFlag flags)
    {
        flags |= CronExpressionFlag.DayOfWeekLast;
        return GetBit(dayOfWeek);
    }

    private static bool Accept(ReadOnlySpan<char> span, ref int position, char character)
    {
        if (position < span.Length && span[position] == character)
        {
            position++;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AcceptCharacter(ReadOnlySpan<char> span, ref int position, char character)
    {
        if (position < span.Length && ToUpper(span[position]) == character)
        {
            position++;
            return true;
        }

        return false;
    }

    private static int ParseNumber(CronField field, ReadOnlySpan<char> span, ref int position, int low, int high)
    {
        var num = GetNumber(span, ref position, null);
        if (num == -1 || num < low || num > high)
        {
            ThrowFormatException(field, "Value must be a number between {0} and {1} (all inclusive).", low, high);
        }
        return num;
    }

    private static int ParseValue(CronField field, ReadOnlySpan<char> span, ref int position)
    {
        var num = GetNumber(span, ref position, field.Names);
        if (num == -1 || num < field.First || num > field.Last)
        {
            ThrowFormatException(field, "Value must be a number between {0} and {1} (all inclusive).", field.First, field.Last);
        }
        return num;
    }

    private static ulong GetBits(CronField field, int num1, int num2, int step)
    {
        if (num2 < num1) return GetReversedRangeBits(field, num1, num2, step);
        if (step == 1) return (1UL << (num2 + 1)) - (1UL << num1);

        return GetRangeBits(num1, num2, step);
    }

    private static ulong GetRangeBits(int low, int high, int step)
    {
        var bits = 0UL;
        for (var i = low; i <= high; i += step)
        {
            SetBit(ref bits, i);
        }
        return bits;
    }

    private static ulong GetReversedRangeBits(CronField field, int num1, int num2, int step)
    {
        var high = field.Last;
        // Skip one of sundays.
        if (field == CronField.DaysOfWeek) high--;

        var bits = GetRangeBits(num1, high, step);

        num1 = field.First + step - (high - num1) % step - 1;
        return bits | GetRangeBits(num1, num2, step);
    }

    private static ulong GetBit(int num1)
    {
        return 1UL << num1;
    }

    private static int GetNumber(ReadOnlySpan<char> span, ref int position, int[]? names)
    {
        if (position < span.Length && IsDigit(span[position]))
        {
            var num = GetNumeric(span[position++]);

            if (position >= span.Length || !IsDigit(span[position])) return num;

            num = num * 10 + GetNumeric(span[position++]);

            if (position >= span.Length || !IsDigit(span[position])) return num;
            return -1;
        }

        if (names == null) return -1;

        if (position >= span.Length || !IsLetter(span[position])) return -1;
        var buffer = ToUpper(span[position++]);

        if (position >= span.Length || !IsLetter(span[position])) return -1;
        buffer |= ToUpper(span[position++]) << 8;

        if (position >= span.Length || !IsLetter(span[position])) return -1;
        buffer |= ToUpper(span[position++]) << 16;

        var length = names.Length;

        for (var i = 0; i < length; i++)
        {
            if (buffer == names[i])
            {
                return i;
            }
        }

        return -1;
    }

    private static void SetBit(ref ulong value, int index)
    {
        value |= 1UL << index;
    }

    private static bool IsEndOfString(ReadOnlySpan<char> span, int position)
    {
        return position >= span.Length;
    }

    private static bool IsWhiteSpace(char code)
    {
        return code == '\t' || code == ' ';
    }

    private static bool IsDigit(char code)
    {
        return code >= 48 && code <= 57;
    }

    private static bool IsLetter(char code)
    {
        return (code >= 65 && code <= 90) || (code >= 97 && code <= 122);
    }

    private static int GetNumeric(char code)
    {
        return code - 48;
    }

    private static uint ToUpper(uint code)
    {
        if (code >= 97 && code <= 122)
        {
            return code - 32;
        }

        return code;
    }

    private static char GetCharAt(ReadOnlySpan<char> span, int position)
    {
        return position < span.Length ? span[position] : '\0';
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowFormatException(CronField field, string format, params object[] args)
    {
        throw new CronFormatException($"{CronFormatException.BaseMessage} {field}: {String.Format(CultureInfo.CurrentCulture, format, args)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowFormatException(string format, params object[] args)
    {
        throw new CronFormatException($"{CronFormatException.BaseMessage} {String.Format(CultureInfo.CurrentCulture, format, args)}");
    }
}
#endif