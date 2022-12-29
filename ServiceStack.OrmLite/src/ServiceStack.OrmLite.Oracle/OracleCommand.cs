using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleCommand : DbCommand
    {
        private readonly DbCommand _command;

        private readonly Lazy<MethodInfo> _setBindByNameMethod;
        private bool _bindByNameIsSet;
        public OracleCommand(DbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            _command = command;
            _setBindByNameMethod = new Lazy<MethodInfo>(() => InitSetBindByName(_command));
        }

        private static MethodInfo InitSetBindByName(DbCommand command)
        {
            return command.GetType().GetMethod("set_BindByName", BindingFlags.Public | BindingFlags.Instance);
        }

        private MethodInfo SetBindByNameMethod
        {
            get
            {
                return _setBindByNameMethod.Value;
            }
        }

        private void SetBindByName()
        {
            if (_bindByNameIsSet || SetBindByNameMethod == null) return;

            SetBindByNameMethod.Invoke(_command, new object[] { true });
            _bindByNameIsSet = true;
        }

        public override void Prepare()
        {
            _command.Prepare();
        }

        public override string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        protected override DbConnection DbConnection
        {
            get { return _command.Connection; }
            set
            {
                _command.Connection = value;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _command.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return _command.Transaction; }
            set { _command.Transaction = value; }
        }

        public override bool DesignTimeVisible
        {
            get { return _command.DesignTimeVisible; }
            set { _command.DesignTimeVisible = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }

        public override void Cancel()
        {
            _command.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _command.CreateParameter();
        }     

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (Parameters.Count > 0) SetBindByName();
            var reader = _command.ExecuteReader(behavior);
            return new OracleDataReader(reader);
        }

        public override int ExecuteNonQuery()
        {
            if (Parameters.Count > 0) SetBindByName();
            return _command.ExecuteNonQuery();
        }

        public override object ExecuteScalar()
        {
            if (Parameters.Count > 0) SetBindByName();
            return _command.ExecuteScalar();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _command.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
