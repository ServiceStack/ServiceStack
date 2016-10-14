#if !NETSTANDARD1_6

using System;
using System.Data.Common;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable 1591 // xml doc comments warnings

namespace ServiceStack.MiniProfiler.Data
{
    public class ProfiledDbCommand : ProfiledCommand, ICloneable
    {
        private bool bindByName;
        /// <summary>
        /// If the underlying command supports BindByName, this sets/clears the underlying
        /// implementation accordingly. This is required to support OracleCommand from dapper-dot-net
        /// </summary>
        public bool BindByName
        {
            get { return bindByName; }
            set
            {
                if (bindByName != value)
                {
                    if (_cmd != null)
                    {
                        var inner = GetBindByName(_cmd.GetType());
                        if (inner != null) inner(_cmd, value);
                    }
                    bindByName = value;
                }
            }
        }
        static Link<Type, Action<IDbCommand, bool>> bindByNameCache;
        static Action<IDbCommand, bool> GetBindByName(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            Action<IDbCommand, bool> action;
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out action))
            {
                return action;
            }
            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool)
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0)
                && (setter = prop.GetSetMethod()) != null
                )
            {
                var method = new DynamicMethod(commandType.GetOperationName() + "_BindByName", null, new Type[] { typeof(IDbCommand), typeof(bool) });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, commandType);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, setter, null);
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand, bool>)method.CreateDelegate(typeof(Action<IDbCommand, bool>));
            }
            // cache it            
            Link<Type, Action<IDbCommand, bool>>.TryAdd(ref bindByNameCache, commandType, ref action);
            return action;
        }


        public ProfiledDbCommand(DbCommand cmd, DbConnection conn, IDbProfiler profiler)
            : base(cmd, conn, profiler)
        {
        }

        protected override DbConnection DbConnection
        {
            get { return base.DbConnection; }
            set
            {
                // TODO: we need a way to grab the IDbProfiler which may not be the same as the MiniProfiler, it could be wrapped
                // allow for command reuse, it is clear the connection is going to need to be reset
                if (Profiler.Current != null)
                {
                    _profiler = Profiler.Current;
                }

                base.DbConnection = value;
            }
        }

        #region Wrapper properties for backwards compatibility

        protected DbCommand _cmd
        {
            get { return DbCommand; }
            set { DbCommand = value; }
        }

        protected DbConnection _conn
        {
            get { return DbConnection; }
            set { DbConnection = value; }
        }

        protected DbTransaction _tran
        {
            get { return DbTransaction; }
            set { DbTransaction = value; }
        }

        protected IDbProfiler _profiler
        {
            get { return DbProfiler; }
            set { DbProfiler = value; }
        }

        #endregion

        public ProfiledDbCommand Clone()
        { // EF expects ICloneable
            ICloneable tail = _cmd as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _cmd.GetType().FullName + " is not cloneable");
            return new ProfiledDbCommand((DbCommand)tail.Clone(), _conn, _profiler);
        }
        object ICloneable.Clone() { return Clone(); }
    }
}

#pragma warning restore 1591 // xml doc comments warnings

#endif
