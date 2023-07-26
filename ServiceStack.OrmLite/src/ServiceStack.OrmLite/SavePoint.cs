using System;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public class SavePoint
{
    private OrmLiteTransaction Transaction { get; }
    private IOrmLiteDialectProvider DialectProvider { get; }
    public string Name { get; }
    private bool didRelease;
    private bool didRollback;

    public SavePoint(OrmLiteTransaction transaction, string name)
    {
        Transaction = transaction;
        Name = name;
        DialectProvider = Transaction.Db.GetDialectProvider();
    }

    void VerifyValidState()
    {
        if (didRelease)
            throw new InvalidOperationException($"SAVEPOINT {Name} already RELEASED");
        if (didRollback)
            throw new InvalidOperationException($"SAVEPOINT {Name} already ROLLBACKED");
    }

    public void Save()
    {
        VerifyValidState();

        var sql = DialectProvider.ToCreateSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            Transaction.Db.ExecuteSql(sql);
        }
    }

    public async Task SaveAsync()
    {
        VerifyValidState();

        var sql = DialectProvider.ToCreateSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            await Transaction.Db.ExecuteSqlAsync(sql).ConfigAwait();
        }
    }
        
    public void Release()
    {
        VerifyValidState();
        didRelease = true;
        
        var sql = DialectProvider.ToReleaseSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            Transaction.Db.ExecuteSql(sql);
        }
    }
        
    public async Task ReleaseAsync()
    {
        VerifyValidState();
        didRelease = true;
        
        var sql = DialectProvider.ToReleaseSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            await Transaction.Db.ExecuteSqlAsync(sql).ConfigAwait();
        }
    }

    public void Rollback()
    {
        VerifyValidState();
        didRollback = true;

        var sql = DialectProvider.ToRollbackSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            Transaction.Db.ExecuteSql(sql);
        }
    }

    public async Task RollbackAsync()
    {
        VerifyValidState();
        didRollback = true;

        var sql = DialectProvider.ToRollbackSavePoint(Name);
        if (!string.IsNullOrEmpty(sql))
        {
            await Transaction.Db.ExecuteSqlAsync(sql).ConfigAwait();
        }
    }
}
