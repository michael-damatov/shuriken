using System.Threading;

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope
    {
        static Shuriken.Monitoring.ApplicationMonitorScope current;

        /// <summary>
        /// Gets the current <see cref="Shuriken.Monitoring.ApplicationMonitorScope"/>.
        /// </summary>
        public static Shuriken.Monitoring.ApplicationMonitorScope Current => Interlocked.CompareExchange(ref current, null, null);
    }
}