using System;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents a common functionality of commands.
    /// </summary>
    public abstract class CommandBase
    {
        internal CommandBase() {}

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void NotifyCanExecuteChanged() => OnCanExecuteChanged(EventArgs.Empty);

        void OnCanExecuteChanged([NotNull] EventArgs args) => CanExecuteChanged?.Invoke(this, args);

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;
    }
}