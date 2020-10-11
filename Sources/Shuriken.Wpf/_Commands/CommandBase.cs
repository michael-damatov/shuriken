namespace Shuriken
{
    /// <summary>
    /// Represents a common functionality of commands.
    /// </summary>
    public abstract partial class CommandBase : ObservableObject
    {
        private protected CommandBase(bool isThreadSafe, CommandOptions options) : base(isThreadSafe) => Options = options;

        private protected RunningCommandExecution CreateRunningExecution() => new RunningCommandExecution(Options.IsCancelCommandEnabled);

        /// <summary>
        /// Gets the options.
        /// </summary>
        public CommandOptions Options { get; }

        /// <summary>
        /// Gets the running execution if available.
        /// </summary>
        [Observable]
        public abstract RunningCommandExecution? RunningExecution { get; }

        /// <summary>
        /// Gets the last completed execution if available.
        /// </summary>
        [Observable]
        public abstract CompletedCommandExecution? CompletedExecution { get; }
    }
}