using System;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a parameterized command.
    /// </summary>
    /// <typeparam name="T">The command parameter type.</typeparam>
    public sealed partial class Command<T> : CommandBase
    {
        [NotNull]
        readonly Action<T> execute;

        readonly Func<T, bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command{T}" /> class.
        /// </summary>
        /// <param name="execute">The execute method.</param>
        /// <param name="canExecute">The function to test whether the <paramref name="execute"/> method may be invoked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
        public Command([NotNull] Action<T> execute, Func<T, bool> canExecute = null)
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
        /// <param name="arg">The argument.</param>
        /// <returns><c>true</c> if this instance can execute; otherwise, <c>false</c>.</returns>
        [Pure]
        public bool CanExecute(T arg) => canExecute == null || canExecute(arg);

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public void Execute(T arg)
        {
            if (CanExecute(arg))
            {
                execute(arg);
            }
        }
    }
}