using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.QualityTools.Testing.Fakes.Shims;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using Microsoft.Win32.Fakes;
using Shuriken;
using Shuriken.Monitoring;
using Tests.Shared;
using Tests.Shared.ViewModels;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed partial class ApplicationMonitorScopeTests
    {
        sealed class FailingNotificationContext<E> : TestNotificationContext where E : Exception, new()
        {
            readonly InvalidNotificationContextMode mode;

            internal FailingNotificationContext([NotNull] TaskScheduler taskScheduler, InvalidNotificationContextMode mode) : base(taskScheduler)
            {
                this.mode = mode;
            }

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

            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            public override Task InvokeAsync(Action action)
            {
                switch (mode)
                {
                    case InvalidNotificationContextMode.FailInvokeAsync:
                        throw new E();

                    case InvalidNotificationContextMode.InvokeAsyncFails:
                        return Task.Run(() => { throw new E(); });

                    case InvalidNotificationContextMode.InvokeAsyncReturnsNull:
                        return null;

                    default:
                        return base.InvokeAsync(action);
                }
            }
        }

        enum InvalidNotificationContextMode
        {
            FailInvoke,
            FailInvokeAsync,
            InvokeAsyncFails,
            InvokeAsyncReturnsNull,
        }

        static async Task ExecuteInApplicationMonitorScopeWithFailingNotificationContext<T, E>(
            InvalidNotificationContextMode mode,
            [NotNull] T observableObject,
            [NotNull] Action<T> changeProperty,
            [NotNull] string propertyName,
            bool canRaisePropertyChangeNotifications = true) where T : ObservableObject where E : Exception, new()
            =>
                await
                    ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                        () => new FailingNotificationContext<E>(TaskScheduler.FromCurrentSynchronizationContext(), mode),
                        async monitorScope =>
                        {
                            var eventRaisingCount = 0;
                            var expectedEventRaisingCount = 0;

                            PropertyChangedEventHandler propertyChangedEventHandler = (sender, e) =>
                            {
                                Assert.AreSame(observableObject, sender);
                                Assert.IsNotNull(e);
                                Assert.AreEqual(propertyName, e.PropertyName);

                                eventRaisingCount++;
                            };

                            observableObject.PropertyChanged += propertyChangedEventHandler;

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
                                observableObject.PropertyChanged -= propertyChangedEventHandler;
                            }
                        }).ConfigureAwait(false);

        [TestMethod]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public async Task _Ctor()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new ApplicationMonitorScope(null), "notificationContext");

            Assert.IsNull(ApplicationMonitorScope.Current);

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    ExceptionAssert.Throws<InvalidOperationException>(() => new ApplicationMonitorScope(monitorScope.NotificationContext));

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Assert.AreSame(monitorScope, ApplicationMonitorScope.Current);

                    // double-disposing
                    await monitorScope.Dispose();
                });

            Assert.IsNull(ApplicationMonitorScope.Current);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "UnthrowableException")]
        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Sessions()
        {
            using (ShimsContext.Create())
            {
                ShimSystemEvents.Behavior = ShimBehaviors.Fallthrough;

                // failure attaching system event (InvalidOperationException)
                ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler = handler => { throw new InvalidOperationException(); };

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(async monitorScope => await Task.Yield());

                // failure attaching system event (ExternalException)
                ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler = handler => { throw new ExternalException(); };

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(async monitorScope => await Task.Yield());

                // raising system event
                SessionSwitchEventHandler eventHandler = null;
                ShimSystemEvents.SessionSwitchAddSessionSwitchEventHandler = handler => eventHandler += handler;
                ShimSystemEvents.SessionSwitchRemoveSessionSwitchEventHandler = handler => eventHandler -= handler;

                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                    async monitorScope =>
                    {
                        await Task.Delay(50);

                        foreach (SessionSwitchReason reason in Enum.GetValues(typeof(SessionSwitchReason)))
                        {
                            eventHandler(null, new SessionSwitchEventArgs(reason));
                        }
                    });
            }
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task Suspend()
        {
            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    DisposableAssert.IsValid(() => monitorScope.Suspend());
                });

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Delay(1000);

                    using (monitorScope.Suspend())
                    {
                        await Task.Delay(50);

                        await monitorScope.Dispose();
                    }
                });
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "MustUseReturnValue")]
        public async Task Suspend_WithOverflow() => await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
            async monitorScope =>
            {
                await Task.Yield();

                var countEventForSuspensions = new PrivateObject(monitorScope).GetField("countEventForSuspensions");
                new PrivateObject(countEventForSuspensions).SetField("count", int.MaxValue);

                ExceptionAssert.Throws<InvalidOperationException>(() => monitorScope.Suspend());
            });
    }
}