using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace Shuriken
{
    partial class CommandBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [NotNull]
        [ItemNotNull]
        readonly HashSet<EventHandler> subscribers = new HashSet<EventHandler>();

        [NotNull]
        [ItemNotNull]
        [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Local",
            Justification = "The collection type should be used to improve performance and reduce memory load for iterations.")]
        List<EventHandler> Subscribers
        {
            get
            {
                lock (subscribers)
                {
                    return subscribers.ToList();
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void NotifyCanExecuteChanged() => OnCanExecuteChanged(EventArgs.Empty);

        void OnCanExecuteChanged([NotNull] EventArgs args)
        {
            foreach (var subscriber in Subscribers)
            {
                subscriber(this, args);
            }
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (value != null)
                {
                    lock (subscribers)
                    {
                        subscribers.Add(value);
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (subscribers)
                    {
                        subscribers.Remove(value);
                    }
                }
            }
        }
    }
}