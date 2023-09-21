using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class CsvSerializerConfigTests
    {
        [Test]
        public void Does_use_CsvConfig()
        {
            CsvConfig.ItemSeperatorString = "|";
            CsvConfig.ItemDelimiterString = "`";
            CsvConfig.RowSeparatorString = "\n\n";

            var dtos = new[] {
                new ModelWithIdAndName { Id = 1, Name = "Value" },
                new ModelWithIdAndName { Id = 2, Name = "Value|Escaped" },
            };

            var csv = dtos.ToCsv();
            Assert.That(csv, Is.EqualTo("Id|Name\n\n1|Value\n\n2|`Value|Escaped`\n\n"));

            var maps = new List<Dictionary<string, object>>()
            {
                new() { {"Id", "1"}, {"Name", "Value"} },
                new() { {"Id", "2"}, {"Name", "Value|Escaped"} },
            };

            csv = maps.ToCsv();
            Assert.That(csv, Is.EqualTo("Id|Name\n\n1|Value\n\n2|`Value|Escaped`\n\n"));

            CsvConfig.Reset();
        }
    }
}