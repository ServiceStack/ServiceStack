using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.CsvTests
{
    [TestFixture]
    public class NewLineTests
    {
        [Test]
        public void Serializes_adhoc_data()
        {
            var data = new List<TableItem> {
                new TableItem { Column1Data = "I", Column2Data = "Like", Column3Data = "To", Column4Data = "Read", Column5Data = "Novels" },
                new TableItem { Column1Data = "I am", Column2Data = "Very", Column3Data = "Cool", Column4Data = "And", Column5Data = "Awesome" },
                new TableItem { Column1Data = "I", Column2Data = " Like ", Column3Data = "Reading", Column4Data = null, Column5Data = null },
                new TableItem { Column1Data = "I", Column2Data = "Don't", Column3Data = "Know,", Column4Data = "Do", Column5Data = "You?" },
                new TableItem { Column1Data = "I", Column2Data = "Saw", Column3Data = "The", Column4Data = "Movie", Column5Data = "\"Jaws\"" },
                new TableItem { Column1Data = "I", Column2Data = "Went", Column3Data = "To", Column4Data = "Space\nCamp", Column5Data = "Last\r\nYear" }
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data,Column2Data,Column3Data,Column4Data,Column5Data\r\n"
                + "I,Like,To,Read,Novels\r\n"
                + "I am,Very,Cool,And,Awesome\r\n"
                + "I, Like ,Reading,,\r\n"
                + "I,Don't,\"Know,\",Do,You?\r\n"
                + "I,Saw,The,Movie,\"\"\"Jaws\"\"\"\r\n"
                + "I,Went,To,\"Space\nCamp\",\"Last\r\nYear\"\r\n"
            ));
        }
        [TearDown]
        public void TearDown()
        {
            CsvConfig.Reset();
        }

        [Test]
        public void Serializes_adhoc_data_pipe_separator()
        {
            CsvConfig.ItemSeperatorString = "|";
            var data = new List<TableItem> {
                new TableItem { Column1Data = "I", Column2Data = "Like", Column3Data = "To", Column4Data = "Read", Column5Data = "Novels" },
                new TableItem { Column1Data = "I am", Column2Data = "Very", Column3Data = "Cool", Column4Data = "And", Column5Data = "Awesome" },
                new TableItem { Column1Data = "I", Column2Data = " Like ", Column3Data = "Reading", Column4Data = null, Column5Data = null },
                new TableItem { Column1Data = "I", Column2Data = "Don't", Column3Data = "Know,", Column4Data = "Do", Column5Data = "You?" },
                new TableItem { Column1Data = "I", Column2Data = "Saw", Column3Data = "The", Column4Data = "Movie", Column5Data = "\"Jaws\"" },
                new TableItem { Column1Data = "I", Column2Data = "Went", Column3Data = "To", Column4Data = "Space\nCamp", Column5Data = "Last\r\nYear" }
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data|Column2Data|Column3Data|Column4Data|Column5Data\r\n"
                + "I|Like|To|Read|Novels\r\n"
                + "I am|Very|Cool|And|Awesome\r\n"
                + "I| Like |Reading||\r\n"
                + "I|Don't|Know,|Do|You?\r\n"
                + "I|Saw|The|Movie|\"\"\"Jaws\"\"\"\r\n"
                + "I|Went|To|\"Space\nCamp\"|\"Last\r\nYear\"\r\n"
            ));
        }

        [Test]
        public void Serializes_adhoc_data_pipe_delimiter()
        {
            CsvConfig.ItemDelimiterString = "|";
            var data = new List<TableItem> {
                new TableItem { Column1Data = "I", Column2Data = "Like", Column3Data = "To", Column4Data = "Read", Column5Data = "Novels" },
                new TableItem { Column1Data = "I am", Column2Data = "Very", Column3Data = "Cool", Column4Data = "And", Column5Data = "Awesome" },
                new TableItem { Column1Data = "I", Column2Data = " Like ", Column3Data = "Reading", Column4Data = null, Column5Data = null },
                new TableItem { Column1Data = "I", Column2Data = "Don't", Column3Data = "Know,", Column4Data = "Do", Column5Data = "You?" },
                new TableItem { Column1Data = "I", Column2Data = "Saw", Column3Data = "The", Column4Data = "Movie", Column5Data = "\"Jaws\"" },
                new TableItem { Column1Data = "I", Column2Data = "Went", Column3Data = "To", Column4Data = "Space\nCamp", Column5Data = "Last\r\nYear" }
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data,Column2Data,Column3Data,Column4Data,Column5Data\r\n"
                + "I,Like,To,Read,Novels\r\n"
                + "I am,Very,Cool,And,Awesome\r\n"
                + "I, Like ,Reading,,\r\n"
                + "I,Don't,|Know,|,Do,You?\r\n"
                + "I,Saw,The,Movie,\"Jaws\"\r\n"
                + "I,Went,To,|Space\nCamp|,|Last\r\nYear|\r\n"
            ));
        }
    }
}