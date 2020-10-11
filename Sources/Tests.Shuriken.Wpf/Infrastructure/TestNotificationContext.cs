using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Shuriken.Monitoring;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [ExcludeFromCodeCoverage]
    internal class TestNotificationContext : INotificationContext
    {
        readonly TaskScheduler taskScheduler;

        internal TestNotificationContext(TaskScheduler taskScheduler) => this.taskScheduler = taskScheduler;

        public virtual void Invoke(Action action)
            => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Wait();

        public virtual Task InvokeAsync(Action action)
            => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, taskScheduler);
    }
}