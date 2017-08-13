using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken
{
    /// <summary>
    /// Represents a parameterized command. The <see cref="ParameterizedCommand{T}.CanExecute"/> method always returns <c>false</c> while the command
    /// is being executed.
    /// </summary>
    /// <typeparam name="T">The command parameter type.</typeparam>
    /// <remarks>
    /// CONSIDER not annotating <see cref="Command{T}"/> properties with the <see cref="ParameterizedCommand{T}.CanExecute"/>. Changes of
    /// <see cref="ParameterizedCommand{T}"/> cannot be tracked automatically. Use the <see cref="CommandBase"/>
    /// method to send notifications. Only if the property is not immutable it should be annotated with the <see cref="ObservableAttribute"/>.
    /// </remarks>
    public sealed class Command<T> : ParameterizedCommand<T>
    {
        [NotNull]
        readonly Action<T, CommandExecutionController, CancellationToken> execute;

        RunningCommandExecution runningExecution;
        CompletedCommandExecution completedExecution;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command([NotNull] Action<T> execute, Func<T, bool> canExecute = null, CommandOptions options = null)
            : base(false, canExecute, options ?? CommandOptions.Default)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (arg, _, __) => execute(arg);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command([NotNull] Action<T, CancellationToken> execute, Func<T, bool> canExecute = null, CommandOptions options = null)
            : base(false, canExecute, options ?? CommandOptions.Default)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (arg, _, cancellationToken) => execute(arg, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command{T}" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command(
            [NotNull] Action<T, CommandExecutionController, CancellationToken> execute,
            Func<T, bool> canExecute = null,
            CommandOptions options = null) : base(false, canExecute, options ?? CommandOptions.Default)
            => this.execute = execute ?? throw new ArgumentNullException(nameof(execute));

        internal override void ExecuteCore(T arg) => Execute(arg);

        /// <inheritdoc />
        public override RunningCommandExecution RunningExecution => runningExecution;

        /// <inheritdoc />
        public override CompletedCommandExecution CompletedExecution => completedExecution;

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public void Execute(T arg)
        {
            if (runningExecution != null)
            {
                return;
            }

            runningExecution = CreateRunningExecution();

            try
            {
                if (CanExecuteCore(arg))
                {
                    Debug.Assert(runningExecution != null);
                    var controller = new CommandExecutionController(runningExecution);

                    execute(arg, controller, runningExecution.CancellationToken);

                    Debug.Assert(runningExecution != null);
                    completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Done);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Assert(runningExecution != null);
                completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Canceled);
            }
            catch (Exception e)
            {
                if (Options.TraceWhenFailed)
                {
                    EventSource.Log.CommandFailed(e.ToString());
                }

                Debug.Assert(runningExecution != null);
                completedExecution = runningExecution.Complete(CompletedCommandExecutionState.Faulted, e);
            }
            finally
            {
                runningExecution = null;
            }
        }
    }
}