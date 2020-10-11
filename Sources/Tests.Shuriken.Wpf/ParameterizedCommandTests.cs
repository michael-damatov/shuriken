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
    public sealed class ParameterizedCommandTests
    {
        [TestMethod]
        public void _Ctor_Exceptions()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command<int>((null as Action<int>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command<int>((null as Action<int, CancellationToken>)!), "execute");
            ExceptionAssert.Throws<ArgumentNullException>(
                () => new Command<int>((null as Action<int, CommandExecutionController, CancellationToken>)!),
                "execute");
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command<T>>> GetTestArgs<T>(
            CommandBaseTests.ReentrancyContainer<Command<T>> container,
            Action<T> execute,
            Action<T, CancellationToken> executeWithCancellationToken,
            Action<T, CommandExecutionController, CancellationToken> executeWithController)
        {
            var nonDefaultOptions = new CommandOptions(true);

            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute, arg => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute, arg => false), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute, options: nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute, arg => true, nonDefaultOptions), true, true, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(execute, arg => false, nonDefaultOptions), false, true, container);

            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithCancellationToken), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithCancellationToken, arg => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithCancellationToken, arg => false), false, false, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command<T>(executeWithCancellationToken, options: nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command<T>(executeWithCancellationToken, arg => true, nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(
                    new Command<T>(executeWithCancellationToken, arg => false, nonDefaultOptions),
                    false,
                    true,
                    container);

            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController, arg => true), true, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController, arg => false), false, false, container);
            yield return CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController, options: nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController, arg => true, nonDefaultOptions), true, true, container);
            yield return
                CommandBaseTests.TestArgs.Create(new Command<T>(executeWithController, arg => false, nonDefaultOptions), false, true, container);
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command<T>>> _ExecutionSuccessful<T>()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command<T>>();

            void Execute(T arg)
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
                reentrancyContainer.Command.Execute(arg);
            }

            void ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    counter.CanBeCanceled++;
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    counter.CancellationRequest++;
                }

                Execute(arg);
            }

            void ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                counter.Controller++;

                if (controller.Execution == reentrancyContainer.Command!.RunningExecution)
                {
                    counter.SameRunningExecution++;
                }

                ExecuteWithCancellationToken(arg, cancellationToken);

                ExceptionAssert.Throws<ArgumentOutOfRangeException>(() => controller.ReportProgress(-0.1f), "value");
                ExceptionAssert.Throws<ArgumentOutOfRangeException>(() => controller.ReportProgress(1.1f), "value");

                controller.ReportProgress(0.3f);
                if (Math.Abs(controller.Execution.Progress - 0.3f) < float.Epsilon)
                {
                    counter.ProgressReporting++;
                }

                controller.DisableCancelCommand();
                if (!controller.Execution.CancelCommand.CanExecute())
                {
                    counter.DisabledCancelCommand++;
                }

                ((IProgress<float>)controller).Report(-0.1f);
                ((IProgress<float>)controller).Report(1.1f);
                ((IProgress<float>)controller).Report(0.35f);
                if (Math.Abs(controller.Execution.Progress - 0.35f) < float.Epsilon)
                {
                    counter.ProgressReportingAsIProgress++;
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

        static void _ExecutionSuccessful<T>(CommandBaseTests.TestArgs<Command<T>> args, int nonGenericExecutionCount)
        {
            Assert.AreEqual(args.IsCancelCommandEnabled, args.Command.Options.IsCancelCommandEnabled);
            Assert.AreEqual(args.CanExecute, args.Command.CanExecute(default!));

            args.UpdateReentrancyContainer(1 + nonGenericExecutionCount);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            args.Command.Execute(default!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute(null!);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            ((ICommand)args.Command).Execute("");
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Done, true, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(int))]
        public void _ExecutionSuccessful_ValueType(CommandBaseTests.TestArgs<Command<int>> args)
        {
            _ExecutionSuccessful(args, 1);

            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(0));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(null!));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(""));
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(int?))]
        public void _ExecutionSuccessful_NullableValueType(CommandBaseTests.TestArgs<Command<int?>> args)
        {
            _ExecutionSuccessful(args, 2);

            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(0));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));
            Assert.IsFalse(((ICommand)args.Command).CanExecute(""));
        }

        [DataDrivenTestMethod(nameof(_ExecutionSuccessful), GenericArgument = typeof(string))]
        public void _ExecutionSuccessful_ReferenceType(CommandBaseTests.TestArgs<Command<string>> args)
        {
            _ExecutionSuccessful(args, 2);

            Assert.IsFalse(((ICommand)args.Command).CanExecute(0));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(null!));
            Assert.AreEqual(args.CanExecute, ((ICommand)args.Command).CanExecute(""));
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command<T>>> _ExecutionCanceled<T>()
        {
            var counter = new CommandBaseTests.CommandExecutionCounter();

            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command<T>>();

            void Execute(T arg)
            {
                reentrancyContainer.Command!.RunningExecution!.CancelCommand.Execute();

                throw new OperationCanceledException();
            }

            void ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken)
            {
                try
                {
                    Execute(arg);
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

            void ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                controller.ReportProgress(0.4f);

                ExecuteWithCancellationToken(arg, cancellationToken);
            }

            foreach (var args in GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController))
            {
                yield return args;
            }

            Assert.AreEqual(reentrancyContainer.ExecutionCount / 3 * 2, counter.CancellationRequest);
        }

        [DataDrivenTestMethod(nameof(_ExecutionCanceled), GenericArgument = typeof(int))]
        public void _ExecutionCanceled(CommandBaseTests.TestArgs<Command<int>> args)
        {
            args.UpdateReentrancyContainer(2);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            args.Command.Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            ((ICommand)args.Command).Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Canceled, false, false);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }

        static IEnumerable<CommandBaseTests.TestArgs<Command<T>>> _ExecutionFailed<T>()
        {
            var reentrancyContainer = new CommandBaseTests.ReentrancyContainer<Command<T>>();

            static void Execute(T arg) => throw new InvalidOperationException();

            void ExecuteWithCancellationToken(T arg, CancellationToken cancellationToken) => Execute(arg);

            void ExecuteWithController(T arg, CommandExecutionController controller, CancellationToken cancellationToken)
            {
                controller.ReportProgress(0.4f);

                ExecuteWithCancellationToken(arg, cancellationToken);
            }

            return GetTestArgs(reentrancyContainer, Execute, ExecuteWithCancellationToken, ExecuteWithController);
        }

        [DataDrivenTestMethod(nameof(_ExecutionFailed), GenericArgument = typeof(int))]
        public void _ExecutionFailed(CommandBaseTests.TestArgs<Command<int>> args)
        {
            args.UpdateReentrancyContainer(2);

            Assert.IsNull(args.Command.RunningExecution!);
            Assert.IsNull(args.Command.CompletedExecution!);

            args.Command.Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            ((ICommand)args.Command).Execute(0);
            CommandBaseTests.AssertExecutionsAfterCompletion(args, CompletedCommandExecutionState.Faulted, false, true);

            CommandBaseTests.CanExecuteChanged(args.Command);
        }
    }
}