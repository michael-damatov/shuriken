using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;

namespace Shuriken.Diagnostics
{
    [EventSource(Name = @"Shuriken")]
    internal sealed class EventSource : System.Diagnostics.Tracing.EventSource
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "The class must be public due to an ETW restriction.")]
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass", Justification = "Members are accessed from the outer class only.")]
        public static class Keywords
        {
            public const EventKeywords Log = (EventKeywords)0x1;
            public const EventKeywords Performance = (EventKeywords)0x2;
        }

        [NotNull]
        public static readonly EventSource Log = new EventSource();

        EventSource() { }

        /// <summary>
        /// Monitor has been started.
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            1,
            Version = 0,
            Message = "Monitor has been started.",
            Level = EventLevel.Informational,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Start,
            Channel = EventChannel.Operational
        )]
        public void MonitorStart() => WriteEvent(1);

        /// <summary>
        /// Monitor has been stopped.
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            2,
            Version = 0,
            Message = "Monitor has been stopped.",
            Level = EventLevel.Informational,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Stop,
            Channel = EventChannel.Operational
        )]
        public void MonitorStop() => WriteEvent(2);

        /// <summary>
        /// Monitor has been suspended.
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            3,
            Version = 0,
            Message = "Monitor has been suspended.",
            Level = EventLevel.Informational,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Suspend,
            Channel = EventChannel.Operational
        )]
        public void MonitorSuspend() => WriteEvent(3);

        /// <summary>
        /// Monitor has been resumed.
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            4,
            Version = 0,
            Message = "Monitor has been resumed.",
            Level = EventLevel.Informational,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Resume,
            Channel = EventChannel.Operational
        )]
        public void MonitorResume() => WriteEvent(4);

        /// <summary>
        /// Stopping the monitoring because of an exception while updating the values: {0}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            5,
            Version = 0,
            Message = "Stopping the monitoring because of an exception while updating the values: {0}",
            Level = EventLevel.Error,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void StoppingDueToFailedUpdate(string exception) => WriteEvent(5, exception);

        /// <summary>
        /// Stopping the monitoring because of an exception while sending the change notifications: {0}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            6,
            Version = 0,
            Message = "Stopping the monitoring because of an exception while sending the change notifications: {0}",
            Level = EventLevel.Error,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void StoppingDueToFailedChangeNotifications(string exception) => WriteEvent(6, exception);

        /// <summary>
        /// Failed attaching the system event '{0}': {1}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            7,
            Version = 0,
            Message = "Failed attaching the system event '{0}': {1}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void FailedAttachingSystemEvent(string systemEvent, string exception) => WriteEvent(7, systemEvent, exception);

        /// <summary>
        /// The {0} event handler is assigned, but the {1} is not available.
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            8,
            Version = 0,
            Message = "The {0} event handler is assigned, but the {1} is not available.",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void MissingMonitoringScope(string eventName, string scope) => WriteEvent(8, eventName, scope);

        /// <summary>
        /// Cannot initially get the value of the '{1}' property of the '{0}' object: {2}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            9,
            Version = 0,
            Message = "Cannot initially get the value of the '{1}' property of the '{0}' object: {2}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableInitiallyToReadProperty(string type, string property, string exception) => WriteEvent(9, type, property, exception);

        /// <summary>
        /// Cannot get the value of the '{1}' property of the '{0}' object: {2}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            10,
            Version = 0,
            Message = "Cannot get the value of the '{1}' property of the '{0}' object: {2}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableSubsequentlyToReadProperty(string type, string property, string exception) => WriteEvent(10, type, property, exception);

        /// <summary>
        /// Cannot initially invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            11,
            Version = 0,
            Message = "Cannot initially invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableInitiallyToInvokeCommandMethod(string type, string property, string method, string exception)
            => WriteEvent(11, type, property, method, exception);

        /// <summary>
        /// Cannot invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            12,
            Version = 0,
            Message = "Cannot invoke the '{2}' method of the '{1}' property of the '{0}' object: {3}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableSubsequentlyToInvokeCommandMethod(string type, string property, string method, string exception)
            => WriteEvent(12, type, property, method, exception);

        /// <summary>
        /// Cannot analyze the value of the '{1}' property of the '{0}' object: {2}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            13,
            Version = 0,
            Message = "Cannot analyze the value of the '{1}' property of the '{0}' object: {2}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableToAnalyzeProperty(string type, string property, string exception) => WriteEvent(13, type, property, exception);

        /// <summary>
        /// Cannot raise the change notification for the '{1}' property of the '{0}' object: {2}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            14,
            Version = 0,
            Message = "Cannot raise the change notification for the '{1}' property of the '{0}' object: {2}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableToRaisePropertyChangeNotification(string type, string property, string exception)
            => WriteEvent(14, type, property, exception);

        /// <summary>
        /// Cannot raise the change notification for the '{1}' command property of the '{0}' object: {2}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            15,
            Version = 0,
            Message = "Cannot raise the change notification for the '{1}' command property of the '{0}' object: {2}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
        )]
        public void UnableToRaiseCommandPropertyChangeNotification(string type, string property, string exception)
            => WriteEvent(15, type, property, exception);

        /// <summary>
        /// Command execution failed: {0}
        /// </summary>
        [Conditional("TRACE")]
        [Event(
            16,
            Version = 0,
            Message = "Command execution failed: {0}",
            Level = EventLevel.Warning,
            Keywords = Keywords.Log,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Operational
            )]
        public void CommandFailed(string exception) => WriteEvent(16, exception);

        /// <remarks>Number of monitored properties. Lower value is better. A high value can indicate an unnecessary observation or memory leaks (e.g. due to missing UI virtualization).</remarks>
        [Conditional("TRACE")]
        [Event(
            19,
            Version = 0,
            Level = EventLevel.Informational,
            Keywords = Keywords.Performance,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Analytic
        )]
        public void PerformanceMonitoredProperties(int count) => WriteEvent(19, count);

        /// <remarks>Complete cycle time [ms]. Lower value is better. For smooth performance it should not exceed 15ms.</remarks>
        [Conditional("TRACE")]
        [Event(
            20,
            Version = 0,
            Level = EventLevel.Informational,
            Keywords = Keywords.Performance,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Analytic
        )]
        public void PerformanceCycleTime(long elapsedMilliseconds) => WriteEvent(20, elapsedMilliseconds);

        /// <param name="capacityThreadAffine">Total number of slots (thread-affine).</param>
        /// <param name="countThreadAffine">Number of used slots (thread-affine).</param>
        /// <param name="capacityThreadSafe">Total number of slots (thread-safe).</param>
        /// <param name="countThreadSafe">Number of used slots (thread-safe).</param>
        [Conditional("TRACE")]
        [Event(
            21,
            Version = 0,
            Level = EventLevel.Informational,
            Keywords = Keywords.Performance,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Analytic
        )]
        public void PerformanceLists(int capacityThreadAffine, int countThreadAffine, int capacityThreadSafe, int countThreadSafe)
            => WriteEvent(21, capacityThreadAffine, countThreadAffine, capacityThreadSafe, countThreadSafe);

        /// <param name="capacityThreadAffine">Total number of slots (thread-affine with changed properties).</param>
        /// <param name="countThreadAffine">Number of used slots (thread-affine with changed properties).</param>
        /// <param name="capacityThreadSafe">Total number of slots (thread-safe with changed properties).</param>
        /// <param name="countThreadSafe">Number of used slots (thread-safe with changed properties).</param>
        [Conditional("TRACE")]
        [Event(
            22,
            Version = 0,
            Level = EventLevel.Informational,
            Keywords = Keywords.Performance,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Analytic
        )]
        public void PerformanceListsWithChangedProperties(int capacityThreadAffine, int countThreadAffine, int capacityThreadSafe, int countThreadSafe)
            => WriteEvent(22, capacityThreadAffine, countThreadAffine, capacityThreadSafe, countThreadSafe);

        /// <param name="capacityThreadAffine">Total number of slots (thread-affine with items to be removed).</param>
        /// <param name="countThreadAffine">Number of used slots (thread-affine with items to be removed).</param>
        /// <param name="capacityThreadSafe">Total number of slots (thread-safe with items to be removed).</param>
        /// <param name="countThreadSafe">Number of used slots (thread-safe with items to be removed).</param>
        [Conditional("TRACE")]
        [Event(
            23,
            Version = 0,
            Level = EventLevel.Informational,
            Keywords = Keywords.Performance,
            Opcode = EventOpcode.Info,
            Channel = EventChannel.Analytic
        )]
        public void PerformanceListsWithItemsToBeRemoved(int capacityThreadAffine, int countThreadAffine, int capacityThreadSafe, int countThreadSafe)
            => WriteEvent(23, capacityThreadAffine, countThreadAffine, capacityThreadSafe, countThreadSafe);
    }
}