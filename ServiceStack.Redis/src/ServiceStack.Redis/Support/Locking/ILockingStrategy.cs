using System;

namespace ServiceStack.Redis.Support.Locking
{
    /// <summary>
    /// Locking strategy interface
    /// </summary>
	public interface ILockingStrategy
    {
        IDisposable ReadLock();

        IDisposable WriteLock();
    }
}