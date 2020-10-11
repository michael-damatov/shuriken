using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Shuriken;
using Shuriken.Monitoring;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        const int total = 200;

        IDisposable? monitoringSuspension;

        public MainViewModel()
        {
            var options = new CommandOptions(true, false);

            Command = new Command(DoWork, options: options);
            AsyncCommand = new AsyncCommand(DoWorkAsync, options: options);

            CommandParameterized = new Command<string>((_, controller, cancellationToken) => DoWork(controller, cancellationToken), options: options);
            AsyncCommandParameterized = new AsyncCommand<string>(
                (_, controller, cancellationToken) => DoWorkAsync(controller, cancellationToken),
                options: options);
        }

        static void DoWork(CommandExecutionController controller, CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    controller.ReportProgress(i / 100f);

                    DoEvents();
                    Thread.Sleep(TimeSpan.FromSeconds(0.1));
                }

                controller.DisableCancelCommand();
            }
            finally
            {
                DoEvents();
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        static void DoEvents() => Application.Current!.Dispatcher!.Invoke(DispatcherPriority.Background, new Action(delegate { }));

        static async Task DoWorkAsync(CommandExecutionController controller, CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    controller.ReportProgress(i / 100f);

                    await Task.Delay(TimeSpan.FromSeconds(0.1), cancellationToken);
                }

                controller.DisableCancelCommand();
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        public string Title
        {
            get
            {
#if NETCOREAPP
                return "Shuriken Demo (.NET Core)";
#else
                return "Shuriken Demo (.NET Framework)";
#endif
            }
        }

        public SampleViewModelContainerRegular ContainerRegular { get; } = new SampleViewModelContainerRegular(total);

        public SampleViewModelContainer Container { get; } = new SampleViewModelContainer(total);

        [Observable]
        public Command Command { get; }

        [Observable]
        public AsyncCommand AsyncCommand { get; }

        [Observable]
        public Command<string> CommandParameterized { get; }

        [Observable]
        public AsyncCommand<string> AsyncCommandParameterized { get; }

        public bool IsMonitoringSuspended
        {
            get => monitoringSuspension != null;
            set
            {
                if (value)
                {
                    if (monitoringSuspension == null)
                    {
                        monitoringSuspension = ApplicationMonitorScope.Current!.Suspend();

                        NotifyPropertyChange();
                    }
                }
                else
                {
                    if (monitoringSuspension != null)
                    {
                        monitoringSuspension.Dispose();
                        monitoringSuspension = null;

                        NotifyPropertyChange();
                    }
                }
            }
        }
    }
}