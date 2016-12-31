using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shared;
using Tests.Shared.ViewModels;

namespace Tests.Shuriken.Wpf
{
    partial class ApplicationMonitorScopeTests
    {
        [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Local")]
        class SampleObservableObject : ObservableObject
        {
            int valueTypeValue;
            string referenceTypeValue;

            public SampleObservableObject(bool isThreadSafe) : base(isThreadSafe)
            {
                Assert.AreEqual(isThreadSafe, IsThreadSafe);
            }

            [Observable]
            public int ValueTypeProp
            {
                get
                {
                    return IsThreadSafe ? Interlocked.CompareExchange(ref valueTypeValue, 0, 0) : valueTypeValue;
                }
                set
                {
                    if (IsThreadSafe)
                    {
                        Interlocked.Exchange(ref valueTypeValue, value);
                    }
                    else
                    {
                        valueTypeValue = value;
                    }
                }
            }

            [Observable]
            public virtual string ReferenceTypeProp
            {
                get
                {
                    return IsThreadSafe ? Interlocked.CompareExchange(ref referenceTypeValue, null, null) : referenceTypeValue;
                }
                set
                {
                    if (IsThreadSafe)
                    {
                        Interlocked.Exchange(ref referenceTypeValue, value);
                    }
                    else
                    {
                        referenceTypeValue = value;
                    }
                }
            }

            [Observable]
            public Command Command { get; set; }

            [UsedImplicitly]
            public string this[int index] => null;

            public void NotifyIndexer() => NotifyIndexerChange();
        }

        sealed class ObservableObjectWithFailingProperty : ObservableObject
        {
            readonly int maxNonFailingAccessCount;

            int accessCount;

            public ObservableObjectWithFailingProperty(bool isThreadSafe, int maxNonFailingAccessCount) : base(isThreadSafe)
            {
                Assert.AreEqual(isThreadSafe, IsThreadSafe);

                this.maxNonFailingAccessCount = maxNonFailingAccessCount;
            }

            [Observable]
            [UsedImplicitly]
            public int Prop
            {
                get
                {
                    if (IsThreadSafe)
                    {
                        if (Interlocked.Increment(ref accessCount) >= maxNonFailingAccessCount)
                        {
                            throw new NotSupportedException();
                        }

                        return 1;
                    }

                    accessCount++;

                    if (accessCount >= maxNonFailingAccessCount)
                    {
                        throw new NotSupportedException();
                    }

                    return 1;
                }
            }
        }

        sealed class ObservableObjectWithFailingCommand : ObservableObject
        {
            [NotNull]
            readonly Command command;

            readonly int maxNonFailingAccessCount;

            int accessCount;

            public ObservableObjectWithFailingCommand(bool isThreadSafe, int maxNonFailingAccessCount) : base(isThreadSafe)
            {
                Assert.AreEqual(isThreadSafe, IsThreadSafe);

                this.maxNonFailingAccessCount = maxNonFailingAccessCount;

                command = new Command(() => { }, CanExecuteCommand);
            }

            bool CanExecuteCommand()
            {
                if (IsThreadSafe)
                {
                    if (Interlocked.Increment(ref accessCount) >= maxNonFailingAccessCount)
                    {
                        throw new NotSupportedException();
                    }

                    return true;
                }

                accessCount++;

                if (accessCount >= maxNonFailingAccessCount)
                {
                    throw new NotSupportedException();
                }

                return true;
            }

            [Observable]
            [UsedImplicitly]
            public Command Command
            {
                get
                {
                    if (IsThreadSafe)
                    {
                        if (Interlocked.Increment(ref accessCount) >= maxNonFailingAccessCount)
                        {
                            throw new NotSupportedException();
                        }

                        return command;
                    }

                    accessCount++;

                    if (accessCount >= maxNonFailingAccessCount)
                    {
                        throw new NotSupportedException();
                    }

                    return command;
                }
            }
        }

        sealed class ObservableObjectWithPropertyFailingDueToInvalidEquals : ObservableObject
        {
            public struct ValueWithInvalidEquals
            {
                public override int GetHashCode()
                {
                    throw new NotSupportedException();
                }

                public override bool Equals(object obj)
                {
                    throw new NotSupportedException();
                }
            }

            [Observable]
            [UsedImplicitly]
            public ValueWithInvalidEquals Prop { get; set; }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        sealed class ObservableObjectNotWellDefined : ObservableObject
        {
            public struct MutableValue { }

            [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
            public int WriteOnly
            {
                set { }
            }

            [Observable]
            public MutableValue Mutable { get; set; }

            [Observable]
            [SuppressMessage("ReSharper", "UnusedParameter.Local")]
            public string this[int index] => null;
        }

        sealed class ObservableObjectWithoutObservableProperties : ObservableObject { }

        sealed class LittleKnownObservableObjectWithoutObservableProperties : ObservableObject
        {
            [Observable]
            [UsedImplicitly]
            public string this[int index] => null;
        }

        static async Task _Notifications_WithFailingPropertyChangeNotification<T>(
            [NotNull] T observableObject,
            [NotNull] Action<T> changeProperty,
            [NotNull] string propertyName) where T : ObservableObject
        {
            var source = new TaskCompletionSource<int>();

            PropertyChangedEventHandler propertyChangedEventHandler = (sender, e) =>
            {
                try
                {
                    Assert.AreSame(observableObject, sender);
                    Assert.IsNotNull(e);
                    Assert.AreEqual(propertyName, e.PropertyName);

                    throw new NotSupportedException();
                }
                finally
                {
                    source.TrySetResult(0);
                }
            };

            observableObject.PropertyChanged += propertyChangedEventHandler;
            try
            {
                await Task.Delay(50);

                changeProperty(observableObject);

                var timeoutTask = Task.Delay(1000);
                var task = await Task.WhenAny(timeoutTask, source.Task);
                Assert.AreNotEqual(timeoutTask, task, "timeout while waiting for property change notification.");
            }
            finally
            {
                observableObject.PropertyChanged -= propertyChangedEventHandler;
            }
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Notifications()
        {
            // value type property
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp));
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(true),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp));

            // reference type property
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false),
                    observableObject => observableObject.ReferenceTypeProp += "a",
                    nameof(SampleObservableObject.ReferenceTypeProp));
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(true),
                    observableObject => observableObject.ReferenceTypeProp += "a",
                    nameof(SampleObservableObject.ReferenceTypeProp));

            // commands
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false),
                    observableObject => observableObject.Command = new Command(() => { }),
                    nameof(SampleObservableObject.Command));

            var threadAffineObservableObject = new SampleObservableObject(false);
            threadAffineObservableObject.Command = new Command(() => { }, () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    threadAffineObservableObject,
                    observableObject => observableObject.ValueTypeProp++,
                    new[] { nameof(SampleObservableObject.ValueTypeProp) },
                    new[] { nameof(SampleObservableObject.Command) });

            // indexer
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false),
                    observableObject => observableObject.NotifyIndexer(),
                    Binding.IndexerName);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Notifications_WithCancelingNotificationContext()
        {
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.FailInvoke,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    "Prop",
                    false);
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.FailInvokeAsync,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    "Prop",
                    false);
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.InvokeAsyncFails,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    "Prop",
                    false);
            await
                ExceptionAssert.Throws<InvalidOperationException>(
                    async () =>
                        await
                            ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                                InvalidNotificationContextMode.InvokeAsyncReturnsNull,
                                new SampleObservableObject(false),
                                observableObject => observableObject.ValueTypeProp++,
                                "Prop"));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Notifications_WithFailingNotificationContext()
        {
            await
                ExceptionAssert.Throws<NotSupportedException>(
                    async () =>
                        await
                            ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                                InvalidNotificationContextMode.FailInvoke,
                                new SampleObservableObject(false),
                                observableObject => observableObject.ValueTypeProp++,
                                "Prop",
                                false));
            await
                ExceptionAssert.Throws<NotSupportedException>(
                    async () =>
                        await
                            ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                                InvalidNotificationContextMode.FailInvokeAsync,
                                new SampleObservableObject(false),
                                observableObject => observableObject.ValueTypeProp++,
                                "Prop"));
            await
                ExceptionAssert.Throws<NotSupportedException>(
                    async () =>
                        await
                            ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                                InvalidNotificationContextMode.InvokeAsyncFails,
                                new SampleObservableObject(false),
                                observableObject => observableObject.ValueTypeProp++,
                                "Prop"));
            await
                ExceptionAssert.Throws<InvalidOperationException>(
                    async () =>
                        await
                            ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                                InvalidNotificationContextMode.InvokeAsyncReturnsNull,
                                new SampleObservableObject(false),
                                observableObject => observableObject.ValueTypeProp++,
                                "Prop"));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Notifications_WithExceptionsWhileMonitoring()
        {
            // property fails on first access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(false, 1), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(true, 1), observableObject => { });

            // property fails on second access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(false, 2), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(true, 2), observableObject => { });

            // property fails due to invalid implementation of Equals
            await
                ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new ObservableObjectWithPropertyFailingDueToInvalidEquals(),
                    observableObject => { });

            // command fails on first access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 2), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 2), observableObject => { });

            // command "CanExecute" fails on first access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 3), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 3), observableObject => { });

            // command fails on second access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 4), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 4), observableObject => { });

            // command "CanExecute" fails on second access
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 5), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 5), observableObject => { });

            // raising change notification for commands fails
            EventHandler canExecuteChangedEventHandler = (sender, e) => { throw new NotSupportedException(); };
            var threadAffineObservableObject = new SampleObservableObject(false);
            threadAffineObservableObject.Command = new Command(() => { }, () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            threadAffineObservableObject.Command.CanExecuteChanged += canExecuteChangedEventHandler;
            try
            {
                await
                    ObservableObjectAssert.RaisesPropertyChangeNotifications(
                        threadAffineObservableObject,
                        observableObject => observableObject.ValueTypeProp++,
                        nameof(SampleObservableObject.ValueTypeProp));
            }
            finally
            {
                threadAffineObservableObject.Command.CanExecuteChanged -= canExecuteChangedEventHandler;
            }

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    // raising change notification for properties fails
                    await
                        _Notifications_WithFailingPropertyChangeNotification(
                            new SampleObservableObject(false),
                            observableObject => observableObject.ValueTypeProp++,
                            "ValueTypeProp");

                    // raising change notification for commands fails
                    await
                        _Notifications_WithFailingPropertyChangeNotification(
                            new SampleObservableObject(false),
                            observableObject => observableObject.Command = new Command(() => { }),
                            "Command");
                });
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        static void _Registration_GarbageCollectedObjects_Core()
        {
            PropertyChangedEventHandler eventHandler = (sender, e) => { };

            var threadAffineObservableObject = new SampleObservableObject(false);
            var threadSafeObservableObject = new SampleObservableObject(true);

            threadAffineObservableObject.PropertyChanged += eventHandler;
            threadSafeObservableObject.PropertyChanged += eventHandler;

            var threadAffineWeakReference = new WeakReference<SampleObservableObject>(threadAffineObservableObject);
            var threadSafeWeakReference = new WeakReference<SampleObservableObject>(threadSafeObservableObject);

            Thread.Sleep(100);

            threadAffineObservableObject = null;
            threadSafeObservableObject = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Thread.Sleep(1000);

            SampleObservableObject target;
            threadAffineWeakReference.TryGetTarget(out target);
            Assert.IsNull(target);
            threadSafeWeakReference.TryGetTarget(out target);
            Assert.IsNull(target);
        }

        [TestMethod]
        public async Task _Registration_GarbageCollectedObjects()
            => await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    _Registration_GarbageCollectedObjects_Core();
                });

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Registration_NotWellDefinedObjects()
        {
            PropertyChangedEventHandler eventHandler = (sender, args) => { };

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    var observableObjectNotWellDefined = new ObservableObjectNotWellDefined();
                    observableObjectNotWellDefined.PropertyChanged += eventHandler;
                    observableObjectNotWellDefined.PropertyChanged -= eventHandler;

                    var observableObjectWithoutObservableProperties = new ObservableObjectWithoutObservableProperties();
                    observableObjectWithoutObservableProperties.PropertyChanged += eventHandler;
                    observableObjectWithoutObservableProperties.PropertyChanged -= eventHandler;
                });

            var littleKnownObservableObjectWithoutObservableProperties = new LittleKnownObservableObjectWithoutObservableProperties();
            littleKnownObservableObjectWithoutObservableProperties.PropertyChanged += eventHandler;
            littleKnownObservableObjectWithoutObservableProperties.PropertyChanged -= eventHandler;
        }

        [TestMethod]
        public void _Registration_OffApplicationMonitorScope()
        {
            var observableObject = new SampleObservableObject(false);

            PropertyChangedEventHandler eventHandler = (sender, e) => Assert.Fail("The event handler should not be raised.");

            observableObject.PropertyChanged += eventHandler;
            try
            {
                observableObject.ValueTypeProp++;
            }
            finally
            {
                observableObject.PropertyChanged -= eventHandler;
            }
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task _Registration_MultipleTimes()
            => await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    PropertyChangedEventHandler eventHandler = (sender, args) => { };

                    var observableObjects = new[]
                    {
                        new SampleObservableObject(false), new SampleObservableObject(false), new SampleObservableObject(true),
                        new SampleObservableObject(true)
                    };

                    foreach (var observableObject in observableObjects)
                    {
                        observableObject.PropertyChanged += eventHandler;
                        observableObject.PropertyChanged += eventHandler;
                    }

                    foreach (var observableObject in observableObjects)
                    {
                        observableObject.PropertyChanged -= eventHandler;
                        observableObject.PropertyChanged -= eventHandler;
                    }
                });
    }
}