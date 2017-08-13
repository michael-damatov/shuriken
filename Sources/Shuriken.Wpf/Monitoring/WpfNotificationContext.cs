using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    /// <summary>
    /// Provides an <see cref="INotificationContext"/> for WPF applications.
    /// </summary>
    public sealed class WpfNotificationContext : INotificationContext
    {
        [NotNull]
        readonly Dispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfNotificationContext"/> class.
        /// </summary>
        /// <param name="dispatcher">The WPF application dispatcher.</param>
        /// <exception cref="ArgumentNullException"><see cref="dispatcher"/> is <c>null</c>.</exception>
        public WpfNotificationContext([NotNull] Dispatcher dispatcher)
            => this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        /// <inheritdoc />
        public void Invoke(Action action) => dispatcher.Invoke(action, DispatcherPriority.DataBind + 1);

        /// <inheritdoc />
        public async Task InvokeAsync(Action action) => await dispatcher.InvokeAsync(action, DispatcherPriority.DataBind + 1);
    }
}