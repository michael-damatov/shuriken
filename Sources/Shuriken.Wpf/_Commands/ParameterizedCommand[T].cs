using System;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a common functionality of parameterized commands.
    /// </summary>
    /// <typeparam name="T">The command parameter type.</typeparam>
    public abstract partial class ParameterizedCommand<T> : CommandBase
    {
        readonly Func<T, bool> canExecute;

        internal ParameterizedCommand(bool isThreadSafe, Func<T, bool> canExecute, [NotNull] CommandOptions options) : base(isThreadSafe, options)
            => this.canExecute = canExecute;

        internal bool CanExecuteCore(T arg) => canExecute == null || canExecute(arg);

        internal abstract void ExecuteCore(T arg);

        /// <summary>
        /// Determines whether this command can execute.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns><c>true</c> if this command can execute; otherwise, <c>false</c>.</returns>
        [Pure]
        public bool CanExecute(T arg) => RunningExecution == null && CanExecuteCore(arg);
    }
}