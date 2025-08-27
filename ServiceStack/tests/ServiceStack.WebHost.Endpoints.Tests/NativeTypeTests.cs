using NUnit.Framework;
using ServiceStack.NativeTypes.Python;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class NativeTypeTests
{
    [Test]
    public void Does_generate_python_enums()
    {
        Assert.That(PythonGenerator.EnumNameFormat("id"), Is.EqualTo("ID"));
        Assert.That(PythonGenerator.EnumNameFormat("id10"), Is.EqualTo("ID10"));
        Assert.That(PythonGenerator.EnumNameFormat("ID10"), Is.EqualTo("ID10"));
        Assert.That(PythonGenerator.EnumNameFormat("ID_10"), Is.EqualTo("ID_10"));
        Assert.That(PythonGenerator.EnumNameFormat("foo"), Is.EqualTo("FOO"));
        Assert.That(PythonGenerator.EnumNameFormat("fooBar"), Is.EqualTo("FOO_BAR"));
        Assert.That(PythonGenerator.EnumNameFormat("FooBar"), Is.EqualTo("FOO_BAR"));
        Assert.That(PythonGenerator.EnumNameFormat("FOO_BAR"), Is.EqualTo("FOO_BAR"));
    }
}