using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public sealed class ParameterlessCommandTests
    {
        [TestMethod]
        public void _Ctor_Exceptions()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command((null as Action)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command((null as Action<CancellationToken>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(
                () => new Command((null as Action<CommandExecutionController, CancellationToken>)!),
                "execute");
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command>> GetTestArgs(
            CommandBaseTests.ReentrancyContainer<Command> container,
            Action execute,
            Action<CancellationToken> executeWithCancellationToken,
            Action<CommandExecutionController, CancellationToken> executeWithController)
        {
            var nonDefaultOptions = new CommandOptions(true);

            yield return CommandBaseTests.TestArgs.Create(new Command(execute), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(execute, () => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(execute, () => false), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(execute, options: nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(execute, () => true, nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(execute, () => false, nonDefaultOptions), false, true, container);

            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken, () => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken, () => false), false, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken, options: nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken, () => true, nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command(executeWithCancellationToken, () => false, nonDefaultOptions), false, true, container);

            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController, () => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController, () => false), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController, options: nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController, () => true, nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command(executeWithController, () => false, nonDefaultOptions), false, true, container);
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command>> _ExecutionSuccessful()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command>();

            void Execute()
            {
                counter.Execution++;

                if (reentrancyContainer.Command!.RunningExecution != null)
                {
                    counter.RunningExecution++;

                    counter.CancelCommand++;

                    if (reentrancyContainer.Command.RunningExecution!.CancelCommand.CanExecute() == reentrancyContainer.IsCancelCommandEnabled)
                    {
                        counter.IsCancelCommandEnabled++;
                    }

                    if (Math.Abs(reentrancyContainer.Command.RunningExecution.Progress) < float.Epsilon)
                    {
                        counter.Progress++;
                    }
                }

                if (!reentrancyContainer.Command.CanExecute())
                {
                    counter.CanExecuteWhileExecuting++;
                }

                // nested execution
                reentrancyContainer.Command.Execute();
            }

            void ExecuteWithCancellationToken(CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    counter.CanBeCanceled++;
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    counter.CancellationRequest++;
                }

                Execute();
            }

            void ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                counter.Controller++;

                if (controller.Execution == reentrancyContainer.Command!.RunningExecution)
                {
                    counter.SameRunningExecution++;
                }

                ExecuteWithCancellationToken(cancellationToken);

                ExceptionAssert.Throws<ArgumentOutOfRangeException>(() => controller.ReportProgress(-0.1f), "value");
                ExceptionAssert.Throws<ArgumentOutOfRangeException>(() => controller.ReportProgress(1.1f), "value");

                controller.ReportProgress(0.3f);
                if (Math.Abs(controller.Execution.Progress - 0.3f) < float.Epsilon)
                {
                    counter.ProgressReporting++;
                }

                ((IProgress<float>)controller).Report(-0.1f);
                ((IProgress<float>)controller).Report(1.1f);
                ((IProgress<float>)controller).Report(0.35f);
                if (Math.Abs(controller.Execution.Progress - 0.35f) < float.Epsilon)
                {
                    counter.ProgressReportingAsIProgress++;
                }

                controller.DisableCancelCommand();
                if (!controller.Execution.CancelCommand.CanExecute())
                {
                    counter.DisabledCancelCommand++;
                }

                reentrancyContainer.CapturedController = controller;
            }

            foreach (var args in GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController))
            {
                yield return args;
            }

            Assert.AreEqual(reentrancyContainer.ExecutionCount, counter.Execution);
            Assert.AreEqual(counter.Execution, counter.RunningExecution);
            Assert.AreEqual(counter.Execution, counter.CancelCommand);
            Assert.AreEqual(counter.Execution, counter.IsCancelCommandEnabled);
            Assert.AreEqual(counter.Execution, counter.Progress);
            Assert.AreEqual(counter.Execution, counter.CanExecuteWhileExecuting);

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3 * 2, counter.CanBeCanceled);
            Assert.AreEqual(counter.CanBeCanceled, counter.CancellationRequest);

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3, counter.Controller);
            Assert.AreEqual(counter.Controller, counter.SameRunningExecution);
            Assert.AreEqual(counter.Controller, counter.ProgressReporting);
            Assert.AreEqual(counter.Controller, counter.ProgressReportingAsIProgress);
            Assert.AreEqual(counter.Controller, counter.DisabledCancelCommand);
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful))]
        public void _ExecutionSuccessful(CommandBaseTests.TestArgs<Command> args)
        {
            Assert.AreEqual(args.IsCancelCommandEnabled, args.Command.Options.IsCancelCommandEnabled);
            Assert.AreEqual(args.CanExecute, args.Command.CanExecute());
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));

            args.UpdateReentrancyContainer(2);

            NullAssert.IsNull(args.Command.RunningExecution);
            NullAssert.IsNull(args.Command.CompletedExecution);

            args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(null!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command>> _ExecutionCanceled()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command>();

            void Execute()
            {
                reentrancyContainer.Command!.RunningExecution!.CancelCommand.Execute();

                throw new OperationCanceledException();
            }

            void ExecuteWithCancellationToken(CancellationToken cancellationToken)
            {
                try
                {
                    Execute();
                }
                finally
                {
                    if (reentrancyContainer.IsCancelCommandEnabled)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            counter.CancellationRequest++;
                        }
                    }
                    else
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            counter.CancellationRequest++;
                        }
                    }
                }
            }

            void ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                controller.ReportProgress(0.4f);

                ExecuteWithCancellationToken(cancellationToken);
            }

            foreach (var args in GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController))
            {
                yield return args;
            }

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3 * 2, counter.CancellationRequest);
        }

        [DataDrivenTestMethod(nameof(_ExecutionCanceled))]
        public void _ExecutionCanceled(CommandBaseTests.TestArgs<Command> args)
        {
            args.UpdateReentrancyContainer(2);

            NullAssert.IsNull(args.Command.RunningExecution);
            NullAssert.IsNull(args.Command.CompletedExecution);

            args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            ((ICommand)args.Command).Execute(null!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command>> _ExecutionFailed()
        {
            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command>();

            static void Execute() => throw new InvalidOperationException();

            void ExecuteWithCancellationToken(CancellationToken cancellationToken) => Execute();

            void ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                controller.ReportProgress(0.4f);

                ExecuteWithCancellationToken(cancellationToken);
            }

            return GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed))]
        public void _ExecutionFailed(CommandBaseTests.TestArgs<Command> args)
        {
            args.UpdateReentrancyContainer(2);

            NullAssert.IsNull(args.Command.RunningExecution);
            NullAssert.IsNull(args.Command.CompletedExecution);

            args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            ((ICommand)args.Command).Execute(null!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }
    }
}