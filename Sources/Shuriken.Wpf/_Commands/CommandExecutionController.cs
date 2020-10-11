using System;

namespace Shuriken
{
    /// <summary>
    /// Represents the object that enables progress reporting, controlling the state of the <see cref="RunningCommandExecution.CancelCommand"/>, and
    /// also allows access to the <see cref="RunningCommandExecution"/> while the command is being executed.
    /// </summary>
    public sealed partial class CommandExecutionController
    {
        internal CommandExecutionController(RunningCommandExecution execution) => Execution = execution;

        /// <summary>
        /// Gets the execution.
        /// </summary>
        public RunningCommandExecution Execution { get; }

        /// <summary>
        /// Reports the current progress.
        /// </summary>
        /// <param name="value">The value ranging between 0 and 1 (inclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 0 or greater than 1.</exception>
        /// <exception cref="InvalidOperationException">The execution has been completed.</exception>
        public void ReportProgress(float value)
        {
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Execution.Progress = value;
        }

        /// <summary>
        /// Disables the <see cref="RunningCommandExecution.CancelCommand"/> of the <see cref="RunningCommandExecution"/> object.
        /// </summary>
        public void DisableCancelCommand() => Execution.IsCancelCommandEnabled = false;
    }
}