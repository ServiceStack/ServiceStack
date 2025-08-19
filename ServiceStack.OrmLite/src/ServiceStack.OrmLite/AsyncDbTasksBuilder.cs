using System;
using System.Data;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Initial builder for adding the first database task
/// </summary>
public readonly struct AsyncDbTasksBuilder(IDbConnectionFactory dbFactory)
{
    private readonly IDbConnectionFactory dbFactory = 
        dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));

    /// <summary>
    /// Add the first database task
    /// </summary>
    public AsyncDbTasks1<T1> Add<T1>(Func<IDbConnection, Task<T1>> fn) => 
        new(dbFactory, dbFactory.AsyncDbTask(fn));
    
    /// <summary>
    /// Add the first database task (void)
    /// </summary>
    public AsyncDbTasks1<bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, dbFactory.AsyncDbTask(fn));
}

/// <summary>
/// Builder with 1 task
/// </summary>
public readonly struct AsyncDbTasks1<T1>(IDbConnectionFactory dbFactory, Task<T1> task1)
{
    /// <summary>
    /// Add a second database task
    /// </summary>
    public AsyncDbTasks2<T1, T2> Add<T2>(Func<IDbConnection, Task<T2>> fn) => 
        new(dbFactory, task1, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a second database task (void)
    /// </summary>
    public AsyncDbTasks2<T1, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, dbFactory.AsyncDbTask(fn));

    public async Task<T1> RunAsync() => await task1;
}

/// <summary>
/// Builder with 2 tasks
/// </summary>
public readonly struct AsyncDbTasks2<T1, T2>(IDbConnectionFactory dbFactory, Task<T1> task1, Task<T2> task2)
{
    /// <summary>
    /// Add a third database task
    /// </summary>
    public AsyncDbTasks3<T1, T2, T3> Add<T3>(Func<IDbConnection, Task<T3>> fn) => 
        new(dbFactory, task1, task2, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a third database task (void)
    /// </summary>
    public AsyncDbTasks3<T1, T2, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, task2, dbFactory.AsyncDbTask(fn));

    public async Task<(T1, T2)> RunAsync()
    {
        await Task.WhenAll(task1, task2);
        return (task1.Result, task2.Result);
    }
}

/// <summary>
/// Builder with 3 tasks
/// </summary>
public readonly struct AsyncDbTasks3<T1, T2, T3>(IDbConnectionFactory dbFactory, Task<T1> task1, Task<T2> task2, Task<T3> task3)
{
    /// <summary>
    /// Add a fourth database task
    /// </summary>
    public AsyncDbTasks4<T1, T2, T3, T4> Add<T4>(Func<IDbConnection, Task<T4>> fn) =>
        new(dbFactory, task1, task2, task3, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a fourth database task (void)
    /// </summary>
    public AsyncDbTasks4<T1, T2, T3, bool> Add(Func<IDbConnection, Task> fn) =>
        new(dbFactory, task1, task2, task3, dbFactory.AsyncDbTask(fn));

    public async Task<(T1, T2, T3)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3);
        return (task1.Result, task2.Result, task3.Result);
    }
}

/// <summary>
/// Builder with 4 tasks
/// </summary>
public readonly struct AsyncDbTasks4<T1, T2, T3, T4>(IDbConnectionFactory dbFactory, Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
{
    /// <summary>
    /// Add a fifth database task
    /// </summary>
    public AsyncDbTasks5<T1, T2, T3, T4, T5> Add<T5>(Func<IDbConnection, Task<T5>> fn) => 
        new(dbFactory, task1, task2, task3, task4, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a fifth database task (void)
    /// </summary>
    public AsyncDbTasks5<T1, T2, T3, T4, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, task2, task3, task4, dbFactory.AsyncDbTask(fn));

    public async Task<(T1, T2, T3, T4)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3, task4);
        return (task1.Result, task2.Result, task3.Result, task4.Result);
    }
}

