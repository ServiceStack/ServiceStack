using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class CommonStringUtilsAndViewUtilsModernizationTests
    {
        #region StringUtils Tests

        [Test]
        public void ReplaceOutsideOfQuotes_Handles_Null_And_Escaped_Quotes()
        {
            // Null returns null
            Assert.That(((string)null).ReplaceOutsideOfQuotes("{", "{{"), Is.Null);

            // Empty returns empty
            Assert.That("".ReplaceOutsideOfQuotes("{", "{{"), Is.EqualTo(""));

            // Escaped quote inside double-quoted string does not prematurely terminate quote
            var escapedInside = "{it} \"hello \\\" {it} world\" {it}";
            var replacedInside = escapedInside.ReplaceOutsideOfQuotes("{", "{{", "}", "}}");
            Assert.That(replacedInside, Is.EqualTo("{{it}} \"hello \\\" {it} world\" {{it}}"));

            // Escaped quote outside quotes does not open a quote
            var escapedOutside = "{it} \\\" {it}";
            var replacedOutside = escapedOutside.ReplaceOutsideOfQuotes("{", "{{", "}", "}}");
            Assert.That(replacedOutside, Is.EqualTo("{{it}} \\\" {{it}}"));

            // Standard replacements outside quotes
            var standard = "{a} '{a}' `{a}` {a}";
            var standardReplaced = standard.ReplaceOutsideOfQuotes("{", "{{", "}", "}}");
            Assert.That(standardReplaced, Is.EqualTo("{{a}} '{a}' `{a}` {{a}}"));
        }

        [Test]
        public void ParseTypeIntoNodes_Guards_Against_Malformed_Delimiters_And_Null()
        {
            Assert.That(((string)null).ParseTypeIntoNodes(), Is.Null);
            Assert.That("".ParseTypeIntoNodes(), Is.Null);

            // Malformed closing delimiters should not throw Stack empty
            Assert.DoesNotThrow(() => "Foo>Bar<".ParseTypeIntoNodes());
            Assert.DoesNotThrow(() => "List<string>>".ParseTypeIntoNodes());
            Assert.DoesNotThrow(() => ">>>".ParseTypeIntoNodes());

            // Valid nested generics
            var node = "Dictionary<string, List<int>>".ParseTypeIntoNodes();
            Assert.That(node.Text, Is.EqualTo("Dictionary"));
            Assert.That(node.Children.Count, Is.EqualTo(2));
            Assert.That(node.Children[0].Text, Is.EqualTo("string"));
            Assert.That(node.Children[1].Text, Is.EqualTo("List"));
            Assert.That(node.Children[1].Children.Count, Is.EqualTo(1));
            Assert.That(node.Children[1].Children[0].Text, Is.EqualTo("int"));
        }

        [Test]
        public void SnakeCaseToPascalCase_Guards_Against_Empty_And_Special_Characters()
        {
            Assert.That(StringUtils.SnakeCaseToPascalCase(null), Is.Null);
            Assert.That(StringUtils.SnakeCaseToPascalCase(""), Is.EqualTo(""));

            // Strings consisting only of special characters or underscores that become empty
            Assert.DoesNotThrow(() => StringUtils.SnakeCaseToPascalCase("???"));
            Assert.DoesNotThrow(() => StringUtils.SnakeCaseToPascalCase("___"));
            Assert.DoesNotThrow(() => StringUtils.SnakeCaseToPascalCase("   "));

            // Standard conversions
            Assert.That(StringUtils.SnakeCaseToPascalCase("first_name"), Is.EqualTo("FirstName"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("user_id_code"), Is.EqualTo("UserIdCode"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("a"), Is.EqualTo("A"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("alreadyPascal"), Is.EqualTo("AlreadyPascal"));
        }

        [Test]
        public void ToEscapedString_Guards_Against_Null()
        {
            Assert.That(((string)null).ToEscapedString(), Is.Null);
            Assert.That("hello".ToEscapedString(), Is.EqualTo("\"hello\""));
            Assert.That("line1\nline2".ToEscapedString(), Is.EqualTo("\"line1\\nline2\""));
            Assert.That("tab\tquote\"slash\\".ToEscapedString(), Is.EqualTo("\"tab\\tquote\\\"slash\\\\\""));
        }

        [Test]
        public void SplitGenericArgs_Clamps_Unmatched_Brackets()
        {
            // Unmatched close bracket first should not prevent splitting subsequent args
            var args = StringUtils.SplitGenericArgs(">arg1, arg2");
            Assert.That(args.Count, Is.EqualTo(2));
            Assert.That(args[0], Is.EqualTo(">arg1"));
            Assert.That(args[1].Trim(), Is.EqualTo("arg2"));

            // Complex generic arguments
            var nested = StringUtils.SplitGenericArgs("Dictionary<string, int>, List<double>");
            Assert.That(nested.Count, Is.EqualTo(2));
            Assert.That(nested[0].Trim(), Is.EqualTo("Dictionary<string, int>"));
            Assert.That(nested[1].Trim(), Is.EqualTo("List<double>"));
        }

        #endregion

        #region Command Tests

        [Test]
        public void Command_IndexOfMethodEnd_Parses_Aliases_With_Underscores_And_Quotes()
        {
            // Underscore alias
            var cmd1 = "COUNT(*) as total_count".ParseCommands().First();
            Assert.That(cmd1.Name, Is.EqualTo("COUNT"));
            Assert.That(cmd1.Suffix.ToString(), Is.EqualTo(" as total_count"));

            // Quoted alias with spaces
            var cmd2 = "COUNT(*) AS \"Total Items\"".ParseCommands().First();
            Assert.That(cmd2.Name, Is.EqualTo("COUNT"));
            Assert.That(cmd2.Suffix.ToString(), Is.EqualTo(" AS \"Total Items\""));

            // Bracketed alias
            var cmd3 = "SUM(Price) as [Total Price]".ParseCommands().First();
            Assert.That(cmd3.Name, Is.EqualTo("SUM"));
            Assert.That(cmd3.Suffix.ToString(), Is.EqualTo(" as [Total Price]"));

            // Tab whitespace after AS
            var cmd4 = "AVG(Score) as\ttarget_score".ParseCommands().First();
            Assert.That(cmd4.Name, Is.EqualTo("AVG"));
            Assert.That(cmd4.Suffix.ToString(), Is.EqualTo(" as\ttarget_score"));

            // Single quote alias
            var cmd5 = "MIN(Age) AS 'Min_Age'".ParseCommands().First();
            Assert.That(cmd5.Name, Is.EqualTo("MIN"));
            Assert.That(cmd5.Suffix.ToString(), Is.EqualTo(" AS 'Min_Age'"));
        }

        #endregion

        #region ViewUtils Tests

        [Test]
        public void NavLink_Emits_Valid_Html_Closing_Tags()
        {
            var navItem = new NavItem
            {
                Label = "Parent",
                Href = "/parent",
                Children = new List<NavItem>
                {
                    new() { Label = "Child1", Href = "/child1" },
                    new() { Label = "-", Href = "" },
                    new() { Label = "Child2", Href = "/child2" }
                }
            };

            var options = new NavOptions { ActivePath = "/child1" };
            var html = ViewUtils.NavLink(navItem, options);

            // Valid closing tags
            Assert.That(html, Does.Contain("</div>"));
            Assert.That(html, Does.Not.Contain("</div\n"));
            Assert.That(html, Does.Not.Contain("</div\r"));
            Assert.That(html, Does.Contain("</li>"));
            Assert.That(html, Does.Not.Contain("</lI>"));

            // Active class applied properly
            Assert.That(html, Does.Contain("active"));
        }

        [Test]
        public void ActiveClass_Guards_Against_Null_ActivePath()
        {
            var navItem = new NavItem { Label = "Home", Href = "/home" };

            // Default NavOptions has ActivePath = null
            var optionsWithNull = new NavOptions { ActivePath = null };

            // Calling NavLink with null ActivePath must not throw NullReferenceException
            string html = null;
            Assert.DoesNotThrow(() => html = ViewUtils.NavLink(navItem, optionsWithNull));
            Assert.That(html, Does.Not.Contain("active"));
        }

        [Test]
        public void TextDumpOptions_And_HtmlDumpOptions_Parse_Handles_Null_And_Non_String_Types()
        {
            // Null options dictionary returns defaults without throwing
            var textOpts = TextDumpOptions.Parse(null);
            Assert.That(textOpts, Is.Not.Null);
            Assert.That(textOpts.HeaderStyle, Is.EqualTo(TextStyle.SplitCase));

            var htmlOpts = HtmlDumpOptions.Parse(null);
            Assert.That(htmlOpts, Is.Not.Null);
            Assert.That(htmlOpts.HeaderStyle, Is.EqualTo(TextStyle.SplitCase));

            // Non-string values in id, className, headerTag must not throw InvalidCastException
            var dict = new Dictionary<string, object>
            {
                ["id"] = 12345,
                ["className"] = 999,
                ["childClass"] = 888,
                ["headerTag"] = "h3"
            };

            HtmlDumpOptions parsedHtml = null;
            Assert.DoesNotThrow(() => parsedHtml = HtmlDumpOptions.Parse(dict));
            Assert.That(parsedHtml.Id, Is.EqualTo("12345"));
            Assert.That(parsedHtml.ClassName, Is.EqualTo("999"));
            Assert.That(parsedHtml.ChildClass, Is.EqualTo("888"));
            Assert.That(parsedHtml.HeaderTag, Is.EqualTo("h3"));
        }

        [Test]
        public void ToKeyValues_Handles_Non_String_Object_Collections()
        {
            // IEnumerable<object> with integers must not throw InvalidCastException from 'from string item in list'
            var intList = new List<object> { 1, 2, 3, 4, 5 };
            var kvps = ViewUtils.ToKeyValues(intList);
            Assert.That(kvps.Count, Is.EqualTo(5));
            Assert.That(kvps[0].Key, Is.EqualTo("1"));
            Assert.That(kvps[0].Value, Is.EqualTo("1"));
            Assert.That(kvps[4].Key, Is.EqualTo("5"));
        }

        [Test]
        public void ToVarNames_And_GetParam_Guard_Against_Null()
        {
            // ToVarNames null/empty returns empty list
            Assert.That(ViewUtils.ToVarNames(null), Is.Empty);
            Assert.That(ViewUtils.ToVarNames(""), Is.Empty);
            Assert.That(ViewUtils.ToVarNames("a, b, c"), Is.EqualTo(new[] { "a", "b", "c" }));

            // GetParam with null req or null name returns null without throwing
            Assert.That(ViewUtils.GetParam(null, "param1"), Is.Null);
        }

        [Test]
        public void ViewUtils_Load_And_GetNavItems_Are_ThreadSafe()
        {
            var appSettings = new SimpleAppSettings();
            appSettings.Set(ViewUtils.NavItemsKey, new List<NavItem>
            {
                new() { Label = "Test1", Href = "/test1" }
            });
            appSettings.Set(ViewUtils.NavItemsMapKey, new Dictionary<string, List<NavItem>>
            {
                ["sidebar"] = new() { new() { Label = "Side1", Href = "/side1" } }
            });

            Assert.DoesNotThrow(() => ViewUtils.Load(appSettings));
            var items = ViewUtils.GetNavItems("sidebar");
            Assert.That(items.Count, Is.GreaterThanOrEqualTo(1));
        }

        #endregion

        #region SimpleAppSettings Tests

        [Test]
        public void SimpleAppSettings_Supports_Concurrent_Reads_And_Writes()
        {
            var settings = new SimpleAppSettings();
            settings.Set("initial", "value");

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                var idx = i;
                tasks.Add(Task.Run(() =>
                {
                    settings.Set($"key_{idx}", $"value_{idx}");
                    Assert.That(settings.Exists($"key_{idx}"), Is.True);
                    Assert.That(settings.GetString($"key_{idx}"), Is.EqualTo($"value_{idx}"));
                    var allKeys = settings.GetAllKeys();
                    Assert.That(allKeys, Is.Not.Null);
                    var all = settings.GetAll();
                    Assert.That(all, Is.Not.Null);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // GetAll returns an isolated snapshot copy
            var copy = settings.GetAll();
            copy["new_external_key"] = "test";
            Assert.That(settings.Exists("new_external_key"), Is.False);
        }

        #endregion

        #region SvgCreator Tests

        [Test]
        public void SvgCreator_Handles_Overflow_And_Nulls_Safely()
        {
            // int.MinValue would cause Math.Abs to throw OverflowException
            Assert.DoesNotThrow(() => SvgCreator.GetDarkColor(int.MinValue));
            Assert.That(SvgCreator.GetDarkColor(int.MinValue), Is.Not.Null);

            // ToDataUri null propagation
            Assert.That(SvgCreator.ToDataUri(null), Is.Null);
            Assert.That(SvgCreator.CreateGradeDataUri('A'), Does.StartWith("data:image/svg+xml,"));

            // CreateSvg with null arguments
            var svg = SvgCreator.CreateSvg('X', null, null);
            Assert.That(svg, Does.Contain("<svg"));
            Assert.That(svg, Does.Contain(">X<"));
        }

        #endregion
    }
}
