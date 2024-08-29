#if NET6_0_OR_GREATER    
#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class CommandsTests
{
    public record class RetryState(int Standard, int LinearBackoff, int ExponentialBackoff, int FullJitterBackoff);
    
    [Test]
    public void Calculate_retry_per_policy()
    {
        RetryPolicy policy = new(
            Times:3, Behavior:RetryBehavior.Default, DelayMs:100, MaxDelayMs:60_000, DelayFirst:false);
        
        var stats = 10.Times(i => i + 1)
            .Map(n => new RetryState(
                Standard:ExecUtils.CalculateRetryDelayMs(n, policy with { Behavior = RetryBehavior.Standard }),
                LinearBackoff:ExecUtils.CalculateRetryDelayMs(n, policy with { Behavior = RetryBehavior.LinearBackoff }),
                ExponentialBackoff:ExecUtils.CalculateRetryDelayMs(n, policy with { Behavior = RetryBehavior.ExponentialBackoff }),
                FullJitterBackoff:ExecUtils.CalculateRetryDelayMs(n, policy with { Behavior = RetryBehavior.FullJitterBackoff })));
        
        stats.PrintDumpTable();
    }

    private static CommandsFeature CreateCommandsFeature()
    {
        var feature = new CommandsFeature
        {
            Log = NullLogger<CommandsFeature>.Instance,
        };
        return feature;
    }

    [Test]
    public async Task Does_not_retry_command_without_Retry_attribute()
    {
        var feature = CreateCommandsFeature();
        var command = new FailNoRetryCommand();
        await feature.ExecuteCommandAsync(command, new FailedRequest());
        Assert.That(command.Attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task  Does_retry_command_3_times_with_default_Retry_attribute()
    {
        var feature = CreateCommandsFeature();
        var command = new FailDefaultRetryCommand();
        await feature.ExecuteCommandAsync(command, new FailedRequest());
        var retryAttempts = 3;
        var executed = 1 + retryAttempts;
        Assert.That(command.Attempts, Is.EqualTo(executed));
    }

    [Test]
    public async Task Does_execute_FailTimes1Command_2_times_with_Retry_1_attribute()
    {
        var feature = CreateCommandsFeature();
        var command = new FailTimes1Command();
        await feature.ExecuteCommandAsync(command, new FailedRequest());
        var retryAttempts = 1;
        var executed = 1 + retryAttempts;
        Assert.That(command.Attempts, Is.EqualTo(executed));
    }

    [Test]
    public async Task Does_execute_FailTimes4Command_5_times_with_Retry_4_attribute()
    {
        var feature = CreateCommandsFeature();
        var command = new FailTimes4Command();
        await feature.ExecuteCommandAsync(command, new FailedRequest());
        var retryAttempts = 4;
        var executed = 1 + retryAttempts;
        Assert.That(command.Attempts, Is.EqualTo(executed));

        var commandResults = feature.CommandFailures.ToList();
        Assert.That(commandResults.Map(x => x.Attempt), Is.EquivalentTo(new[]{ 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task Can_execute_command_with_Result()
    {
        var feature = CreateCommandsFeature();
        var command = new AddContactCommand();
        var result = await feature.ExecuteCommandWithResultAsync(command, new CreateContact
        {
            FirstName = "First",
            LastName = "Last",
        });
        
        Assert.That(result!.Id, Is.EqualTo(1));
        Assert.That(result.FirstName, Is.EqualTo("First"));
        Assert.That(result.LastName, Is.EqualTo("Last"));
    }

    class CreateContactValidator : AbstractValidator<CreateContact>
    {
        public CreateContactValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
        }
    }

    [Test]
    public async Task Does_execute_Validators()
    {
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = appHost =>
            {
                appHost.Register<IValidator<CreateContact>>(new CreateContactValidator());
                appHost.Plugins.Add(new ValidationFeature());
                appHost.Plugins.Add(new CommandsFeature {
                    Log = NullLogger<CommandsFeature>.Instance,
                });
            }
        }.Init();
        
        var command = new AddContactCommand();
        var feature = appHost.AssertPlugin<CommandsFeature>();
        var result = await feature.ExecuteCommandWithResultAsync(command, new CreateContact());
        
        Assert.That(result, Is.Null);
        Assert.That(feature.CommandFailures.Count, Is.EqualTo(1));
        Assert.That(feature.CommandFailures.First().Error!.ErrorCode, Is.EqualTo("NotEmpty"));
    }

    [Test]
    public async Task Can_execute_commands_with_builtin_base_classes()
    {
        void AssertCount(int count)
        {
            Assert.That(MyAsyncCommand.Count, Is.EqualTo(count));
            Assert.That(MySyncCommand.Count, Is.EqualTo(count));
            Assert.That(MyAsyncCommandArg.Count, Is.EqualTo(count));
            Assert.That(MySyncCommandArg.Count, Is.EqualTo(count));
            Assert.That(MyAsyncCommandArgResult.Count, Is.EqualTo(count));
            Assert.That(MySyncCommandArgResult.Count, Is.EqualTo(count));
        }
        MyAsyncCommand.Count = MySyncCommand.Count = MyAsyncCommandArg.Count = MySyncCommandArg.Count = 
            MyAsyncCommandArgResult.Count = MySyncCommandArgResult.Count = 0;
        
        var feature = CreateCommandsFeature();
        await feature.ExecuteCommandAsync(new MyAsyncCommand());
        await feature.ExecuteCommandAsync(new MySyncCommand());
        await feature.ExecuteCommandAsync(new MyAsyncCommandArg(), new Arg());
        await feature.ExecuteCommandAsync(new MySyncCommandArg(), new Arg());
        await feature.ExecuteCommandWithResultAsync(new MyAsyncCommandArgResult(), new Arg());
        await feature.ExecuteCommandWithResultAsync(new MySyncCommandArgResult(), new Arg());
        
        AssertCount(1);
    }
}

public class MyAsyncCommand : AsyncCommand
{
    public static int Count;
    protected override async Task RunAsync(CancellationToken token) => Count++;
}
public class MySyncCommand : SyncCommand
{
    public static int Count;
    protected override void Run() => Count++;
}
public class Arg {}
public class MyAsyncCommandArg : AsyncCommand<Arg>
{
    public static int Count;
    protected override async Task RunAsync(Arg request, CancellationToken token) => Count++;
}
public class MySyncCommandArg : SyncCommand<Arg>
{
    public static int Count;
    protected override void Run(Arg request) => Count++;
}
public class MyAsyncCommandArgResult : AsyncCommandWithResult<Arg,Arg>
{
    public static int Count;
    protected override async Task<Arg> RunAsync(Arg request, CancellationToken token)
    {
        Count++;
        return request;
    }
}
public class MySyncCommandArgResult : SyncCommandWithResult<Arg,Arg>
{
    public static int Count;
    protected override Arg Run(Arg request)
    {
        Count++;
        return request;
    }
}

public class FailedRequest {}

public class FailNoRetryCommand : IAsyncCommand<FailedRequest>
{
    public int Attempts { get; set; }
    public Task ExecuteAsync(FailedRequest request)
    {
        if (Attempts++ < 3)
            throw new Exception($"{Attempts} Attempt Failed");
        return Task.CompletedTask;
    }
}

[Retry]
public class FailDefaultRetryCommand : IAsyncCommand<FailedRequest>
{
    public int Attempts { get; set; }
    public Task ExecuteAsync(FailedRequest request)
    {
        if (Attempts++ < 3)
            throw new Exception($"{Attempts} Attempt Failed");
        return Task.CompletedTask;
    }
}

[Retry(Times = 1)]
public class FailTimes1Command : IAsyncCommand<FailedRequest>
{
    public int Attempts { get; set; }
    public Task ExecuteAsync(FailedRequest request)
    {
        if (Attempts++ < 3)
            throw new Exception($"{Attempts} Attempt Failed");
        return Task.CompletedTask;
    }
}

[Retry(Times = 4)]
public class FailTimes4Command : IAsyncCommand<FailedRequest>
{
    public int Attempts { get; set; }
    public Task ExecuteAsync(FailedRequest request)
    {
        if (Attempts++ < 5)
            throw new Exception($"{Attempts} Attempt Failed");
        return Task.CompletedTask;
    }
}

public class AddContactCommand : IAsyncCommand<CreateContact,Contact?>
{
    public Contact? Result { get; private set; }
    
    public async Task ExecuteAsync(CreateContact request)
    {
        var newContact = request.ConvertTo<Contact>();
        newContact.Id = 1;
        Result = newContact;
    }
}
#endif
