using System;
using System.Diagnostics;
using Shuriken.Diagnostics;
using Shuriken.Monitoring;

namespace Demo.Shuriken.Wpf
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
#if DEBUG
            EventListener.OperationalEvent += (_, e) => Debug.WriteLine(e.ToDebugMessage());
#endif

            var app = new App();
            app.InitializeComponent();

            var applicationMonitorScope = new ApplicationMonitorScope(new WpfNotificationContext(app.Dispatcher));
            try
            {
                app.Run();
            }
            finally
            {
                applicationMonitorScope.Dispose().GetAwaiter().GetResult();
            }
        }
    }
}