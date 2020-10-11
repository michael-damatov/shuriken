using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    public static class CommandBaseTests
    {
        internal sealed class CommandExecutionCounter
        {
            public int Execution { get; set; }

            public int RunningExecution { get; set; }

            public int CancelCommand { get; set; }

            public int IsCancelCommandEnabled { get; set; }

            public int Progress { get; set; }

            public int CanExecuteWhileExecuting { get; set; }

            public int CanBeCanceled { get; set; }

            public int CancellationRequest { get; set; }

            public int Controller { get; set; }

            public int SameRunningExecution { get; set; }

            public int ProgressReporting { get; set; }

            public int ProgressReportingAsIProgress { get; set; }

            public int DisabledCancelCommand { get; set; }
        }

        internal sealed class ReentrancyContainer<C> where C : CommandBase
        {
            public C? Command { get; private set; }

            public bool IsCancelCommandEnabled { get; private set; }

            public int ExecutionCount { get; private set; }

            public CommandExecutionController? CapturedController { get; set; }

            internal void Update(C command, bool isCancelCommandEnabled, int executionCountIncrement)
            {
                Command = command;
                IsCancelCommandEnabled = isCancelCommandEnabled;

                ExecutionCount += executionCountIncrement;
            }
        }

        internal static class TestArgs
        {
            public static TestArgs<C> Create<C>(C command, bool canExecute, bool isCancelCommandEnabled, ReentrancyContainer<C> reentrancyContainer)
                where C : CommandBase
                => new TestArgs<C>(command, canExecute, isCancelCommandEnabled, reentrancyContainer);
        }

        public sealed class TestArgs<C> where C : CommandBase
        {
            readonly ReentrancyContainer<C> reentrancyContainer;

            internal TestArgs(C command, bool canExecute, bool isCancelCommandEnabled, ReentrancyContainer<C> reentrancyContainer)
            {
                this.reentrancyContainer = reentrancyContainer;

                Command = command;
                CanExecute = canExecute;
                IsCancelCommandEnabled = isCancelCommandEnabled;
            }

            internal C Command { get; }

            internal bool CanExecute { get; }

            internal bool IsCancelCommandEnabled { get; }

            public CommandExecutionController? CapturedController => reentrancyContainer.CapturedController;

            internal void UpdateReentrancyContainer(int executionCount)
                => reentrancyContainer.Update(Command, IsCancelCommandEnabled, CanExecute ? executionCount : 0);
        }

        internal static void CanExecuteChanged<C>(C command) where C : CommandBase
        {
            var eventRaised = false;

            command.CanExecuteChanged += (sender, e) =>
            {
                Assert.AreSame(command, sender!);
                Assert.AreEqual(EventArgs.Empty, e);
                eventRaised = true;
            };

            command.NotifyCanExecuteChanged();

            Assert.IsTrue(eventRaised);
        }

        internal static void AssertExecutionsAfterCompletion<C>(
            TestArgs<C> args,
            CompletedCommandExecutionState state,
            bool isProgressOne,
            bool hasException) where C : CommandBase
        {
            Assert.IsNull(args.Command.RunningExecution!);

            if (args.CanExecute)
            {
                Assert.IsNotNull(args.Command.CompletedExecution!);

                Assert.AreEqual(state, args.Command.CompletedExecution!.State);

                if (isProgressOne)
                {
                    Assert.AreEqual(1, args.Command.CompletedExecution.Progress);
                }
                else
                {
                    Assert.IsTrue(args.Command.CompletedExecution.Progress < 1f);
                }

                if (hasException)
                {
                    Assert.IsNotNull(args.Command.CompletedExecution.Exception!);
                }
                else
                {
                    Assert.IsNull(args.Command.CompletedExecution.Exception!);
                }

                if (args.CapturedController != null)
                {
                    ExceptionAssert.Throws<InvalidOperationException>(() => args.CapturedController.ReportProgress(0.7f));
                    ((IProgress<float>)args.CapturedController).Report(0.75f);
                }
            }
            else
            {
                Assert.IsNull(args.Command.CompletedExecution!);
            }
        }
    }
}