using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite
{
    public interface IOrmLiteExecFilter
    {
        SqlExpression<T> SqlExpression<T>(IDbConnection dbConn);
        IDbCommand CreateCommand(IDbConnection dbConn);
        void DisposeCommand(IDbCommand dbCmd, IDbConnection dbConn);
        T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter);
        IDbCommand Exec(IDbConnection dbConn, Func<IDbCommand, IDbCommand> filter);
        Task<T> Exec<T>(IDbConnection dbConn, Func<IDbCommand, Task<T>> filter);
        Task<IDbCommand> Exec(IDbConnection dbConn, Func<IDbCommand, Task<IDbCommand>> filter);
        void Exec(IDbConnection dbConn, Action<IDbCommand> filter);
        Task Exec(IDbConnection dbConn, Func<IDbCommand, Task> filter);
        IEnumerable<T> ExecLazy<T>(IDbConnection dbConn, Func<IDbCommand, IEnumerable<T>> filter);
    }

    public class OrmLiteExecFilter : IOrmLiteExecFilter
    {
        public virtual SqlExpression<T> SqlExpression<T>(IDbConnection dbConn)
        {
            return dbConn.GetDialectProvider().SqlExpression<T>();
        }

        public virtual IDbCommand CreateCommand(IDbConnection dbConn)
        {
            var ormLiteConn = dbConn as OrmLiteConnection;

            var dbCmd = dbConn.CreateCommand();

            dbCmd.Transaction = ormLiteConn != null 
                ? ormLiteConn.Transaction 
                : OrmLiteContext.TSTransaction;

            dbCmd.CommandTimeout = ormLiteConn != null 
                ? (ormLiteConn.CommandTimeout ?? OrmLiteConfig.CommandTimeout) 
                : OrmLiteConfig.CommandTimeout;

            ormLiteConn.SetLastCommand(dbCmd);
            ormLiteConn.SetLastCommandText(null);

            return ormLiteConn != null
                ? new OrmLiteCommand(ormLiteConn, dbCmd)
                : dbCmd;
        }

        public virtual void DisposeCommand(IDbCommand dbCmd, IDbConnection dbConn)
        {
            if (dbCmd == null) return;

            OrmLiteConfig.AfterExecFilter?.Invoke(dbCmd);

            dbConn.SetLastCommand(dbCmd);
            dbConn.SetLastCommandText(dbCmd.CommandText);

            dbCmd.Dispose();
        }

        public virtual T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
            Exception e = null;
            try
            {
                var ret = filter(dbCmd);
                return ret;
            }
            catch (Exception ex)
            {
                e = ex;
                OrmLiteConfig.ExceptionFilter?.Invoke(dbCmd, ex);
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteCommandError(id, dbCmd, e);
                else
                    Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
                DisposeCommand(dbCmd, dbConn);
            }
        }

        public virtual IDbCommand Exec(IDbConnection dbConn, Func<IDbCommand, IDbCommand> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var ret = filter(dbCmd);
            if (dbCmd != null)
            {
                dbConn.SetLastCommand(dbCmd);
                dbConn.SetLastCommandText(dbCmd.CommandText);
            }
            return ret;
        }

        public virtual void Exec(IDbConnection dbConn, Action<IDbCommand> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
            Exception e = null;
            try
            {
                filter(dbCmd);
            }
            catch (Exception ex)
            {
                e = ex;
                OrmLiteConfig.ExceptionFilter?.Invoke(dbCmd, ex);
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteCommandError(id, dbCmd, e);
                else
                    Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
                DisposeCommand(dbCmd, dbConn);
            }
        }

        public virtual async Task<T> Exec<T>(IDbConnection dbConn, Func<IDbCommand, Task<T>> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
            Exception e = null;

            try
            {
                return await filter(dbCmd);
            }
            catch (Exception ex)
            {
                e = ex.UnwrapIfSingleException(); 
                OrmLiteConfig.ExceptionFilter?.Invoke(dbCmd, e);
                throw e;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteCommandError(id, dbCmd, e);
                else
                    Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
                DisposeCommand(dbCmd, dbConn);
            }
        }

        public virtual async Task<IDbCommand> Exec(IDbConnection dbConn, Func<IDbCommand, Task<IDbCommand>> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            return await filter(dbCmd);
        }

        public virtual async Task Exec(IDbConnection dbConn, Func<IDbCommand, Task> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
            Exception e = null;

            try
            {
                await filter(dbCmd);
            }
            catch (Exception ex)
            {
                e = ex.UnwrapIfSingleException();
                OrmLiteConfig.ExceptionFilter?.Invoke(dbCmd, e);
                throw e;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteCommandError(id, dbCmd, e);
                else
                    Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
                DisposeCommand(dbCmd, dbConn);
            }
        }

        public virtual IEnumerable<T> ExecLazy<T>(IDbConnection dbConn, Func<IDbCommand, IEnumerable<T>> filter)
        {
            var dbCmd = CreateCommand(dbConn);
            var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
            try
            {
                var results = filter(dbCmd);
                foreach (var item in results)
                {
                    yield return item;
                }
            }
            finally
            {
                Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
                DisposeCommand(dbCmd, dbConn);
            }
        }
    }
}
