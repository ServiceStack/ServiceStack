using System.Threading;
using System;

namespace ServiceStack.Redis.Support.Locking
{
    /// <summary>
    /// This class manages a read lock for a local readers/writer lock, 
    /// using the Resource Acquisition Is Initialization pattern
    /// </summary>
    public class ReadLock : IDisposable
    {
        private readonly ReaderWriterLockSlim lockObject;

        /// <summary>
        /// RAII initialization 
        /// </summary>
        /// <param name="lockObject"></param>
        public ReadLock(ReaderWriterLockSlim lockObject)
        {
            this.lockObject = lockObject;
            lockObject.EnterReadLock();
        }

        /// <summary>
        /// RAII disposal
        /// </summary>
        public void Dispose()
        {
            lockObject.ExitReadLock();
        }
    }
}