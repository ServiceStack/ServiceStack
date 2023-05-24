using NUnit.Framework;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class AppTaskTests
{
    [Test]
    public void Does_ignore_non_AppTask_commands()
    {
        Assert.That(AppTasks.GetAppTaskCommands(new[] { "--configDir=/firstConfigDir", "--configDir=/secondConfigDir" }), Is.Null);
        Assert.That(AppTasks.GetAppTaskCommands(new[] { "-v", "/vol1:/vol1", "-v", "/vol2:/vol2" }), Is.Null);
    }

    [Test]
    public void Does_parse_AppTask_commands()
    {
        Assert.That(AppTasks.GetAppTaskCommands(new[] { "app.dll", "--AppTasks=task1:arg1,arg2;task2:arg1,arg2" }), 
            Is.EqualTo("task1:arg1,arg2;task2:arg1,arg2"));
        Assert.That(AppTasks.GetAppTaskCommands(new[] { "run", "--AppTasks=migrate" }), 
            Is.EqualTo("migrate"));
        Assert.That(AppTasks.GetAppTaskCommands(new[] { "run", "--AppTasks=migrate.revert:last" }), 
            Is.EqualTo("migrate.revert:last"));
    }
}