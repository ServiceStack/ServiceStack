using System;
using System.Data.Common;
using System.Data;
using ServiceStack.Data;

#pragma warning disable 1591 // xml doc comments warnings

namespace ServiceStack.MiniProfiler.Data
{
    public class ProfiledCommand : DbCommand, IHasDbCommand
    {
        private DbCommand _cmd;
        private DbConnection _conn;
        private DbTransaction _tran;
        private IDbProfiler _profiler;

        public ProfiledCommand(DbCommand cmd, DbConnection conn, IDbProfiler profiler)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");

            _cmd = cmd;
            _conn = conn;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        public override string CommandText
        {
            get { return _cmd.CommandText; }
            set { _cmd.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return _cmd.CommandTimeout; }
            set { _cmd.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return _cmd.CommandType; }
            set { _cmd.CommandType = value; }
        }

        public DbCommand DbCommand
        {
            get { return _cmd; }
            protected set { _cmd = value; }
        }

        IDbCommand IHasDbCommand.DbCommand
        {
            get { return DbCommand; }            
        }

        protected override DbConnection DbConnection
        {
            get { return _conn; }
            set
            {
                _conn = value;
                var awesomeConn = value as ProfiledConnection;
                _cmd.Connection = awesomeConn == null ? value : awesomeConn.WrappedConnection;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _cmd.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return _tran; }
            set
            {
                this._tran = value;
                var awesomeTran = value as ProfiledDbTransaction;
                _cmd.Transaction = awesomeTran == null || !(awesomeTran.DbTransaction is DbTransaction) ?
                    value : (DbTransaction)awesomeTran.DbTransaction;
            }
        }

        protected IDbProfiler DbProfiler
        {
            get { return _profiler; }
            set { _profiler = value; }
        }

        public override bool DesignTimeVisible
        {
            get { return _cmd.DesignTimeVisible; }
            set { _cmd.DesignTimeVisible = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return _cmd.UpdatedRowSource; }
            set { _cmd.UpdatedRowSource = value; }
        }


        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (_profiler == null || !_profiler.IsActive)
            {
                return _cmd.ExecuteReader(behavior);
            }

            DbDataReader result = null;
            _profiler.ExecuteStart(this, ExecuteType.Reader);
            try
            {
                result = _cmd.ExecuteReader(behavior);
                result = new ProfiledDbDataReader(result, _conn, _profiler);
            }
            catch (Exception e)
            {
                _profiler.OnError(this, ExecuteType.Reader, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, ExecuteType.Reader, result);
            }
            return result;
        }

        public override int ExecuteNonQuery()
        {
            if (_profiler == null || !_profiler.IsActive)
            {
                return _cmd.ExecuteNonQuery();
            }

            int result;

            _profiler.ExecuteStart(this, ExecuteType.NonQuery);
            try
            {
                result = _cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _profiler.OnError(this, ExecuteType.NonQuery, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, ExecuteType.NonQuery, null);
            }
            return result;
        }

        public override object ExecuteScalar()
        {
            if (_profiler == null || !_profiler.IsActive)
            {
                return _cmd.ExecuteScalar();
            }

            object result;
            _profiler.ExecuteStart(this, ExecuteType.Scalar);
            try
            {
                result = _cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                _profiler.OnError(this, ExecuteType.Scalar, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(this, ExecuteType.Scalar, null);
            }
            return result;
        }

        public override void Cancel()
        {
            _cmd.Cancel();
        }

        public override void Prepare()
        {
            _cmd.Prepare();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _cmd.CreateParameter();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _cmd != null)
            {
                _cmd.Dispose();
            }
            _cmd = null;
            base.Dispose(disposing);
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings