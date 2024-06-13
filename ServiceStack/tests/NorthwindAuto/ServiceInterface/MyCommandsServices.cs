using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class MyCommands
{
    [Command<AddTodoCommand>]
    public CreateTodo? CreateTodo { get; set; }

    [Command<ThrowExceptionCommand>]
    public ThrowException? ThrowException { get; set; }

    [Command<ThrowExceptionCommand>]
    public ThrowException? ThrowArgumentException { get; set; }

    [Command<ThrowExceptionCommand>]
    public ThrowException? ThrowNotSupportedException { get; set; }
}

[Retry]
[Tag("Todo")]
public class AddTodoCommand(ILogger<AddTodoCommand> log, IDbConnection db) : IAsyncCommand<CreateTodo,Todo?>
{
    public Todo? Result { get; private set; }
    
    public async Task ExecuteAsync(CreateTodo request)
    {
        var newTodo = request.ConvertTo<Todo>();
        newTodo.Id = await db.InsertAsync(newTodo, selectIdentity:true);
        log.LogDebug("Created Todo {Id}: {Text}", newTodo.Id, newTodo.Text);
        Result = newTodo;
    }
}

[Lifetime(Lifetime.Scoped)]
public class AddOneWayTodoCommand(ILogger<AddTodoCommand> log, IDbConnection db) : IAsyncCommand<CreateTodo>
{
    public async Task ExecuteAsync(CreateTodo request)
    {
        var newTodo = request.ConvertTo<Todo>();
        newTodo.Id = await db.InsertAsync(newTodo, selectIdentity:true);
        log.LogDebug("Created Todo {Id}: {Text}", newTodo.Id, newTodo.Text);
    }
}


public class FailedRequest {}

[Tag("Fail")]
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
[Tag("Fail")]
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
[Tag("Fail")]
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
[Tag("Fail")]
public class FailTimes4Command : IAsyncCommand<FailedRequest>
{
    public int Attempts { get; set; }
    public Task ExecuteAsync(FailedRequest request)
    {
        if (Attempts++ < 3)
            throw new Exception($"{Attempts} Attempt Failed");
        return Task.CompletedTask;
    }
}

public class FailedCommandTests
{
    public bool? FailNoRetryCommand { get; set; }
    public bool? FailDefaultRetryCommand { get; set; }
    public bool? FailTimes1Command { get; set; }
    public bool? FailTimes4Command { get; set; }
}


public record ThrowException(string Type, string Message, string? Param=null);

public class ThrowExceptionCommand : IAsyncCommand<ThrowException>
{
    public Task ExecuteAsync(ThrowException request)
    {
        throw request.Type switch {
            nameof(ArgumentException) => new ArgumentException(request.Message),
            nameof(ArgumentNullException) => new ArgumentNullException(request.Message),
            nameof(NotSupportedException) => new NotSupportedException(request.Message),
            _ => new Exception(request.Message) 
        };
    }
}

public class MyCommandsServices(ICommandExecutor executor) : Service
{
    public async Task<object> Any(CommandOperation request)
    {
        var commands = new MyCommands();

        if (request.NewTodo != null)
            commands.CreateTodo = new() { Text = request.NewTodo };
        if (request.ThrowException != null)
            commands.ThrowException = new(nameof(Exception), request.ThrowException);
        if (request.ThrowArgumentException != null)
            commands.ThrowArgumentException = new(nameof(ArgumentException), request.ThrowArgumentException);
        if (request.ThrowNotSupportedException != null)
            commands.ThrowNotSupportedException = new(nameof(NotSupportedException), request.ThrowNotSupportedException);

        await Request.ExecuteCommandsAsync(commands);

        return new EmptyResponse();
    }

    public async Task Any(FailedCommandTests request)
    {
        if (request.FailNoRetryCommand != null)
            await executor.ExecuteAsync(executor.Command<FailNoRetryCommand>(), new FailedRequest());
        if (request.FailDefaultRetryCommand != null)
            await executor.ExecuteAsync(executor.Command<FailDefaultRetryCommand>(), new FailedRequest());
        if (request.FailTimes1Command != null)
            await executor.ExecuteAsync(executor.Command<FailTimes1Command>(), new FailedRequest());
        if (request.FailTimes4Command != null)
            await executor.ExecuteAsync(executor.Command<FailTimes4Command>(), new FailedRequest());
    }
}