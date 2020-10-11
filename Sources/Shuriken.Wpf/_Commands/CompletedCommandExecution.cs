using System;
using System.Diagnostics;

namespace Shuriken
{
    /// <summary>
    /// Represents the last completed execution.
    /// </summary>
    public sealed class CompletedCommandExecution
    {
        internal CompletedCommandExecution(CompletedCommandExecutionState state, float progress, Exception? exception)
        {
            Debug.Assert(
                state == CompletedCommandExecutionState.Done && Math.Abs(progress - 1f) < float.Epsilon && exception == null ||
                state == CompletedCommandExecutionState.Canceled && progress >= 0f && progress <= 1f && exception == null ||
                state == CompletedCommandExecutionState.Faulted &&
                progress >= 0f &&
                progress <= 1f &&
                exception != null &&
                !(exception is OperationCanceledException));

            State = state;
            Progress = progress;
            Exception = exception;
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public CompletedCommandExecutionState State { get; }

        /// <summary>
        /// Gets the progress.
        /// </summary>
        /// <remarks>
        /// If the <see cref="State"/> is <see cref="CompletedCommandExecutionState.Done"/> the value is always 1; otherwise, it's in the range 0 to 1
        /// (both inclusive).
        /// </remarks>
        public float Progress { get; }

        /// <summary>
        /// Gets the exception if the <see cref="State"/> is <see cref="CompletedCommandExecutionState.Faulted"/>.
        /// </summary>
        /// <remarks>
        /// The property can never be an <see cref="OperationCanceledException"/>.
        /// </remarks>
        public Exception? Exception { get; }
    }
}