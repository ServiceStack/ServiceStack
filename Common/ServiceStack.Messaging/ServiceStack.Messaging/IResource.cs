using System;

namespace ServiceStack.Messaging
{
    public interface IResource : IDisposable
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
    }
}