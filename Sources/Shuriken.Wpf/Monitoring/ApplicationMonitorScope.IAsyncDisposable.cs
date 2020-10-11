using System;
using System.Threading;
using System.Threading.Tasks;

#if !NETCOREAPP
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope : IDisposable
#if NETCOREAPP
        , IAsyncDisposable
#endif
    {
        static void Wait(ValueTask task)
        {
#if NETCOREAPP
            task.AsTask().GetAwaiter().GetResult();
#else
            task.GetAwaiter().GetResult();
#endif
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ApplicationMonitorScope"/> class.
        /// </summary>
        ~ApplicationMonitorScope() => Wait(Dispose(false));

        async ValueTask Dispose(bool disposing)
        {
            try
            {
                cancellation.Cancel();

                if (disposing)
                {
                    try
                    {
                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref current, null, this);
            }
        }

        /// <inheritdoc />
        public void Dispose() => Wait(DisposeAsync());

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await Dispose(true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}