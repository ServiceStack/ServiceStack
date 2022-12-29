using NUnit.Framework;
using ServiceStack.Html;

namespace ServiceStack.Common.Tests;

public class MinifierTests
{
    [Test]
    public void Does_add_seperator_between_strings()
    {
        Assert.That(JSMinifier.MinifyJs("let a = `b`\nlet c = 1").Trim(), Is.EqualTo("let a=`b`\nlet c=1"));
        Assert.That(JSMinifier.MinifyJs("let a = 'b'\nlet c = 1").Trim(), Is.EqualTo("let a='b'\nlet c=1"));
        Assert.That(JSMinifier.MinifyJs("let a = \"b\"\nlet c = 1").Trim(), Is.EqualTo("let a=\"b\"\nlet c=1"));
    }

}