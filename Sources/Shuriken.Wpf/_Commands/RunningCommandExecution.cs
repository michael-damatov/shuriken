using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Represents the running command execution.
    /// </summary>
    public sealed class RunningCommandExecution : ObservableObject
    {
        readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();

        readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim();

        /// <remarks>
        /// 0 -> false, 1 -> true
        /// </remarks>
        [ValueRange(0, 1)]
        int isCancelCommandEnabled;

        bool isCompleted;

        float progress;

        internal RunningCommandExecution(bool isCancelCommandEnabled) : base(true)
        {
            IsCancelCommandEnabled = isCancelCommandEnabled;

            CancelCommand = new Command(
                () => cancellationSource.Cancel(),
                () => IsCancelCommandEnabled && !cancellationSource.IsCancellationRequested);
        }

        internal bool IsCancelCommandEnabled
        {
            get => Interlocked.CompareExchange(ref isCancelCommandEnabled, 0, 0) == 1;
            set => Interlocked.Exchange(ref isCancelCommandEnabled, value ? 1 : 0);
        }

        internal CancellationToken CancellationToken => cancellationSource.Token;

        internal CompletedCommandExecution Complete(CompletedCommandExecutionState state, Exception? exception = null)
        {
            float progress;

            gate.EnterWriteLock();
            try
            {
                if (state == CompletedCommandExecutionState.Done)
                {
                    this.progress = 1f;
                }

                progress = this.progress;

                isCompleted = true;
            }
            finally
            {
                gate.ExitWriteLock();
            }

            IsCancelCommandEnabled = false;

            return new CompletedCommandExecution(state, progress, exception);
        }

        /// <summary>
        /// Gets the progress.
        /// </summary>
        /// <remarks>
        /// The value is always in the range 0 to 1 (both inclusive).
        /// </remarks>
        [Observable]
        public float Progress
        {
            get
            {
                gate.EnterReadLock();
                try
                {
                    Debug.Assert(progress >= 0f && progress <= 1f);

                    return progress;
                }
                finally
                {
                    gate.ExitReadLock();
                }
            }
            internal set
            {
                Debug.Assert(value >= 0f && value <= 1f);

                gate.EnterWriteLock();
                try
                {
                    if (isCompleted)
                    {
                        throw new InvalidOperationException("The execution has been completed.");
                    }

                    progress = value;
                }
                finally
                {
                    gate.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets the "cancel" command.
        /// </summary>
        /// <remarks>
        /// Use the command constructor to specify whether the command is initially enabled.<para />
        /// The command becomes disabled when
        /// <list type="bullet">
        ///     <item>the execution completes</item>
        ///     <item>the command is triggered</item>
        ///     <item>
        ///         the <see cref="Shuriken.CommandExecutionController.DisableCancelCommand"/> method of the <see cref="Shuriken.CommandExecutionController"/> class is
        ///         invoked
        ///     </item>
        /// </list>
        /// </remarks>
        [Observable]
        public Command CancelCommand { get; }
    }
}