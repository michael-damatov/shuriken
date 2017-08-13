using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    /// <summary>
    /// Performs monitoring (automatic tracking) of the <see cref="ObservableObject"/> instances.
    /// </summary>
    /// <remarks>
    /// If the scope is closed deterministically (e.g. by exiting the <c>using</c> block) any exception caused the scope to close is re-thrown.
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Disposing private disposable fields would cause racing conditions.")]
    public sealed partial class ApplicationMonitorScope
    {
        /// <remarks>
        /// <list type="table">
        ///     <listheader>
        ///         <term>State</term>
        ///         <description>Condition</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Running</term>
        ///         <description>
        ///             <c>countEventForSuspensions</c>.<see cref="CountdownEvent.CurrentCount"/> == 0.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Suspended</term>
        ///         <description>
        ///             <c>countEventForSuspensions</c>.<see cref="CountdownEvent.CurrentCount"/> &gt; 0.
        ///         </description>
        ///     </item>
        /// </list>
        /// The field also serves as a sync root for the <see cref="sessionSuspension"/> field.
        /// </remarks>
        [NotNull]
        readonly CountEvent countEventForSuspensions = new CountEvent();

        [NotNull]
        readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        [NotNull]
        readonly Task task;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationMonitorScope"/> class.
        /// </summary>
        /// <param name="notificationContext">The notification context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="notificationContext"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The monitor scope is already available.</exception>
        /// <remarks>
        /// The <paramref name="notificationContext"/> is used for:
        /// <list type="bullet">
        ///     <item>getting values of non-thread-safe objects.</item>
        ///     <item>sending notifications for changed properties.</item>
        /// </list>
        /// </remarks>
        public ApplicationMonitorScope([NotNull] INotificationContext notificationContext)
        {
            if (notificationContext == null)
            {
                throw new ArgumentNullException(nameof(notificationContext));
            }

            var currentApplicationMonitorScope = Interlocked.CompareExchange(ref current, this, null);

            if (currentApplicationMonitorScope != null)
            {
                throw new InvalidOperationException($"The {nameof(ApplicationMonitorScope)} is already available.");
            }

            NotificationContext = notificationContext;

            task = Task.Factory.StartNew(
                () =>
                {
                    Debug.Assert(!Thread.CurrentThread.IsThreadPoolThread);
                    Thread.CurrentThread.Name = "ObservableObjectMonitor";

                    EventSource.Log.MonitorStart();

                    RegisterForSessionSwitchNotifications();
                    try
                    {
                        RunMonitor();
                    }
                    finally
                    {
                        UnregisterFromSessionSwitchNotifications();

                        EventSource.Log.MonitorStop();
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Gets the notification context.
        /// </summary>
        [NotNull]
        public INotificationContext NotificationContext { get; }

        /// <summary>
        /// Suspends the monitoring.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> object used to resume the monitoring by invoking the <see cref="IDisposable.Dispose"/> method.
        /// </returns>
        /// <exception cref="InvalidOperationException">Maximum suspension depth has been exceeded.</exception>
        /// <remarks>
        /// Do not invoke the method from the property annotated with the <see cref="ObservableAttribute"/>.
        /// </remarks>
        [MustUseReturnValue]
        [NotNull]
        public IDisposable Suspend() => new Suspension(countEventForSuspensions);
    }
}