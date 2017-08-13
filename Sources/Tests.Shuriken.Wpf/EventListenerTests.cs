using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken.Diagnostics;
using Tests.Shared.ViewModels;
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
        [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
        public async Task _Events()
        {
            var sync = new object();

            var operationalEvents = 0;
            var analyticEvents = 0;

            EventHandler<EventWrittenEventArgs> operationalEventHandler = (sender, e) =>
            {
                Assert.IsNull(sender);

                Assert.IsNotNull(e);
                Assert.AreEqual(EventChannel.Operational, e.Channel);

                lock (sync)
                {
                    operationalEvents++;
                }

                AssertDebugMessage(e);
            };

            EventHandler<EventWrittenEventArgs> analyticEventHandler = (sender, e) =>
            {
                Assert.IsNull(sender);

                Assert.IsNotNull(e);
                Assert.AreEqual(EventChannel.Analytic, e.Channel);

                lock (sync)
                {
                    analyticEvents++;
                }

                AssertDebugMessage(e);
            };

            EventHandler<EventWrittenEventArgs> failingEventHandler = (sender, e) => throw new NotSupportedException();

            // subscribing
            EventListener.OperationalEvent += operationalEventHandler;
            EventListener.OperationalEvent += failingEventHandler;
            EventListener.AnalyticEvent += analyticEventHandler;
            EventListener.AnalyticEvent += failingEventHandler;

            try
            {
                // producing events
                await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(async monitorScope => await Task.Delay(500));
            }
            finally
            {
                // unsubscribing
                EventListener.OperationalEvent -= operationalEventHandler;
                EventListener.OperationalEvent -= failingEventHandler;
                EventListener.AnalyticEvent -= analyticEventHandler;
                EventListener.AnalyticEvent -= failingEventHandler;
            }

            lock (sync)
            {
                Assert.IsTrue(operationalEvents > 0);
                Assert.IsTrue(analyticEvents > 0);
            }
        }
    }
}