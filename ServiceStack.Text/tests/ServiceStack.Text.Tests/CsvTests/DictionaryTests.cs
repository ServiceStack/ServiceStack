using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.CsvTests
{
    [TestFixture]
    public class DictionaryTests
    {
        [Test]
        public void Serializes_dictionary_mismatched_keys_deserializes_tabular_csv()
        {
            var data = new List<Dictionary<string, string>> {
                new Dictionary<string, string> { {"Column2Data", "Like"}, {"Column3Data", "To"}, {"Column4Data", "Read"}, {"Column5Data", "Novels"}},
                new Dictionary<string, string> { { "Column1Data", "I am" }, {"Column3Data", "Cool"}, {"Column4Data", "And"}, {"Column5Data", "Awesome"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", " Like "}, {"Column4Data", null}, {"Column5Data", null}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Don't"}, {"Column3Data", "Know,"}, {"Column5Data", "You?"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Saw"}, {"Column3Data", "The"}, {"Column4Data", "Movie"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Went"}, {"Column3Data", "To"}, {"Column4Data", "Space\nCamp"}, {"Column5Data", "Last\r\nYear"}}
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data,Column2Data,Column3Data,Column4Data,Column5Data\r\n"
                + ",Like,To,Read,Novels\r\n"
                + "I am,,Cool,And,Awesome\r\n"
                + "I, Like ,,,\r\n"
                + "I,Don't,\"Know,\",,You?\r\n"
                + "I,Saw,The,Movie,\r\n"
                + "I,Went,To,\"Space\nCamp\",\"Last\r\nYear\"\r\n"
            ));
        }

        [Test]
        public void Serializes_dictionary_data()
        {
            var data = new List<Dictionary<string, string>> {
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Like"}, {"Column3Data", "To"}, {"Column4Data", "Read"}, {"Column5Data", "Novels"}},
                new Dictionary<string, string> { { "Column1Data", "I am" }, {"Column2Data", "Very"}, {"Column3Data", "Cool"}, {"Column4Data", "And"}, {"Column5Data", "Awesome"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", " Like "}, {"Column3Data", "Reading"}, {"Column4Data", null}, {"Column5Data", null}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Don't"}, {"Column3Data", "Know,"}, {"Column4Data", "Do"}, {"Column5Data", "You?"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Saw"}, {"Column3Data", "The"}, {"Column4Data", "Movie"}, {"Column5Data", "\"Jaws\""}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Went"}, {"Column3Data", "To"}, {"Column4Data", "Space\nCamp"}, {"Column5Data", "Last\r\nYear"}}
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

        [Test]
        public void Serializes_dictionary_object_data()
        {
            var data = new List<Dictionary<string, object>>
                           {
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I"},
                                       {"Column2Data", "Like"},
                                       {"Column3Data", "To"},
                                       {"Column4Data", "Read"},
                                       {"Column5Data", 123}
                                   },
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I am"},
                                       {"Column2Data", "Very"},
                                       {"Column3Data", "Cool"},
                                       {"Column4Data", "And"},
                                       {"Column5Data", 4}
                                   },
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I"},
                                       {"Column2Data", " Like "},
                                       {"Column3Data", 2},
                                       {"Column4Data", null},
                                       {"Column5Data", null}
                                   },
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I"},
                                       {"Column2Data", "Don't"},
                                       {"Column3Data", "Know,"},
                                       {"Column4Data", "Do"},
                                       {"Column5Data", "You?"}
                                   },
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I"},
                                       {"Column2Data", "Saw"},
                                       {"Column3Data", "The"},
                                       {"Column4Data", "Movie"},
                                       {"Column5Data", "\"Jaws\""}
                                   },
                               new Dictionary<string, object>
                                   {
                                       {"Column1Data", "I"},
                                       {"Column2Data", "Went"},
                                       {"Column3Data", "To"},
                                       {"Column4Data", "Space\nCamp"},
                                       {"Column5Data", "Last\r\nYear"}
                                   }
                           };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data,Column2Data,Column3Data,Column4Data,Column5Data\r\n"
                + "I,Like,To,Read,123\r\n"
                + "I am,Very,Cool,And,4\r\n"
                + "I, Like ,2,,\r\n"
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
        public void Serializes_dictionary_data_long_delimiter()
        {
            CsvConfig.ItemDelimiterString = "^~^";
            var data = new List<Dictionary<string, string>> {
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Like"}, {"Column3Data", "To"}, {"Column4Data", "Read"}, {"Column5Data", "Novels"}},
                new Dictionary<string, string> { { "Column1Data", "I am" }, {"Column2Data", "Very"}, {"Column3Data", "Cool"}, {"Column4Data", "And"}, {"Column5Data", "Awesome"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", " Like "}, {"Column3Data", "Reading"}, {"Column4Data", null}, {"Column5Data", null}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Don't"}, {"Column3Data", "Know,"}, {"Column4Data", "Do"}, {"Column5Data", "You?"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Saw"}, {"Column3Data", "The"}, {"Column4Data", "Movie"}, {"Column5Data", "\"Jaws\""}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Went"}, {"Column3Data", "To"}, {"Column4Data", "Space\nCamp"}, {"Column5Data", "Last\r\nYear"}}
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.That(csv, Is.EqualTo(
                "Column1Data,Column2Data,Column3Data,Column4Data,Column5Data\r\n"
                + "I,Like,To,Read,Novels\r\n"
                + "I am,Very,Cool,And,Awesome\r\n"
                + "I, Like ,Reading,,\r\n"
                + "I,Don't,^~^Know,^~^,Do,You?\r\n"
                + "I,Saw,The,Movie,\"Jaws\"\r\n"
                + "I,Went,To,^~^Space\nCamp^~^,^~^Last\r\nYear^~^\r\n"
            ));
        }

        [Test]
        public void Serializes_dictionary_data_pipe_separator()
        {
            CsvConfig.ItemSeperatorString = "|";
            var data = new List<Dictionary<string, string>> {
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Like"}, {"Column3Data", "To"}, {"Column4Data", "Read"}, {"Column5Data", "Novels"}},
                new Dictionary<string, string> { { "Column1Data", "I am" }, {"Column2Data", "Very"}, {"Column3Data", "Cool"}, {"Column4Data", "And"}, {"Column5Data", "Awesome"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", " Like "}, {"Column3Data", "Reading"}, {"Column4Data", null}, {"Column5Data", null}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Don't"}, {"Column3Data", "Know,"}, {"Column4Data", "Do"}, {"Column5Data", "You?"}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Saw"}, {"Column3Data", "The"}, {"Column4Data", "Movie"}, {"Column5Data", "\"Jaws\""}},
                new Dictionary<string, string> { { "Column1Data", "I" }, {"Column2Data", "Went"}, {"Column3Data", "To"}, {"Column4Data", "Space\nCamp"}, {"Column5Data", "Last\r\nYear"}}
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
    }
}
