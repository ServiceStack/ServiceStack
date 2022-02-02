//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Text
{
    public static class CsvStreamExtensions
    {
        public static void WriteCsv<T>(this Stream outputStream, IEnumerable<T> records)
        {
            using (var textWriter = new StreamWriter(outputStream))
            {
                textWriter.WriteCsv(records);
            }
        }

        public static void WriteCsv<T>(this TextWriter writer, IEnumerable<T> records)
        {
            CsvWriter<T>.Write(writer, records);
        }

    }
}