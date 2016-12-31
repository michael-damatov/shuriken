using System;
using Shuriken.Monitoring;

namespace Demo.Shuriken.Wpf
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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