namespace Shuriken
{
    /// <summary>
    /// Represents command options.
    /// </summary>
    public sealed class CommandOptions
    {
        internal static CommandOptions Default { get; } = new CommandOptions(false);

        internal static CommandOptions DefaultAsync { get; } = new CommandOptions(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandOptions"/> class.
        /// </summary>
        /// <param name="isCancelCommandEnabled">
        ///     if set to <c>true</c> the <see cref="RunningCommandExecution.CancelCommand"/> becomes enabled each time when the execution starts.
        /// </param>
        /// <param name="traceWhenFailed">
        ///     if set to <c>true</c> the <see cref="CompletedCommandExecution.Exception"/> is traced each time when the execution fails; set it to
        ///     <c>false</c> when command failing is the fully expected behavior, and thus the <see cref="CompletedCommandExecution.Exception"/>
        ///     should never be traced.
        /// </param>
        public CommandOptions(bool isCancelCommandEnabled, bool traceWhenFailed = true)
        {
            IsCancelCommandEnabled = isCancelCommandEnabled;
            TraceWhenFailed = traceWhenFailed;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="RunningCommandExecution.CancelCommand"/> becomes enabled each time when the execution
        /// starts.
        /// </summary>
        public bool IsCancelCommandEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="CompletedCommandExecution.Exception"/> is traced each time when the execution fails. The
        /// property is <c>true</c> when command failing is the fully expected behavior, and thus the
        /// <see cref="CompletedCommandExecution.Exception"/> should never be traced.
        /// </summary>
        public bool TraceWhenFailed { get; }
    }
}