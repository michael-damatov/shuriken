using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    partial class ApplicationMonitorScope
    {
        IDisposable sessionSuspension;

        /// <exception cref="InvalidOperationException">Maximum suspension depth has been exceeded.</exception>
        [SecuritySafeCritical]
        void SystemEventsOnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            Debug.Assert(e != null);

            switch (e.Reason)
            {
                case SessionSwitchReason.ConsoleConnect:
                case SessionSwitchReason.RemoteConnect:
                case SessionSwitchReason.SessionUnlock:
                    lock (countEventForSuspensions)
                    {
                        if (sessionSuspension != null)
                        {
                            sessionSuspension.Dispose();
                            sessionSuspension = null;
                        }
                    }
                    break;

                case SessionSwitchReason.ConsoleDisconnect:
                case SessionSwitchReason.RemoteDisconnect:
                case SessionSwitchReason.SessionLock:
                    lock (countEventForSuspensions)
                    {
                        if (sessionSuspension == null)
                        {
                            sessionSuspension = Suspend();
                        }
                    }
                    break;
            }
        }

        [SecuritySafeCritical]
        void RegisterForSessionSwitchNotifications()
        {
            try
            {
                SystemEvents.SessionSwitch += SystemEventsOnSessionSwitch;
            }
            catch (SystemException e) when (e is InvalidOperationException || e is ExternalException)
            {
                EventSource.Log.FailedAttachingSystemEvent(nameof(SystemEvents.SessionSwitch), e.ToString());
            }
        }

        [SecuritySafeCritical]
        void UnregisterFromSessionSwitchNotifications()
        {
            try
            {
                SystemEvents.SessionSwitch -= SystemEventsOnSessionSwitch;
            }
            finally
            {
                lock (countEventForSuspensions)
                {
                    sessionSuspension = null;
                }
            }
        }
    }
}