/// <summary>
/// Builder with 5 tasks
/// </summary>
public readonly struct AsyncDbTasks5<T1, T2, T3, T4, T5>(IDbConnectionFactory dbFactory, Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5)
{
    /// <summary>
    /// Add a sixth database task
    /// </summary>
    public AsyncDbTasks6<T1, T2, T3, T4, T5, T6> Add<T6>(Func<IDbConnection, Task<T6>> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a sixth database task
    /// </summary>
    public AsyncDbTasks6<T1, T2, T3, T4, T5, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, dbFactory.AsyncDbTask(fn));

    public async Task<(T1, T2, T3, T4, T5)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3, task4, task5);
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
    }
}

/// <summary>
/// Builder with 6 tasks
/// </summary>
public readonly struct AsyncDbTasks6<T1, T2, T3, T4, T5, T6>(IDbConnectionFactory dbFactory, Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6)
{
    /// <summary>
    /// Add a seventh database task
    /// </summary>
    public AsyncDbTasks7<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Func<IDbConnection, Task<T7>> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, task6, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add a seventh database task (void)
    /// </summary>
    public AsyncDbTasks7<T1, T2, T3, T4, T5, T6, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, task6, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Execute and return the results as a tuple
    /// </summary>
    public async Task<(T1, T2, T3, T4, T5, T6)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3, task4, task5, task6);
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result);
    }
}

/// <summary>
/// Builder with 7 tasks
/// </summary>
public readonly struct AsyncDbTasks7<T1, T2, T3, T4, T5, T6, T7>(IDbConnectionFactory dbFactory, 
    Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7)
{
    /// <summary>
    /// Add an eighth database task
    /// </summary>
    public AsyncDbTasks8<T1, T2, T3, T4, T5, T6, T7, T8> Add<T8>(Func<IDbConnection, Task<T8>> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, task6, task7, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Add an eighth database task (void)
    /// </summary>
    public AsyncDbTasks8<T1, T2, T3, T4, T5, T6, T7, bool> Add(Func<IDbConnection, Task> fn) => 
        new(dbFactory, task1, task2, task3, task4, task5, task6, task7, dbFactory.AsyncDbTask(fn));

    /// <summary>
    /// Execute and return the results as a tuple
    /// </summary>
    public async Task<(T1, T2, T3, T4, T5, T6, T7)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3, task4, task5, task6, task7);
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result, task7.Result);
    }
}

/// <summary>
/// Builder with 8 tasks (maximum)
/// </summary>
public readonly struct AsyncDbTasks8<T1, T2, T3, T4, T5, T6, T7, T8>(IDbConnectionFactory dbFactory, 
    Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, Task<T6> task6, Task<T7> task7, Task<T8> task8)
{
    /// <summary>
    /// Execute and return the results as a tuple
    /// </summary>
    public async Task<(T1, T2, T3, T4, T5, T6, T7, T8)> RunAsync()
    {
        await Task.WhenAll(task1, task2, task3, task4, task5, task6, task7, task8);
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result, task7.Result, task8.Result);
    }
}

public static class AsyncDbTasksBuilderUtils
{
    public static AsyncDbTasksBuilder AsyncDbTasksBuilder(this IDbConnectionFactory dbFactory) => new(dbFactory);

    internal static async Task<Result> AsyncDbTask<Result>(this IDbConnectionFactory dbFactory, Func<IDbConnection, Task<Result>> fn)
    {
        using var db = await dbFactory.OpenDbConnectionAsync().ConfigAwait();
        return await fn(db).ConfigAwait();
    }

    internal static async Task<bool> AsyncDbTask(this IDbConnectionFactory dbFactory, Func<IDbConnection, Task> fn)
    {
        using var db = await dbFactory.OpenDbConnectionAsync().ConfigAwait();
        await fn(db).ConfigAwait();
        return true;
    }
}
