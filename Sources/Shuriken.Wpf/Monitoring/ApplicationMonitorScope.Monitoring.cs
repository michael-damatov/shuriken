using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    [SuppressMessage("ReSharper", "LoopCanBePartlyConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    partial class ApplicationMonitorScope
    {
        readonly struct Sets
        {
            readonly HashSet<ObservableObjectInfo>? threadAffine;

            readonly HashSet<ObservableObjectInfo>? threadSafe;

            public Sets(HashSet<ObservableObjectInfo> threadAffine, HashSet<ObservableObjectInfo> threadSafe)
            {
                Debug.Assert(threadAffine.Count == 0);
                Debug.Assert(threadSafe.Count == 0);
                Debug.Assert(threadAffine != threadSafe);

                this.threadAffine = threadAffine;
                this.threadSafe = threadSafe;
            }

            public HashSet<ObservableObjectInfo> ThreadAffine => threadAffine!;

            public HashSet<ObservableObjectInfo> ThreadSafe => threadSafe!;
        }

        readonly struct Lists
        {
            public static void LogPerformanceLists(
                [NonNegativeValue] int capacityThreadAffine,
                [NonNegativeValue] int countThreadAffine,
                [NonNegativeValue] int capacityThreadSafe,
                [NonNegativeValue] int countThreadSafe)
                => EventSource.Log.PerformanceLists(capacityThreadAffine, countThreadAffine, capacityThreadSafe, countThreadSafe);

            public static void LogPerformanceListsWithChangedProperties(
                [NonNegativeValue] int capacityThreadAffine,
                [NonNegativeValue] int countThreadAffine,
                [NonNegativeValue] int capacityThreadSafe,
                [NonNegativeValue] int countThreadSafe)
                => EventSource.Log.PerformanceListsWithChangedProperties(
                    capacityThreadAffine,
                    countThreadAffine,
                    capacityThreadSafe,
                    countThreadSafe);

            public static void LogPerformanceListsWithItemsToBeRemoved(
                [NonNegativeValue] int capacityThreadAffine,
                [NonNegativeValue] int countThreadAffine,
                [NonNegativeValue] int capacityThreadSafe,
                [NonNegativeValue] int countThreadSafe)
                => EventSource.Log.PerformanceListsWithItemsToBeRemoved(capacityThreadAffine, countThreadAffine, capacityThreadSafe, countThreadSafe);

            readonly Action<int, int, int, int>? logPerformance;

            readonly List<ObservableObjectInfo>? threadAffine;

            readonly List<ObservableObjectInfo>? threadSafe;

            public Lists(Action<int, int, int, int> logPerformance)
            {
                this.logPerformance = logPerformance;

                threadAffine = new List<ObservableObjectInfo>();
                threadSafe = new List<ObservableObjectInfo>();
            }

            List<ObservableObjectInfo> ThreadAffine => threadAffine!;

            List<ObservableObjectInfo> ThreadSafe => threadSafe!;

            public bool AreEmpty => ThreadAffine.Count == 0 && ThreadSafe.Count == 0;

            public void Clear()
            {
                ThreadAffine.Clear();
                ThreadSafe.Clear();
            }

            public void Add(Sets sets)
            {
                ThreadAffine.AddRange(sets.ThreadAffine);
                ThreadSafe.AddRange(sets.ThreadSafe);
            }

            /// <exception cref="InvalidOperationException">
            ///     The <see cref="INotificationContext.InvokeAsync"/> method of the <see cref="INotificationContext"/> object returns <c>null</c>.
            /// </exception>
            public void UpdateValues(INotificationContext notificationContext, Lists listsWithItemsToBeRemoved)
            {
                var local = this;
                var threadAffineUpdates = notificationContext.InvokeAsync(
                    () =>
                    {
                        foreach (var observableObjectInfo in local.ThreadAffine)
                        {
                            if (!observableObjectInfo.UpdateValues())
                            {
                                listsWithItemsToBeRemoved.ThreadAffine.Add(observableObjectInfo);
                            }
                        }
                    });
                if (threadAffineUpdates == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "The '{0}' method of the '{1}' class returns null.",
                            nameof(INotificationContext.InvokeAsync),
                            notificationContext.GetType().Name));
                }

                foreach (var observableObjectInfo in ThreadSafe)
                {
                    if (!observableObjectInfo.UpdateValues())
                    {
                        listsWithItemsToBeRemoved.ThreadSafe.Add(observableObjectInfo);
                    }
                }

                threadAffineUpdates.GetAwaiter().GetResult();
            }

            [NonNegativeValue]
            public int AnalyzeValues()
            {
                var monitoredProperties = 0;

                foreach (var observableObjectInfo in ThreadAffine)
                {
                    monitoredProperties += observableObjectInfo.AnalyzeValues();
                }
                foreach (var observableObjectInfo in ThreadSafe)
                {
                    monitoredProperties += observableObjectInfo.AnalyzeValues();
                }

                return monitoredProperties;
            }

            public void AddWithChangedProperties(Lists lists)
            {
                foreach (var observableObjectInfo in lists.ThreadAffine)
                {
                    if (observableObjectInfo.HasChangedProperties)
                    {
                        ThreadAffine.Add(observableObjectInfo);
                    }
                }

                foreach (var observableObjectInfo in lists.ThreadSafe)
                {
                    if (observableObjectInfo.HasChangedProperties)
                    {
                        ThreadSafe.Add(observableObjectInfo);
                    }
                }
            }

            public void SendNotifications(INotificationContext notificationContext, Lists listsWithItemsToBeRemoved)
            {
                if (ThreadAffine.Count == 0 && ThreadSafe.Count == 0)
                {
                    return;
                }

                var local = this;
                notificationContext.Invoke(
                    () =>
                    {
                        foreach (var observableObjectInfo in local.ThreadAffine)
                        {
                            if (!observableObjectInfo.SendNotifications())
                            {
                                listsWithItemsToBeRemoved.ThreadAffine.Add(observableObjectInfo);
                            }
                        }

                        foreach (var observableObjectInfo in local.ThreadSafe)
                        {
                            if (!observableObjectInfo.SendNotifications())
                            {
                                listsWithItemsToBeRemoved.ThreadSafe.Add(observableObjectInfo);
                            }
                        }
                    });
            }

            public void ClearSets(Sets sets)
            {
                sets.ThreadAffine.ExceptWith(ThreadAffine);
                sets.ThreadSafe.ExceptWith(ThreadSafe);
            }

            [Conditional("TRACE")]
            public void LogPerformance() => logPerformance!(ThreadAffine.Capacity, ThreadAffine.Count, ThreadSafe.Capacity, ThreadSafe.Count);
        }

        /// <remarks>
        /// Serves also as the sync root for <see cref="observableObjectInfos"/>.
        /// </remarks>
        readonly ConditionalWeakTable<ObservableObject, ObservableObjectInfo> observableObjectTable =
            new ConditionalWeakTable<ObservableObject, ObservableObjectInfo>();

        readonly Sets observableObjectInfos = new Sets(new HashSet<ObservableObjectInfo>(), new HashSet<ObservableObjectInfo>());

        /// <exception cref="InvalidOperationException">
        ///     The <see cref="INotificationContext.InvokeAsync"/> method of the <see cref="INotificationContext"/> object returns <c>null</c>.
        /// </exception>
        [SuppressMessage("ReSharper", "TooWideLocalVariableScope",
            Justification = "Redeclaration of the 'monitoredProperties' variable is avoided to reduce stack load.")]
        void RunMonitor()
        {
            var lists = new Lists(Lists.LogPerformanceLists);
            var listsWithChangedProperties = new Lists(Lists.LogPerformanceListsWithChangedProperties);
            var listsWithItemsToBeRemoved = new Lists(Lists.LogPerformanceListsWithItemsToBeRemoved);

            var stopwatch = new Stopwatch();

            int monitoredProperties;

            while (!cancellation.IsCancellationRequested)
            {
                stopwatch.Stop();

                EventSource.Log.PerformanceCycleTime(stopwatch.ElapsedMilliseconds);

                if (cancellation.Token.WaitHandle.WaitOne(15))
                {
                    return;
                }

                countEventForSuspensions.WaitForZeroOrCancellation(cancellation.Token);
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }

                stopwatch.Restart();

                lists.Clear();
                listsWithItemsToBeRemoved.Clear();
                listsWithChangedProperties.Clear();

                lock (observableObjectTable)
                {
                    lists.Add(observableObjectInfos);
                }

                lists.LogPerformance();

                if (lists.AreEmpty)
                {
                    listsWithChangedProperties.LogPerformance();
                    listsWithItemsToBeRemoved.LogPerformance();

                    continue;
                }

                // updating values (retrieving current values)
                try
                {
                    lists.UpdateValues(NotificationContext, listsWithItemsToBeRemoved);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    EventSource.Log.StoppingDueToFailedUpdate(e.ToString());

                    throw;
                }

                // analyzing values
                monitoredProperties = lists.AnalyzeValues();

                // sending change notifications
                listsWithChangedProperties.AddWithChangedProperties(lists);

                listsWithChangedProperties.LogPerformance();

                try
                {
                    listsWithChangedProperties.SendNotifications(NotificationContext, listsWithItemsToBeRemoved);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    EventSource.Log.StoppingDueToFailedChangeNotifications(e.ToString());

                    throw;
                }

                EventSource.Log.PerformanceMonitoredProperties(monitoredProperties);

                // clearing
                listsWithItemsToBeRemoved.LogPerformance();

                if (listsWithItemsToBeRemoved.AreEmpty)
                {
                    continue;
                }

                lock (observableObjectTable)
                {
                    listsWithItemsToBeRemoved.ClearSets(observableObjectInfos);
                }
            }
        }

        internal void Register(ObservableObject observableObject)
        {
            var observableObjectInfo = observableObjectTable.GetValue(observableObject, o => new ObservableObjectInfo(o));

            var set = observableObject.IsThreadSafe ? observableObjectInfos.ThreadSafe : observableObjectInfos.ThreadAffine;

            lock (observableObjectTable)
            {
                set.Add(observableObjectInfo);
            }
        }

        internal void Unregister(ObservableObject observableObject)
        {
            if (observableObjectTable.TryGetValue(observableObject, out var observableObjectInfo))
            {
                var set = observableObject.IsThreadSafe ? observableObjectInfos.ThreadSafe : observableObjectInfos.ThreadAffine;

                lock (observableObjectTable)
                {
                    set.Remove(observableObjectInfo);
                }
            }

            observableObjectTable.Remove(observableObject);
        }
    }
}