using System;

namespace Shuriken
{
    /// <summary>
    /// Defined the state of the completed execution.
    /// </summary>
    public enum CompletedCommandExecutionState
    {
        /// <summary>
        /// The execution has successfully completed.
        /// </summary>
        Done,

        /// <summary>
        /// The execution was canceled, i.e an <see cref="OperationCanceledException"/> was thrown during the execution.
        /// </summary>
        Canceled,

        /// <summary>
        /// The execution was faulted, i.e. an exception other than <see cref="OperationCanceledException"/> was thrown during the execution.
        /// </summary>
        Faulted,
    }
}