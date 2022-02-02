using System;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public class Table
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Int { get; set; }
        public int? NInt { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime DateTime2 { get; set; }
    }
    
    public class InspectTests
    {
        [Test]
        public void Does_not_display_markdown_table_columns_with_all_default_values()
        {
            var rows = new[] {
                new Table { Id = 1, Name = "A" },
                new Table { Id = 2, Name = "B", NInt = 0 },
                new Table { Id = 3, Name = "C", DateTime2 = DateTime.MaxValue },
            };

            var output = Inspect.dumpTable(rows);
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
| # | Id | Name | DateTime2  |
|---|----|------|------------|
| 1 |  1 | A    | 0001-01-01 |
| 2 |  2 | B    | 0001-01-01 |
| 3 |  3 | C    | 9999-12-31 |".NormalizeNewLines()));
        }

        [Test]
        public void Does_not_display_html_table_columns_with_all_default_values()
        {
            var rows = new[] {
                new Table { Id = 1, Name = "A" },
                new Table { Id = 2, Name = "B", NInt = 0 },
                new Table { Id = 3, Name = "C", DateTime2 = DateTime.MaxValue },
            };

            var output = Inspect.htmlDump(rows);
            Assert.That(output.RemoveNewLines(), Is.EqualTo(@"
<table class=""table""><thead>
<tr><th>Id</th><th>Name</th><th>DateTime2</th></tr>
</thead><tbody>
<tr><td>1</td><td>A</td><td>0001-01-01</td></tr>
<tr><td>2</td><td>B</td><td>0001-01-01</td></tr>
<tr><td>3</td><td>C</td><td>9999-12-31</td></tr>
</tbody></table>
            ".RemoveNewLines()));
        }
        
        [Test]
        public void Does_display_only_specified_markdown_table_columns()
        {
            var rows = new[] {
                new Table { Id = 1, Name = "A" },
                new Table { Id = 2, Name = "B", NInt = 0 },
                new Table { Id = 3, Name = "C", DateTime2 = DateTime.MaxValue },
            };

            var output = Inspect.dumpTable(rows, headers:new[]{ "Id", "Int", "Name" });
            output.Print();
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
| Id | Int | Name |
|----|-----|------|
|  1 |   0 | A    |
|  2 |   0 | B    |
|  3 |   0 | C    |".NormalizeNewLines()));
        }
        
        [Test]
        public void Does_display_only_specified_html_table_columns()
        {
            var rows = new[] {
                new Table { Id = 1, Name = "A" },
                new Table { Id = 2, Name = "B", NInt = 0 },
                new Table { Id = 3, Name = "C", DateTime2 = DateTime.MaxValue },
            };

            var output = Inspect.htmlDump(rows, headers:new[]{ "Id", "Int", "Name" });
            output.Print();
            Assert.That(output.RemoveNewLines(), Is.EqualTo(@"
<table class=""table""><thead>
<tr><th>Id</th><th>Int</th><th>Name</th></tr>
</thead><tbody>
<tr><td>1</td><td>0</td><td>A</td></tr>
<tr><td>2</td><td>0</td><td>B</td></tr>
<tr><td>3</td><td>0</td><td>C</td></tr>
</tbody></table>
            ".RemoveNewLines()));
        }

        
    }
}