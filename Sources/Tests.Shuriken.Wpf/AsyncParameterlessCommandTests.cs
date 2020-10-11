using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public sealed class AsyncParameterlessCommandTests
    {
        [TestMethod]
        public void _Ctor_Exceptions()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new AsyncCommand((null as Func<Task>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(() => new AsyncCommand((null as Func<CancellationToken, Task>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(
                () => new AsyncCommand((null as Func<CommandExecutionController, CancellationToken, Task>)!),
                "execute");
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand>> GetTestArgs(
            CommandBaseTests.ReentrancyContainer<AsyncCommand> container,
            Func<Task> execute,
            Func<CancellationToken, Task> executeWithCancellationToken,
            Func<CommandExecutionController, CancellationToken, Task> executeWithController)
        {
            var nonDefaultOptions = new CommandOptions(false);

            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute, options: nonDefaultOptions), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute, () => true, nonDefaultOptions), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute, () => false, nonDefaultOptions), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute, () => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(execute, () => false), false, true, container);

            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithCancellationToken, options: nonDefaultOptions), true, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(
                    new AsyncCommand(executeWithCancellationToken, () => true, nonDefaultOptions),
                    true,
                    false,
                    container);
            yield return
                CommandBaseTests.TestArgs.Create(
                    new AsyncCommand(executeWithCancellationToken, () => false, nonDefaultOptions),
                    false,
                    false,
                    container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithCancellationToken), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithCancellationToken, () => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithCancellationToken, () => false), false, true, container);

            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController, options: nonDefaultOptions), true, false, container)
                ;
            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController, () => true, nonDefaultOptions), true, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController, () => false, nonDefaultOptions), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController, () => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand(executeWithController, () => false), false, true, container);
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand>> _ExecutionSuccessful()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand>();

            async Task Execute()
            {
                await Task.Yield();

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
                await reentrancyContainer.Command.Execute().ConfigureAwait(false);
            }

            async Task ExecuteWithCancellationToken(CancellationToken cancellationToken)
            {
                await Task.Yield();

                if (cancellationToken.CanBeCanceled)
                {
                    counter.CanBeCanceled++;
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    counter.CancellationRequest++;
                }

                await Execute().ConfigureAwait(false);
            }

            async Task ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                counter.Controller++;

                if (controller.Execution == reentrancyContainer.Command!.RunningExecution)
                {
                    counter.SameRunningExecution++;
                }

                await ExecuteWithCancellationToken(cancellationToken);

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
        [Timeout(1_000)]
        public async Task _ExecutionSuccessful(CommandBaseTests.TestArgs<AsyncCommand> args)
        {
            Assert.AreEqual(args.IsCancelCommandEnabled, args.Command.Options.IsCancelCommandEnabled);
            Assert.AreEqual(args.CanExecute, args.Command.CanExecute());
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));

            args.UpdateReentrancyContainer(3);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(null!);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            args.Command.StartExecute();
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand>> _ExecutionCanceled()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand>();

            async Task Execute()
            {
                await Task.Yield();

                reentrancyContainer.Command!.RunningExecution!.CancelCommand.Execute();

                throw new OperationCanceledException();
            }

            async Task ExecuteWithCancellationToken(CancellationToken cancellationToken)
            {
                try
                {
                    await Execute();
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

            async Task ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                controller.ReportProgress(0.4f);

                await ExecuteWithCancellationToken(cancellationToken).ConfigureAwait(false);
            }

            foreach (var args in GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController))
            {
                yield return args;
            }

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3 * 2, counter.CancellationRequest);
        }

        [DataDrivenTestMethod(nameof(_ExecutionCanceled))]
        [Timeout(1_000)]
        public async Task _ExecutionCanceled(CommandBaseTests.TestArgs<AsyncCommand> args)
        {
            args.UpdateReentrancyContainer(3);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            ((ICommand)args.Command).Execute(null!);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            args.Command.StartExecute();
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand>> _ExecutionFailed()
        {
            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand>();

            static async Task Execute()
            {
                await Task.Yield();

                throw new InvalidOperationException();
            }

            Task ExecuteWithCancellationToken(CancellationToken cancellationToken) => Execute();

            async Task ExecuteWithController(CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                controller.ReportProgress(0.4f);

                await ExecuteWithCancellationToken(cancellationToken).ConfigureAwait(false);
            }

            return GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed))]
        [Timeout(1_000)]
        public async Task _ExecutionFailed(CommandBaseTests.TestArgs<AsyncCommand> args)
        {
            args.UpdateReentrancyContainer(3);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute();
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            ((ICommand)args.Command).Execute(null!);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            args.Command.StartExecute();
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<AsyncCommand> _ExecutionFailed_NullTask()
        {
            yield return new AsyncCommand(() => null!);
            yield return new AsyncCommand(cancellationToken => null!);
            yield return new AsyncCommand((controller, cancellationToken) => null!);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed_NullTask))]
        public async Task _ExecutionFailed_NullTask(AsyncCommand command)
        {
            await command.Execute();

            Assert.IsInstanceOfType(command.CompletedExecution!.Exception!, typeof(InvalidOperationException));
        }
    }
}