using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using JetBrains.Annotations;

namespace Shuriken.Diagnostics
{
    /// <summary>
    /// Allows capturing of the library events.
    /// </summary>
    public static class EventListener
    {
        sealed class Listener : System.Diagnostics.Tracing.EventListener
        {
            readonly Dictionary<EventChannel, List<EventHandler<EventWrittenEventArgs>>> channelSubscribers;

            public Listener(Dictionary<EventChannel, List<EventHandler<EventWrittenEventArgs>>> channelSubscribers)
            {
                this.channelSubscribers = channelSubscribers;

                EnableEvents(EventSource.Log, EventLevel.LogAlways);
            }

            public override void Dispose()
            {
                try
                {
                    DisableEvents(EventSource.Log);
                }
                finally
                {
                    base.Dispose();
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                lock (channelSubscribers)
                {
                    if (channelSubscribers.TryGetValue(eventData.Channel, out var subscribers))
                    {
                        foreach (var eventHandler in subscribers)
                        {
                            try
                            {
                                eventHandler(null, eventData);
                            }
                            catch
                            {
                                // all exceptions thrown by event handlers are intentionally suppressed
                            }
                        }
                    }
                }
            }
        }

        static readonly Dictionary<EventChannel, List<EventHandler<EventWrittenEventArgs>>> channelSubscribers =
            new Dictionary<EventChannel, List<EventHandler<EventWrittenEventArgs>>>();

        static Listener? listener;

        static void AddEventHandler(EventChannel channel, EventHandler<EventWrittenEventArgs>? value)
        {
            if (value != null)
            {
                lock (channelSubscribers)
                {
                    if (!channelSubscribers.TryGetValue(channel, out var subscribers))
                    {
                        subscribers = new List<EventHandler<EventWrittenEventArgs>>();
                        channelSubscribers.Add(channel, subscribers);
                    }

                    subscribers.Add(value);

                    if (channelSubscribers.Values.Sum(s => s.Count) == 1)
                    {
                        listener = new Listener(channelSubscribers);
                    }
                }
            }
        }

        static void RemoveEventHandler(EventChannel channel, EventHandler<EventWrittenEventArgs>? value)
        {
            if (value != null)
            {
                lock (channelSubscribers)
                {
                    if (channelSubscribers.TryGetValue(channel, out var subscribers))
                    {
                        if (subscribers.Remove(value))
                        {
                            if (subscribers.Count == 0)
                            {
                                channelSubscribers.Remove(channel);
                            }

                            if (channelSubscribers.Values.Sum(s => s.Count) == 0)
                            {
                                listener?.Dispose();
                                listener = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when operational events are raised.
        /// </summary>
        public static event EventHandler<EventWrittenEventArgs>? OperationalEvent
        {
            add => AddEventHandler(EventChannel.Operational, value);
            remove => RemoveEventHandler(EventChannel.Operational, value);
        }

        /// <summary>
        /// Occurs when analytic events are raised.
        /// </summary>
        public static event EventHandler<EventWrittenEventArgs>? AnalyticEvent
        {
            add => AddEventHandler(EventChannel.Analytic, value);
            remove => RemoveEventHandler(EventChannel.Analytic, value);
        }

        /// <summary>
        /// Create the event message suitable for the debugging.
        /// </summary>
        /// <param name="eventArgs">The <see cref="EventWrittenEventArgs" /> object containing the event data.</param>
        /// <returns>The event message.</returns>
        [Pure]
        public static string ToDebugMessage(this EventWrittenEventArgs? eventArgs)
        {
            if (eventArgs == null)
            {
                return "";
            }

            string? message;
            if (eventArgs.Payload != null)
            {
                try
                {
                    message = string.Format(eventArgs.Message ?? "", eventArgs.Payload.ToArray());
                }
                catch
                {
                    message = eventArgs.Message;
                }
            }
            else
            {
                message = eventArgs.Message;
            }

            return string.Format(
                "[{0}/{1}] {2}: {3}",
                System.Diagnostics.Tracing.EventSource.GetName(eventArgs.EventSource.GetType()),
                eventArgs.Channel,
                eventArgs.Level.ToString().ToUpper(),
                message);
        }
    }
}