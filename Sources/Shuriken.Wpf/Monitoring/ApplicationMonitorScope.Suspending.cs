using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope
    {
        [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
            Justification = "Disposing private disposable fields would cause racing conditions.")]
        sealed class CountEvent
        {
            /// <remarks>
            /// The field is also used as sync root for <c>lock</c> statements.
            /// </remarks>
            readonly ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(true);

            [NonNegativeValue]
            int count;

            /// <exception cref="InvalidOperationException">Maximum suspension depth has been exceeded.</exception>
            public void Increment()
            {
                lock (manualResetEvent)
                {
                    switch (count)
                    {
                        case 0:
                            manualResetEvent.Reset();
                            EventSource.Log.MonitorSuspend();
                            break;

                        case int.MaxValue:
                            throw new InvalidOperationException("Maximum suspension depth has been exceeded.");
                    }

                    count++;
                }
            }

            public void TryDecrement()
            {
                lock (manualResetEvent)
                {
                    if (count > 0)
                    {
                        count--;

                        if (count == 0)
                        {
                            manualResetEvent.Set();
                            EventSource.Log.MonitorResume();
                        }
                    }
                }
            }

            [SuppressMessage(
                "ReSharper",
                "InconsistentlySynchronizedField",
                Justification = "The field should not be used inside synchronized block.")]
            public void WaitForZeroOrCancellation(CancellationToken cancellationToken)
            {
                Debug.Assert(cancellationToken.CanBeCanceled);

                try
                {
                    manualResetEvent.Wait(cancellationToken);
                }
                catch (OperationCanceledException) {}
            }
        }

        sealed class Suspension : IDisposable
        {
            CountEvent? countEvent;

            /// <exception cref="InvalidOperationException">Maximum suspension depth has been exceeded.</exception>
            public Suspension(CountEvent countEventForSuspensions)
            {
                Interlocked.Exchange(ref countEvent, countEventForSuspensions);

                try
                {
                    countEventForSuspensions.Increment();
                }
                catch
                {
                    Interlocked.Exchange(ref countEvent, null);

                    throw;
                }
            }

            ~Suspension() => DisposeCore();

            void DisposeCore() => Interlocked.Exchange(ref countEvent, null)?.TryDecrement();

            public void Dispose()
            {
                DisposeCore();
                GC.SuppressFinalize(this);
            }
        }
    }
}