using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public sealed class ObservableObjectTests
    {
        sealed class Ghost : ObservableObject
        {
            [Observable]
            public bool Active { get; set; }
        }

        [TestMethod]
        [DoNotParallelize]
        public Task _GhostUpdates()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    // property changes exactly in time between the monitor captures the values, so the monitor doesn't detect any changes

                    var ghost = new Ghost();

                    var eventRaiseCount = 0;

                    async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                    {
                        eventRaiseCount++;

                        ghost.Active = false;

                        await Task.Delay(1);

                        ghost.Active = true;
                    }

                    ghost.PropertyChanged += OnPropertyChanged;
                    try
                    {
                        await Task.Delay(50).ConfigureAwait(false);

                        ghost.Active = true;

                        await Task.Delay(1_000).ConfigureAwait(false);
                    }
                    finally
                    {
                        ghost.PropertyChanged -= OnPropertyChanged;
                    }

                    Assert.IsTrue(eventRaiseCount > 1);

                    await Task.Delay(50).ConfigureAwait(false);
                });

        sealed class MutableCommands : ObservableObject
        {
            [Observable]
            public Command? ParameterlessCommand { get; set; }

            [Observable]
            public AsyncCommand<int>? ParameterizedCommand { get; set; }
        }

        [TestMethod]
        [DoNotParallelize]
        public Task _MutableCommands()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    // "Command" property becomes null after being assigned, so no further "can execute" change notification may be raised

                    var canExecute = false;

                    var parameterlessCommand = new Command(() => { }, () => canExecute);
                    var parameterizedCommand = new AsyncCommand<int>(_ => Task.Delay(50));

                    var mutableCommands = new MutableCommands
                    {
                        ParameterlessCommand = parameterlessCommand, ParameterizedCommand = parameterizedCommand
                    };

                    var parameterlessCommandEventRaiseCount = 0;
                    var parameterizedCommandEventRaiseCount = 0;

                    void CommandOnCanExecuteChanged(object? sender, EventArgs e) => parameterlessCommandEventRaiseCount++;
                    void AsyncCommandOnCanExecuteChanged(object? sender, EventArgs e) => parameterizedCommandEventRaiseCount++;

                    static void OnPropertyChanged(object sender, PropertyChangedEventArgs e) { }

                    mutableCommands.PropertyChanged += OnPropertyChanged;
                    parameterlessCommand.CanExecuteChanged += CommandOnCanExecuteChanged;
                    parameterizedCommand.CanExecuteChanged += AsyncCommandOnCanExecuteChanged;
                    try
                    {
                        await Task.Delay(50).ConfigureAwait(false);

                        canExecute = true;
                        await mutableCommands.ParameterizedCommand.Execute(0).ConfigureAwait(false);

                        await Task.Delay(100).ConfigureAwait(false);

                        mutableCommands.ParameterlessCommand = null;
                        mutableCommands.ParameterizedCommand = null;

                        await Task.Delay(50).ConfigureAwait(false);
                    }
                    finally
                    {
                        parameterizedCommand.CanExecuteChanged -= AsyncCommandOnCanExecuteChanged;
                        parameterlessCommand.CanExecuteChanged -= CommandOnCanExecuteChanged;
                        mutableCommands.PropertyChanged -= OnPropertyChanged;
                    }

                    Assert.AreEqual(1, parameterlessCommandEventRaiseCount);
                    Assert.AreEqual(2, parameterizedCommandEventRaiseCount);

                    await Task.Delay(50).ConfigureAwait(false);
                });
    }
}