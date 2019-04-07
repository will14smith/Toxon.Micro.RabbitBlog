using System;
using System.Threading;
using System.Threading.Tasks;

namespace Toxon.Micro.RabbitBlog.Rabbit
{
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly IDisposable _semaphoreReleaser;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1);
            _semaphoreReleaser = new SemaphoreSlimReleaser(_semaphore);
        }

        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return _semaphoreReleaser;
        }

        private sealed class SemaphoreSlimReleaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public SemaphoreSlimReleaser(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
}
