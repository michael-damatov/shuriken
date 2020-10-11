using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    /// <summary>
    /// Defines methods to invoke actions in a specific context. The <see cref="ApplicationMonitorScope"/> uses the context to retrieve the current
    /// values and send notifications.
    /// </summary>
    public interface INotificationContext
    {
        /// <summary>
        /// Executes the specified action synchronously.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        void Invoke([InstantHandle] Action action);

        /// <summary>
        /// Executes the specified action asynchronously.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>
        /// A <see cref="Task"/>, which is returned immediately after the method is called, that can be used to wait for the completion.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        [Pure]
        Task InvokeAsync(Action action);
    }
}