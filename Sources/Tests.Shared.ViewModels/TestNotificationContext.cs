using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Shuriken.Monitoring;

namespace Tests.Shared.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal class TestNotificationContext : INotificationContext
    {
        [NotNull]
        readonly TaskScheduler taskScheduler;

        internal TestNotificationContext([NotNull] TaskScheduler taskScheduler)
        {
            this.taskScheduler = taskScheduler;
        }

        public virtual void Invoke(Action action)
            => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Wait();

        public virtual Task InvokeAsync(Action action)
            => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, taskScheduler);
    }
}