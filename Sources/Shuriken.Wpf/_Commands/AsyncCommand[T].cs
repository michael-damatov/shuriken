using System;
using System.Threading;
using System.Threading.Tasks;
using Shuriken.Diagnostics;

namespace Shuriken
{
    /// <summary>
    /// Represents an asynchronous parameterized command. The <see cref="ParameterizedCommand{T}.CanExecute"/> method always returns <c>false</c>
    /// while the command is being executed.
    /// </summary>
    /// <typeparam name="T">The command parameter type.</typeparam>
    /// <remarks>
    /// Although the class is a thread-safe <see cref="ParameterizedCommand{T}.CanExecute"/>, the <see cref="ParameterizedCommand{T}"/> method is
    /// called in the same context as the parent <see cref="ObservableAttribute"/> (if the corresponding command property is annotated with the
    /// <see cref="ObservableObject"/>), i.e. only if the parent <see cref="ParameterizedCommand{T}.CanExecute"/> is thread-safe the
    /// <see cref="ParameterizedCommand{T}"/> method will be invoked in a background thread.<para />
    /// ALWAYS annotate <see cref="ObservableAttribute"/> properties with the <see cref="ParameterizedCommand{T}.CanExecute"/>. Even if the property
    /// is immutable (never changes) and the <see cref="ParameterizedCommand{T}"/> cannot be tracked automatically the method will always return
    /// <c>false</c> while the command is being executed. The <see cref="ParameterlessCommand"/> always returns <c>false</c> while the
    /// command is being executed.
    /// </remarks>
    public sealed class AsyncCommand<T> : ParameterizedCommand<T>
    {
        readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim();

        readonly Func<T, CommandExecutionController, CancellationToken, Task> execute;

        RunningCommandExecution? runningExecution;
        CompletedCommandExecution? completedExecution;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand(Func<T, Task> execute, Func<T, bool>? canExecute = null, CommandOptions? options = null)
            : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (arg, _, __) => execute(arg);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand(Func<T, CancellationToken, Task> execute, Func<T, bool>? canExecute = null, CommandOptions? options = null)
            : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (arg, _, cancellationToken) => execute(arg, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand(
            Func<T, CommandExecutionController, CancellationToken, Task> execute,
            Func<T, bool>? canExecute = null,
            CommandOptions? options = null) : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
            => this.execute = execute ?? throw new ArgumentNullException(nameof(execute));

#pragma warning disable 4014 // Because this call is not awaited, execution of the current method continues before  the call is completed. Consider applying the 'await' operator to the result of the call.
        private protected override void ExecuteCore(T arg) => Execute(arg);
#pragma warning restore 4014

        /// <inheritdoc />
        public override RunningCommandExecution? RunningExecution
        {
            get
            {
                gate.EnterReadLock();
                try
                {
                    return runningExecution;
                }
                finally
                {
                    gate.ExitReadLock();
                }
            }
        }

        /// <inheritdoc />
        public override CompletedCommandExecution? CompletedExecution
        {
            get
            {
                gate.EnterReadLock();
                try
                {
                    return completedExecution;
                }
                finally
                {
                    gate.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public async Task Execute(T arg)
        {
            RunningCommandExecution runningExecution;

            gate.EnterWriteLock();
            try
            {
                if (this.runningExecution != null)
                {
                    return;
                }

                runningExecution = CreateRunningExecution();
                this.runningExecution = runningExecution;
            }
            finally
            {
                gate.ExitWriteLock();
            }

            var completedExecution = null as CompletedCommandExecution;

            try
            {
                if (CanExecuteCore(arg))
                {
                    var controller = new CommandExecutionController(runningExecution);

                    var task = execute(arg, controller, runningExecution.CancellationToken)
                        ?? throw new InvalidOperationException($"The 'execute' callback returns null {nameof(Task)}.");

                    await task.ConfigureAwait(false);

                    completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Done);
                }
            }
            catch (OperationCanceledException)
            {
                completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Canceled);
            }
            catch (Exception e)
            {
                if (Options.TraceWhenFailed)
                {
                    EventSource.Log.CommandFailed(e.ToString());
                }

                completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Faulted, e);
            }
            finally
            {
                gate.EnterWriteLock();
                try
                {
                    if (completedExecution != null)
                    {
                        this.completedExecution = completedExecution;
                    }

                    this.runningExecution = null;
                }
                finally
                {
                    gate.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Starts the execution.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public void StartExecute(T arg) => ExecuteCore(arg);
    }
}