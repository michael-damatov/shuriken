using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken
{
    /// <summary>
    /// Represents an asynchronous parameterless command. The <see cref="ParameterlessCommand.CanExecute"/> method always returns <c>false</c> while
    /// the command is being executed.
    /// </summary>
    /// <remarks>
    /// Although the class is a thread-safe <see cref="ParameterlessCommand.CanExecute"/>, the <see cref="ParameterlessCommand"/> method is called in
    /// the same context as the parent <see cref="ObservableAttribute"/> (if the corresponding command property is annotated with the
    /// <see cref="ObservableObject"/>), i.e. only if the parent <see cref="ParameterlessCommand.CanExecute"/> is thread-safe the
    /// <see cref="ParameterlessCommand"/> method will be invoked in a background thread.<para />
    /// ALWAYS annotate <see cref="ObservableAttribute"/> properties with the <see cref="ParameterlessCommand.CanExecute"/>. Even if the property is
    /// immutable (never changes) the <see cref="ParameterlessCommand"/> can change. The <see cref="ParameterlessCommand"/> always returns
    /// <c>false</c> while the command is being executed.
    /// </remarks>
    public sealed class AsyncCommand : ParameterlessCommand
    {
        [NotNull]
        readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim();

        [NotNull]
        readonly Func<CommandExecutionController, CancellationToken, Task> execute;

        RunningCommandExecution runningExecution;
        CompletedCommandExecution completedExecution;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand([NotNull] Func<Task> execute, Func<bool> canExecute = null, CommandOptions options = null)
            : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (_, __) => execute();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand([NotNull] Func<CancellationToken, Task> execute, Func<bool> canExecute = null, CommandOptions options = null)
            : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (_, cancellationToken) => execute(cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public AsyncCommand(
            [NotNull] Func<CommandExecutionController, CancellationToken, Task> execute,
            Func<bool> canExecute = null,
            CommandOptions options = null) : base(true, canExecute, options ?? CommandOptions.DefaultAsync)
            => this.execute = execute ?? throw new ArgumentNullException(nameof(execute));

#pragma warning disable 4014 // Because this call is not awaited, execution of the current method continues before  the call is completed. Consider applying the 'await' operator to the result of the call.
        internal override void ExecuteCore() => Execute();
#pragma warning restore 4014

        /// <inheritdoc />
        public override RunningCommandExecution RunningExecution
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
        public override CompletedCommandExecution CompletedExecution
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
        public async Task Execute()
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
                if (CanExecuteCore())
                {
                    var controller = new CommandExecutionController(runningExecution);

                    var task = execute(controller, runningExecution.CancellationToken)
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
        public void StartExecute() => ExecuteCore();
    }
}