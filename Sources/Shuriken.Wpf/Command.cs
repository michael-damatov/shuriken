using System;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a parameterless command.
    /// </summary>
    public sealed partial class Command : CommandBase
    {
        [NotNull]
        readonly Action execute;

        readonly Func<bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        /// <param name="execute">The execute method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute"/> method may be invoked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
        public Command([NotNull] Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether this command can execute.
        /// </summary>
        /// <returns><c>true</c> if this instance can execute; otherwise, <c>false</c>.</returns>
        [Pure]
        public bool CanExecute() => canExecute == null || canExecute();

        /// <summary>
        /// Executes this command.
        /// </summary>
        public void Execute()
        {
            if (CanExecute())
            {
                execute();
            }
        }
    }
}