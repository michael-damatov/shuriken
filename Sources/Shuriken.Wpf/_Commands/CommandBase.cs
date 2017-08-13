using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a common functionality of commands.
    /// </summary>
    public abstract partial class CommandBase : ObservableObject
    {
        internal CommandBase(bool isThreadSafe, [NotNull] CommandOptions options) : base(isThreadSafe) => Options = options;

        [NotNull]
        internal RunningCommandExecution CreateRunningExecution() => new RunningCommandExecution(Options.IsCancelCommandEnabled);

        /// <summary>
        /// Gets the options.
        /// </summary>
        [NotNull]
        public CommandOptions Options { get; }

        /// <summary>
        /// Gets the running execution if available.
        /// </summary>
        [Observable]
        public abstract RunningCommandExecution RunningExecution { get; }

        /// <summary>
        /// Gets the last completed execution if available.
        /// </summary>
        [Observable]
        public abstract CompletedCommandExecution CompletedExecution { get; }
    }
}