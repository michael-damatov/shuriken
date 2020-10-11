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
    public sealed class AsyncParameterizedCommandTests
    {
        [TestMethod]
        public void _Ctor_Exceptions()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((null as Func<int, Task>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((null as Func<int, CancellationToken, Task>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(
                () => new AsyncCommand<int>((null as Func<int, CommandExecutionController, CancellationToken, Task>)!),
                "execute");
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand<T>>> GetTestArgs<T>(
            CommandBaseTests.ReentrancyContainer<AsyncCommand<T>> container,
            Func<T, Task> execute,
            Func<T, CancellationToken, Task> executeWithCancellationToken,
            Func<T, CommandExecutionController, CancellationToken, Task> executeWithController)
        {
            var nonDefaultOptions = new CommandOptions(false);

            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute, options: nonDefaultOptions), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute, arg => true, nonDefaultOptions), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute, arg => false, nonDefaultOptions), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute, arg => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(execute, arg => false), false, true, container);

            yield return
                CommandBaseTests.TestArgs.Create(
                    new AsyncCommand<T>(executeWithCancellationToken, options: nonDefaultOptions),
                    true,
                    false,
                    container);
            yield return
                CommandBaseTests.TestArgs.Create(
                    new AsyncCommand<T>(executeWithCancellationToken, arg => true, nonDefaultOptions),
                    true,
                    false,
                    container);
            yield return
                CommandBaseTests.TestArgs.Create(
                    new AsyncCommand<T>(executeWithCancellationToken, arg => false, nonDefaultOptions),
                    false,
                    false,
                    container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithCancellationToken), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithCancellationToken, arg => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithCancellationToken, arg => false), false, true, container);

            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController, options: nonDefaultOptions), true, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController, arg => true, nonDefaultOptions), true, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController, arg => false, nonDefaultOptions), false, false, container)
                ;
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController, arg => true), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new AsyncCommand<T>(executeWithController, arg => false), false, true, container);
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand<T>>> _ExecutionSuccessful<T>()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand<T>>();

            async Task Execute(T arg)
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

                    if (Math.Abs(reentrancyContainer.Command.RunningExecution!.Progress) < float.Epsilon)
                    {
                        counter.Progress++;
                    }
                }

                if (!reentrancyContainer.Command.CanExecute(default!))
                {
                    counter.CanExecuteWhileExecuting++;
                }

                // nested execution
                await reentrancyContainer.Command.Execute(arg).ConfigureAwait(false);
            }

            async Task ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken)
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

                await Execute(arg).ConfigureAwait(false);
            }

            async Task ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                counter.Controller++;

                if (controller.Execution == reentrancyContainer.Command!.RunningExecution)
                {
                    counter.SameRunningExecution++;
                }

                await ExecuteWithCancellationToken(arg, cancellationToken);

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

        static async Task _ExecutionSuccessful<T>(CommandBaseTests.TestArgs<AsyncCommand<T>> args, int nonGenericExecutionCount)
        {
            Assert.AreEqual(args.IsCancelCommandEnabled, args.Command.Options.IsCancelCommandEnabled);
            Assert.AreEqual(args.CanExecute, args.Command.CanExecute(default!));

            args.UpdateReentrancyContainer(1 + nonGenericExecutionCount + 1);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute(default!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(0);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(null!);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute("");
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            args.Command.StartExecute(default!);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(int))]
        [Timeout(1_000)]
        public async Task _ExecutionSuccessful_ValueType(CommandBaseTests.TestArgs<AsyncCommand<int>> args)
        {
            await _ExecutionSuccessful(args, 1);

            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(0));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(null!));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(""));
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(int?))]
        [Timeout(1_000)]
        public async Task _ExecutionSuccessful_NullableValueType(CommandBaseTests.TestArgs<AsyncCommand<int?>> args)
        {
            await _ExecutionSuccessful(args, 2);

            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(0));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(""));
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(string))]
        [Timeout(1_000)]
        public async Task _ExecutionSuccessful_ReferenceType(CommandBaseTests.TestArgs<AsyncCommand<string>> args)
        {
            await _ExecutionSuccessful(args, 2);

            Assert.IsFalse(((ICommand)args.Command).CanExecute(0));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(""));
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand<T>>> _ExecutionCanceled<T>()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand<T>>();

            async Task Execute(T arg)
            {
                await Task.Yield();

                reentrancyContainer.Command!.RunningExecution!.CancelCommand.Execute();

                throw new OperationCanceledException();
            }

            async Task ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken)
            {
                try
                {
                    await Execute(arg);
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

            async Task ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                controller.ReportProgress(0.4f);

                await ExecuteWithCancellationToken(arg, cancellationToken).ConfigureAwait(false);
            }

            foreach (var args in GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController))
            {
                yield return args;
            }

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3 * 2, counter.CancellationRequest);
        }

        [DataDrivenTestMethod(nameof(_ExecutionCanceled), GenericArgument = typeof(int))]
        [Timeout(1_000)]
        public async Task _ExecutionCanceled(CommandBaseTests.TestArgs<AsyncCommand<int>> args)
        {
            args.UpdateReentrancyContainer(3);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            ((ICommand)args.Command).Execute(0);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            args.Command.StartExecute(0);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<AsyncCommand<T>>> _ExecutionFailed<T>()
        {
            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<AsyncCommand<T>>();

            static async Task Execute(T arg)
            {
                await Task.Yield();

                throw new InvalidOperationException();
            }

            Task ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken) => Execute(arg);

            async Task ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                await Task.Yield();

                controller.ReportProgress(0.4f);

                await ExecuteWithCancellationToken(arg, cancellationToken).ConfigureAwait(false);
            }

            return GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed), GenericArgument = typeof(int))]
        [Timeout(1_000)]
        public async Task _ExecutionFailed(CommandBaseTests.TestArgs<AsyncCommand<int>> args)
        {
            args.UpdateReentrancyContainer(3);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            await args.Command.Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            ((ICommand)args.Command).Execute(0);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            args.Command.StartExecute(0);
            while (args.Command.RunningExecution != null)
            {
                await Task.Delay(100);
            }
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<AsyncCommand<T>> _ExecutionFailed_NullTask<T>()
        {
            yield return new AsyncCommand<T>(arg => null!);
            yield return new AsyncCommand<T>((arg, cancellationToken) => null!);
            yield return new AsyncCommand<T>((arg, controller, cancellationToken) => null!);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed_NullTask), GenericArgument = typeof(int))]
        public async Task _ExecutionFailed_NullTask(AsyncCommand<int> command)
        {
            await command.Execute(0);

            Assert.IsInstanceOfType(command.CompletedExecution!.Exception!, typeof(InvalidOperationException));
        }
    }
}