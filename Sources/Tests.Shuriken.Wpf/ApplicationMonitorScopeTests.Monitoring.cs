using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [SuppressMessage("ReSharper", "ThrowExceptionInUnexpectedLocation")]
    [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    [SuppressMessage("ReSharper", "NotAccessedVariable")]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    partial class ApplicationMonitorScopeTests
    {
        class SampleObservableObject : ObservableObject
        {
            int valueTypeValue;
            string? referenceTypeValue;

            public SampleObservableObject(bool isThreadSafe) : base(isThreadSafe) => Assert.AreEqual(isThreadSafe, IsThreadSafe);

            [Observable]
            public int ValueTypeProp
            {
                get => IsThreadSafe ? Interlocked.CompareExchange(ref valueTypeValue, 0, 0) : valueTypeValue;
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
            public virtual string? ReferenceTypeProp
            {
                get => IsThreadSafe ? Interlocked.CompareExchange(ref referenceTypeValue, null, null) : referenceTypeValue;
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
            public Command? Command { get; set; }

            [Observable]
            public Command<int>? Command2 { get; set; }

            [Observable]
            public AsyncCommand? Command3 { get; set; }

            [Observable]
            public AsyncCommand<int>? Command4 { get; set; }

            [Observable]
            public ParameterlessCommand? Command5 { get; set; }

            [Observable]
            public ParameterizedCommand<int>? Command6 { get; set; }

            [UsedImplicitly]
            public string? this[int index] => null;

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

        sealed class ObservableObjectWithFailingCommand<T> : ObservableObject
        {
            readonly Command<T> command;

            readonly int maxNonFailingAccessCount;

            int accessCount;

            public ObservableObjectWithFailingCommand(bool isThreadSafe, int maxNonFailingAccessCount) : base(isThreadSafe)
            {
                Assert.AreEqual(isThreadSafe, IsThreadSafe);

                this.maxNonFailingAccessCount = maxNonFailingAccessCount;

                command = new Command<T>(arg => { }, CanExecuteCommand);
            }

            bool CanExecuteCommand(T arg)
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
            public Command<T> Command
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
                public override int GetHashCode() => throw new NotSupportedException();

                public override bool Equals(object? obj) => throw new NotSupportedException();
            }

            [Observable]
            [UsedImplicitly]
            public ValueWithInvalidEquals Prop { get; set; }
        }

        sealed class ObservableObjectNotWellDefined : ObservableObject
        {
            public struct MutableValue { }

            public int WriteOnly
            {
                set { }
            }

            [Observable]
            public MutableValue Mutable { get; set; }

            [Observable]
            public string? this[int index] => null;
        }

        sealed class LittleKnownObservableObjectWithoutObservableProperties : ObservableObject
        {
            [Observable]
            [UsedImplicitly]
            public string? this[int index] => null;
        }

        static async Task _Notifications_WithFailingPropertyChangeNotification<T>(
            T observableObject,
            Action<T> changeProperty,
            string propertyName) where T : ObservableObject
        {
            var source = new TaskCompletionSource<int>();

            void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
            {
                try
                {
                    Assert.AreSame(observableObject, sender!);
                    Assert.IsNotNull(e);
                    Assert.AreEqual(propertyName, e.PropertyName);

                    throw new NotSupportedException();
                }
                finally
                {
                    source.TrySetResult(0);
                }
            }

            observableObject.PropertyChanged += PropertyChangedEventHandler;
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
                observableObject.PropertyChanged -= PropertyChangedEventHandler;
            }
        }

        [TestMethod]
        public async Task _Notifications_Properties()
        {
            // value type property
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject => observableObject.ValueTypeProp++,
                nameof(SampleObservableObject.ValueTypeProp));
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(true),
                observableObject => observableObject.ValueTypeProp++,
                nameof(SampleObservableObject.ValueTypeProp));

            // reference type property
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject => observableObject.ReferenceTypeProp += "a",
                nameof(SampleObservableObject.ReferenceTypeProp));
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(true),
                observableObject => observableObject.ReferenceTypeProp += "a",
                nameof(SampleObservableObject.ReferenceTypeProp));

            // indexer
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false),
                    observableObject => observableObject.NotifyIndexer(),
                    Binding.IndexerName)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_Command_Parameterless()
        {
            // property change notification
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject => observableObject.Command = new Command(() => { }),
                nameof(SampleObservableObject.Command));

            // change notification because of CanExecute changes
            var threadAffineObservableObject = new SampleObservableObject(false);
            threadAffineObservableObject.Command = new Command(() => { }, () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                threadAffineObservableObject,
                observableObject => observableObject.ValueTypeProp++,
                new[] { nameof(SampleObservableObject.ValueTypeProp) },
                new[] { nameof(SampleObservableObject.Command) });

            // change notification because of CanExecute changes during the execution
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false) { Command = new Command(() => SpinWait.SpinUntil(() => false, 100)) },
                    observableObject => observableObject.Command!.Execute(),
                    Array.Empty<string>(),
                    new[] { nameof(SampleObservableObject.Command), nameof(SampleObservableObject.Command) })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_Command_Parameterized()
        {
            // property change notification
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject => observableObject.Command2 = new Command<int>(arg => { }),
                nameof(SampleObservableObject.Command2));

            // change notification because of CanExecute changes during the execution
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false) { Command2 = new Command<int>(arg => SpinWait.SpinUntil(() => false, 100)) },
                    observableObject => observableObject.Command2!.Execute(0),
                    Array.Empty<string>(),
                    new[] { nameof(SampleObservableObject.Command2), nameof(SampleObservableObject.Command2) })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_AsyncCommand_Parameterless()
        {
            // property change notification
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject =>
                {
                    observableObject.Command3 = new AsyncCommand(() => Task.CompletedTask);
                    observableObject.Command5 = new AsyncCommand(() => Task.CompletedTask);
                },
                new[] { nameof(SampleObservableObject.Command3), nameof(SampleObservableObject.Command5) },
                Array.Empty<string>());

            // change notification because of CanExecute changes
            var threadAffineObservableObject = new SampleObservableObject(false);
            threadAffineObservableObject.Command3 = new AsyncCommand(
                () => Task.CompletedTask,
                () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            threadAffineObservableObject.Command5 = new AsyncCommand(
                () => Task.CompletedTask,
                () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                threadAffineObservableObject,
                observableObject => observableObject.ValueTypeProp++,
                new[] { nameof(SampleObservableObject.ValueTypeProp) },
                new[] { nameof(SampleObservableObject.Command3), nameof(SampleObservableObject.Command5) });

            // change notification because of CanExecute changes during the execution
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false)
                    {
                        Command3 = new AsyncCommand(() => Task.Delay(100)), Command5 = new AsyncCommand(() => Task.Delay(100)),
                    },
                    observableObject => Task.WhenAll(observableObject.Command3!.Execute(), ((AsyncCommand)observableObject.Command5!).Execute())
                        .GetAwaiter()
                        .GetResult(),
                    Array.Empty<string>(),
                    new[]
                    {
                        nameof(SampleObservableObject.Command3), nameof(SampleObservableObject.Command3), nameof(SampleObservableObject.Command5),
                        nameof(SampleObservableObject.Command5),
                    })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_AsyncCommand_Parameterized()
        {
            // property change notification
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new SampleObservableObject(false),
                observableObject =>
                {
                    observableObject.Command4 = new AsyncCommand<int>(arg => Task.CompletedTask);
                    observableObject.Command6 = new AsyncCommand<int>(arg => Task.CompletedTask);
                },
                new[] { nameof(SampleObservableObject.Command4), nameof(SampleObservableObject.Command6) },
                Array.Empty<string>());

            // change notification because of CanExecute changes during the execution
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new SampleObservableObject(false)
                    {
                        Command4 = new AsyncCommand<int>(arg => Task.Delay(100)), Command6 = new AsyncCommand<int>(arg => Task.Delay(100)),
                    },
                    observableObject => Task.WhenAll(
                            observableObject.Command4!.Execute(0),
                            ((AsyncCommand<int>)observableObject.Command6!).Execute(0))
                        .GetAwaiter()
                        .GetResult(),
                    Array.Empty<string>(),
                    new[]
                    {
                        nameof(SampleObservableObject.Command4), nameof(SampleObservableObject.Command4), nameof(SampleObservableObject.Command6),
                        nameof(SampleObservableObject.Command6),
                    })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithCancelingNotificationContext()
        {
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.FailInvoke,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp),
                    false);
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.FailInvokeAsync,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp),
                    false);
            await
                ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                    InvalidNotificationContextMode.InvokeAsyncFails,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp),
                    false);
            await ExceptionAssert.Throws<InvalidOperationException>(
                    () => ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, OperationCanceledException>(
                        InvalidNotificationContextMode.InvokeAsyncReturnsNull,
                        new SampleObservableObject(false),
                        observableObject => observableObject.ValueTypeProp++,
                        nameof(SampleObservableObject.ValueTypeProp)))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithFailingNotificationContext()
        {
            await ExceptionAssert.Throws<NotSupportedException>(
                () => ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                    InvalidNotificationContextMode.FailInvoke,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp),
                    false));
            await ExceptionAssert.Throws<NotSupportedException>(
                () => ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                    InvalidNotificationContextMode.FailInvokeAsync,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp)));
            await ExceptionAssert.Throws<NotSupportedException>(
                () => ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                    InvalidNotificationContextMode.InvokeAsyncFails,
                    new SampleObservableObject(false),
                    observableObject => observableObject.ValueTypeProp++,
                    nameof(SampleObservableObject.ValueTypeProp)));
            await ExceptionAssert.Throws<InvalidOperationException>(
                    () => ExecuteInApplicationMonitorScopeWithFailingNotificationContext<SampleObservableObject, NotSupportedException>(
                        InvalidNotificationContextMode.InvokeAsyncReturnsNull,
                        new SampleObservableObject(false),
                        observableObject => observableObject.ValueTypeProp++,
                        nameof(SampleObservableObject.ValueTypeProp)))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_PropertyFailsOnFirstAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(false, 1), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(true, 1), observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_PropertyFailsOnSecondAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(false, 2), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingProperty(true, 2), observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public Task _Notifications_WithExceptionsWhileMonitoring_PropertyFailsDueToInvalidImplementationOfEquals()
            => ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new ObservableObjectWithPropertyFailingDueToInvalidEquals(),
                observableObject => { });

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_CommandFailsOnFirstAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 2), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 2), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new ObservableObjectWithFailingCommand<int>(false, 2),
                observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new ObservableObjectWithFailingCommand<int>(true, 2),
                    observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_CommandCanExecuteFailsOnFirstAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 3), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 3), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new ObservableObjectWithFailingCommand<int>(false, 3),
                observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new ObservableObjectWithFailingCommand<int>(true, 3),
                    observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_CommandFailsOnSecondAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 4), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 4), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new ObservableObjectWithFailingCommand<int>(false, 4),
                observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new ObservableObjectWithFailingCommand<int>(true, 4),
                    observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_CommandCanExecuteFailsOnSecondAccess()
        {
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(false, 5), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(new ObservableObjectWithFailingCommand(true, 5), observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                new ObservableObjectWithFailingCommand<int>(false, 5),
                observableObject => { });
            await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    new ObservableObjectWithFailingCommand<int>(true, 5),
                    observableObject => { })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task _Notifications_WithExceptionsWhileMonitoring_RaisingChangeNotificationForCommandFails()
        {
            static void CanExecuteChangedEventHandler(object? sender, EventArgs e) => throw new NotSupportedException();
            var threadAffineObservableObject = new SampleObservableObject(false);
            threadAffineObservableObject.Command = new Command(() => { }, () => threadAffineObservableObject.ValueTypeProp % 2 == 1);
            threadAffineObservableObject.Command4 = new AsyncCommand<int>(arg => Task.Delay(100));
            threadAffineObservableObject.Command.CanExecuteChanged += CanExecuteChangedEventHandler;
            threadAffineObservableObject.Command4.CanExecuteChanged += CanExecuteChangedEventHandler;
            try
            {
                await ObservableObjectAssert.RaisesPropertyChangeNotifications(
                    threadAffineObservableObject,
                    observableObject =>
                    {
                        observableObject.ValueTypeProp++;
                        observableObject.Command4!.Execute(0).GetAwaiter().GetResult();
                    },
                    nameof(SampleObservableObject.ValueTypeProp));
            }
            finally
            {
                threadAffineObservableObject.Command.CanExecuteChanged -= CanExecuteChangedEventHandler;
                threadAffineObservableObject.Command4.CanExecuteChanged -= CanExecuteChangedEventHandler;
            }

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                    async monitorScope =>
                    {
                        // raising change notification for properties fails
                        await _Notifications_WithFailingPropertyChangeNotification(
                            new SampleObservableObject(false),
                            observableObject => observableObject.ValueTypeProp++,
                            nameof(SampleObservableObject.ValueTypeProp));

                        // raising change notification for commands fails
                        await _Notifications_WithFailingPropertyChangeNotification(
                            new SampleObservableObject(false),
                            observableObject => observableObject.Command = new Command(() => { }),
                            nameof(SampleObservableObject.Command));
                        await _Notifications_WithFailingPropertyChangeNotification(
                                new SampleObservableObject(false),
                                observableObject => observableObject.Command2 = new Command<int>(arg => { }),
                                nameof(SampleObservableObject.Command2))
                            .ConfigureAwait(false);
                    })
                .ConfigureAwait(false);
        }

        [TestMethod]
        public Task _Registration_GarbageCollectedObjects()
        {
            var threadAffineWeakReference = null as WeakReference<SampleObservableObject>;
            var threadSafeWeakReference = null as WeakReference<SampleObservableObject>;

            return ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) { }

                    SampleObservableObject? threadAffineObservableObject = new SampleObservableObject(false);
                    SampleObservableObject? threadSafeObservableObject = new SampleObservableObject(true);

                    threadAffineObservableObject.PropertyChanged += OnPropertyChanged;
                    threadSafeObservableObject.PropertyChanged += OnPropertyChanged;

                    threadAffineWeakReference = new WeakReference<SampleObservableObject>(threadAffineObservableObject);
                    threadSafeWeakReference = new WeakReference<SampleObservableObject>(threadSafeObservableObject);

                    Thread.Sleep(100);

                    threadAffineObservableObject = null;
                    threadSafeObservableObject = null;
                });
        }

        [TestMethod]
        public async Task _Registration_NotWellDefinedObjects()
        {
            static void EventHandler(object? sender, PropertyChangedEventArgs args) { }

            await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    var observableObjectNotWellDefined = new ObservableObjectNotWellDefined();
                    observableObjectNotWellDefined.PropertyChanged += EventHandler;
                    observableObjectNotWellDefined.PropertyChanged -= EventHandler;
                });

            var littleKnownObservableObjectWithoutObservableProperties = new LittleKnownObservableObjectWithoutObservableProperties();
            littleKnownObservableObjectWithoutObservableProperties.PropertyChanged += EventHandler;
            littleKnownObservableObjectWithoutObservableProperties.PropertyChanged -= EventHandler;
        }

        [TestMethod]
        public void _Registration_OffApplicationMonitorScope()
        {
            var observableObject = new SampleObservableObject(false);

            static void EventHandler(object? sender, PropertyChangedEventArgs e) => Assert.Fail("The event handler should not be raised.");

            observableObject.PropertyChanged += EventHandler;
            try
            {
                observableObject.ValueTypeProp++;
            }
            finally
            {
                observableObject.PropertyChanged -= EventHandler;
            }
        }

        [TestMethod]
        public Task _Registration_MultipleTimes()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    await Task.Yield();

                    static void EventHandler(object? sender, PropertyChangedEventArgs args) { }

                    var observableObjects = new[]
                    {
                        new SampleObservableObject(false), new SampleObservableObject(false), new SampleObservableObject(true),
                        new SampleObservableObject(true),
                    };

                    foreach (var observableObject in observableObjects)
                    {
                        observableObject.PropertyChanged += EventHandler;
                        observableObject.PropertyChanged += EventHandler;
                    }

                    foreach (var observableObject in observableObjects)
                    {
                        observableObject.PropertyChanged -= EventHandler;
                        observableObject.PropertyChanged -= EventHandler;
                    }
                });
    }
}