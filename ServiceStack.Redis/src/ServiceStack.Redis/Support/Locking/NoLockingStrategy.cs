using System;

namespace ServiceStack.Redis.Support.Locking
{
    public class NoLockingStrategy : ILockingStrategy
    {
        public IDisposable ReadLock()
        {
            return null;
        }

        public IDisposable WriteLock()
        {
            return null;
        }
    }
}