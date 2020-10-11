using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Shuriken.Monitoring;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    [SuppressMessage("ReSharper", "MustUseReturnValue")]
    [SuppressMessage("ReSharper", "UnthrowableException")]
    public sealed partial class ApplicationMonitorScopeTests
    {
        sealed class FailingNotificationContext<E> : TestNotificationContext where E : Exception, new()
        {
            readonly InvalidNotificationContextMode mode;

            internal FailingNotificationContext(TaskScheduler taskScheduler, InvalidNotificationContextMode mode) : base(taskScheduler)
                => this.mode = mode;

            public override void Invoke(Action action)
            {
                switch (mode)
                {
                    case InvalidNotificationContextMode.FailInvoke:
                        throw new E();

                    default:
                        base.Invoke(action);
                        break;
                }
            }

            public override Task InvokeAsync(Action action)
                => mode switch
                {
                    InvalidNotificationContextMode.FailInvokeAsync => throw new E(),
                    InvalidNotificationContextMode.InvokeAsyncFails => Task.Run(() => throw new E()),
                    InvalidNotificationContextMode.InvokeAsyncReturnsNull => null!,
                    _ => base.InvokeAsync(action),
                };
        }

        enum InvalidNotificationContextMode
        {
            FailInvoke,
            FailInvokeAsync,
            InvokeAsyncFails,
            InvokeAsyncReturnsNull,
        }

        static Task ExecuteInApplicationMonitorScopeWithFailingNotificationContext<T, E>(
            InvalidNotificationContextMode mode,
            T observableObject,
            Action<T> changeProperty,
            string propertyName,
            bool canRaisePropertyChangeNotifications = true) where T : ObservableObject where E : Exception, new()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                () => new FailingNotificationContext<E>(TaskScheduler.FromCurrentSynchronizationContext(), mode),
                async monitorScope =>
                {
                    var eventRaisingCount = 0;
                    var expectedEventRaisingCount = 0;

                    void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
                    {
                        Assert.AreSame(observableObject, sender!);
                        Assert.IsNotNull(e);
                        Assert.AreEqual(propertyName, e.PropertyName);

                        eventRaisingCount++;
                    }

                    observableObject.PropertyChanged += PropertyChangedEventHandler;

                    try
                    {
                        await Task.Delay(50);
                        changeProperty(observableObject);

                        if (canRaisePropertyChangeNotifications)
                        {
                            expectedEventRaisingCount++;
                        }

                        await Task.Delay(1000);
                        Assert.AreEqual(expectedEventRaisingCount, eventRaisingCount);
                    }
                    finally
                    {
                        observableObject.PropertyChanged -= PropertyChangedEventHandler;
                    }
                });

        [TestMethod]
        public async Task _Ctor()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new ApplicationMonitorScope(null!), "notificationContext");

            NullAssert.IsNull(ApplicationMonitorScope.Current);

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    ExceptionAssert.Throws<InvalidOperationException>(() => new ApplicationMonitorScope(monitorScope.NotificationContext));

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Assert.AreSame(monitorScope, ApplicationMonitorScope.Current!);

                    // double-disposing
                    await monitorScope.DisposeAsync().ConfigureAwait(false);
                });

            NullAssert.IsNull(ApplicationMonitorScope.Current);
        }

#if !NETCOREAPP
        [TestMethod]
        public async Task _Sessions()
        {
            using (Microsoft.QualityTools.Testing.Fakes.ShimsContext.Create())
            {
                Microsoft.Win32.Fakes.ShimSystemEvents.Behavior = Microsoft.QualityTools.Testing.Fakes.Shims.ShimBehaviors.Fallthrough;

                // failure attaching system event (InvalidOperationException)
                Microsoft.Win32.Fakes.ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler = handler => throw new InvalidOperationException();

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(async monitorScope => await Task.Yield());

                // failure attaching system event (ExternalException)
                Microsoft.Win32.Fakes.ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler =
                    handler => throw new System.Runtime.InteropServices.ExternalException();

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(async monitorScope => await Task.Yield());

                // raising system event
                Microsoft.Win32.SessionSwitchEventHandler? eventHandler = null;
                Microsoft.Win32.Fakes.ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler = handler => eventHandler += handler;
                Microsoft.Win32.Fakes.ShimSystemEvents.SessionSwitchRemoveSessionSwitchEventHandler = handler => eventHandler -= handler;

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                    async monitorScope =>
                    {
                        await Task.Delay(50);

                        foreach (var reason in Enum.GetValues(typeof(Microsoft.Win32.SessionSwitchReason)))
                        {
                            eventHandler!(null!, new Microsoft.Win32.SessionSwitchEventArgs((Microsoft.Win32.SessionSwitchReason)reason!));
                        }
                    });
            }
        }
#endif

        [TestMethod]
        public async Task Suspend()
        {
            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    DisposableTypeAssert.IsValid(monitorScope.Suspend);
                });

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                    async monitorScope =>
                    {
                        await Task.Delay(1000);

                        using (monitorScope.Suspend())
                        {
                            await Task.Delay(50);

                            await monitorScope.DisposeAsync();
                        }
                    })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public Task Suspend_WithOverflow()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    var countEventForSuspensions = typeof(ApplicationMonitorScope).GetField(
                        "countEventForSuspensions",
                        BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(monitorScope)!;
                    countEventForSuspensions.GetType().GetField("count", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(
                        countEventForSuspensions,
                        int.MaxValue);

                    ExceptionAssert.Throws<InvalidOperationException>(() => monitorScope.Suspend());
                });
    }
}