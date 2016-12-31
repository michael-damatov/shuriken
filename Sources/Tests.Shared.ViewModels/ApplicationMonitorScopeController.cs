using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken.Monitoring;

namespace Tests.Shared.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal static class ApplicationMonitorScopeController
    {
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        internal static async Task ExecuteInApplicationMonitorScope<C>(
            [NotNull] Func<C> notificationContextFactory,
            [NotNull] Func<ApplicationMonitorScope, Task> action) where C : INotificationContext
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            try
            {
                var notificationContext = notificationContextFactory();

                var monitorScope = new ApplicationMonitorScope(notificationContext);
                try
                {
                    Assert.AreSame(notificationContext, monitorScope.NotificationContext);
                    Assert.AreSame(monitorScope, ApplicationMonitorScope.Current);

                    await action(monitorScope);
                }
                finally
                {
                    await monitorScope.Dispose();
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        internal static async Task ExecuteInApplicationMonitorScope([NotNull] Func<ApplicationMonitorScope, Task> action)
            =>
                await
                    ExecuteInApplicationMonitorScope(() => new TestNotificationContext(TaskScheduler.FromCurrentSynchronizationContext()), action)
                        .ConfigureAwait(false);

    }
}