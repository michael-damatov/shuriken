using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken.Diagnostics;
using Tests.Shuriken.Wpf.Infrastructure;
using EventListener = Shuriken.Diagnostics.EventListener;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed class EventListenerTests
    {
        [TestMethod]
        public void ToDebugMessage_Null() => Assert.AreEqual("", (null as EventWrittenEventArgs).ToDebugMessage());

        static void AssertDebugMessage(EventWrittenEventArgs e)
        {
            var message = e.ToDebugMessage();

            Assert.IsNotNull(message);
            StringAssert.Contains(message, e.Channel.ToString());
            StringAssert.Contains(message, e.Level.ToString().ToUpper());
        }

        [TestMethod]
        [Timeout(1_000)]
        public async Task _Events()
        {
            var sync = new object();

            var operationalEvents = 0;
            var analyticEvents = 0;

            void OperationalEventHandler(object? sender, EventWrittenEventArgs e)
            {
                NullAssert.IsNull(sender);

                Assert.IsNotNull(e);
                Assert.AreEqual(EventChannel.Operational, e.Channel);

                lock (sync)
                {
                    operationalEvents++;
                }

                AssertDebugMessage(e);
            }

            void AnalyticEventHandler(object? sender, EventWrittenEventArgs e)
            {
                NullAssert.IsNull(sender);

                Assert.IsNotNull(e);
                Assert.AreEqual(EventChannel.Analytic, e.Channel);

                lock (sync)
                {
                    analyticEvents++;
                }

                AssertDebugMessage(e);
            }

            static void FailingEventHandler(object? sender, EventWrittenEventArgs e) => throw new NotSupportedException();

            // subscribing
            EventListener.OperationalEvent += OperationalEventHandler;
            EventListener.OperationalEvent += FailingEventHandler;
            EventListener.AnalyticEvent += AnalyticEventHandler;
            EventListener.AnalyticEvent += FailingEventHandler;

            try
            {
                // producing events
                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(monitorScope => Task.Delay(500));
            }
            finally
            {
                // unsubscribing
                EventListener.OperationalEvent -= OperationalEventHandler;
                EventListener.OperationalEvent -= FailingEventHandler;
                EventListener.AnalyticEvent -= AnalyticEventHandler;
                EventListener.AnalyticEvent -= FailingEventHandler;
            }

            lock (sync)
            {
                Assert.IsTrue(operationalEvents > 0);
                Assert.IsTrue(analyticEvents > 0);
            }
        }
    }
}