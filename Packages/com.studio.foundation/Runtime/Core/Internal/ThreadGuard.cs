using System;
using System.Threading;

namespace Studio.Foundation.Internal
{
    internal sealed class ThreadGuard
    {
        private readonly int ownerThreadId = Thread.CurrentThread.ManagedThreadId;

        public void Check()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Access is restricted to the creating thread.");
        }
    }
}
