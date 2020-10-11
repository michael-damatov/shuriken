using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken.Monitoring;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [ExcludeFromCodeCoverage]
    internal static class ApplicationMonitorScopeController
    {
        internal static async Task ExecuteInApplicationMonitorScope<C>(Func<C> notificationContextFactory, Func<ApplicationMonitorScope, Task> action)
            where C : INotificationContext
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            try
            {
                var notificationContext = notificationContextFactory();

                var monitorScope = new ApplicationMonitorScope(notificationContext);
                try
                {
                    Assert.AreSame(notificationContext, monitorScope.NotificationContext);
                    Assert.AreSame(monitorScope, ApplicationMonitorScope.Current!);

                    await action(monitorScope);
                }
                finally
                {
                    await monitorScope.DisposeAsync();
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        internal static Task ExecuteInApplicationMonitorScope(Func<ApplicationMonitorScope, Task> action)
            => ExecuteInApplicationMonitorScope(() => new TestNotificationContext(TaskScheduler.FromCurrentSynchronizationContext()), action);
    }
}