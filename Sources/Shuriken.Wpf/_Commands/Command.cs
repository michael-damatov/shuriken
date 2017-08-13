using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken
{
    /// <summary>
    /// Represents a parameterless command. The <see cref="ParameterlessCommand.CanExecute"/> method always returns <c>false</c> while the command is
    /// being executed.
    /// </summary>
    /// <remarks>
    /// CONSIDER annotating <see cref="Command"/> properties with the <see cref="ParameterlessCommand.CanExecute"/>. Even if the property is immutable
    /// (never changes) the <see cref="ParameterlessCommand"/> can change. However, if the <see cref="ParameterlessCommand"/> never changes (e.g. only
    /// <c>false</c> while the command is being executed) the property should not be annotated with the <see cref="ObservableAttribute"/>.
    /// </remarks>
    public sealed class Command : ParameterlessCommand
    {
        [NotNull]
        readonly Action<CommandExecutionController, CancellationToken> execute;

        RunningCommandExecution runningExecution;
        CompletedCommandExecution completedExecution;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command([NotNull] Action execute, Func<bool> canExecute = null, CommandOptions options = null)
            : base(false, canExecute, options ?? CommandOptions.Default)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (_, __) => execute();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command([NotNull] Action<CancellationToken> execute, Func<bool> canExecute = null, CommandOptions options = null)
            : base(false, canExecute, options ?? CommandOptions.Default)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = (_, cancellationToken) => execute(cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        /// <param name="execute">The execution method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute" /> method may be invoked.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute" /> is <c>null</c>.</exception>
        public Command(
            [NotNull] Action<CommandExecutionController, CancellationToken> execute,
            Func<bool> canExecute = null,
            CommandOptions options = null) : base(false, canExecute, options ?? CommandOptions.Default)
            => this.execute = execute ?? throw new ArgumentNullException(nameof(execute));

        internal override void ExecuteCore() => Execute();

        /// <inheritdoc />
        public override RunningCommandExecution RunningExecution => runningExecution;

        /// <inheritdoc />
        public override CompletedCommandExecution CompletedExecution => completedExecution;

        /// <summary>
        /// Executes this command.
        /// </summary>
        public void Execute()
        {
            if (runningExecution != null)
            {
                return;
            }

            runningExecution = CreateRunningExecution();

            try
            {
                if (CanExecuteCore())
                {
                    Debug.Assert(runningExecution != null);
                    var controller = new CommandExecutionController(runningExecution);

                    execute(controller, runningExecution.CancellationToken);

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