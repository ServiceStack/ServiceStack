//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//   Damian Hickey (dhickey@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;

namespace ServiceStack.Text
{
    public static class SystemTime
    {
        public static Func<DateTime> UtcDateTimeResolver;

        public static DateTime Now
        {
            get
            {
                var temp = UtcDateTimeResolver;
                return temp == null ? DateTime.Now : temp().ToLocalTime();
            }
        }

        public static DateTime UtcNow
        {
            get
            {
                var temp = UtcDateTimeResolver;
                return temp == null ? DateTime.UtcNow : temp();
            }
        }
    }
}
