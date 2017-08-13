using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope
    {
        /// <summary>
        /// Finalizes an instance of the <see cref="Shuriken.Monitoring.ApplicationMonitorScope"/> class.
        /// </summary>
        ~ApplicationMonitorScope() => Dispose(false).GetAwaiter().GetResult();

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cancellation",
            Justification = "Disposing private disposable fields would cause racing conditions.")]
        async Task Dispose(bool disposing)
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async Task Dispose()
        {
            await Dispose(true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}