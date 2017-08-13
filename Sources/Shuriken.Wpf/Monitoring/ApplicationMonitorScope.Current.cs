using System.Threading;

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope
    {
        static ApplicationMonitorScope current;

        /// <summary>
        /// Gets the current <see cref="Shuriken.Monitoring.ApplicationMonitorScope"/>.
        /// </summary>
        public static ApplicationMonitorScope Current => Interlocked.CompareExchange(ref current, null, null);
    }
}