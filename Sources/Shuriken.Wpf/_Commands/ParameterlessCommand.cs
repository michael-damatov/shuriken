using System;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a common functionality of parameterless commands.
    /// </summary>
    public abstract partial class ParameterlessCommand : CommandBase
    {
        readonly Func<bool> canExecute;

        internal ParameterlessCommand(bool isThreadSafe, Func<bool> canExecute, [NotNull] CommandOptions options) : base(isThreadSafe, options)
            => this.canExecute = canExecute;

        internal bool CanExecuteCore() => canExecute == null || canExecute();

        internal abstract void ExecuteCore();

        /// <summary>
        /// Determines whether this command can execute.
        /// </summary>
        /// <returns><c>true</c> if this command can execute; otherwise, <c>false</c>.</returns>
        [Pure]
        public bool CanExecute() => RunningExecution == null && CanExecuteCore();
    }
}