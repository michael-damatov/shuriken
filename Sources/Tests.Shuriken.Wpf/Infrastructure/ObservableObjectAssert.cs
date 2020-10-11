using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [ExcludeFromCodeCoverage]
    internal static class ObservableObjectAssert
    {
        const int defaultMaxMillisecondsToWait = 1000;

        [Pure]
        static Dictionary<T, int> ToItemsWithCount<T>(this ICollection<T> items) where T : notnull
            => (from item in items group item by item).ToDictionary(g => g.Key, g => g.Count());

        static void CompareLists<T>(
            ICollection<T> x,
            ICollection<T> y,
            out Dictionary<T, int> distinctXItemsWithCount,
            out Dictionary<T, int> distinctYItemsWithCount) where T : notnull
        {
            distinctXItemsWithCount = x.ToItemsWithCount();
            distinctYItemsWithCount = y.ToItemsWithCount();

            var xKeys = distinctXItemsWithCount.Keys.ToList();

            foreach (var key in xKeys)
            {
                if (distinctYItemsWithCount.ContainsKey(key))
                {
                    var xCount = distinctXItemsWithCount[key];
                    var yCount = distinctYItemsWithCount[key];

                    var min = Math.Min(xCount, yCount);

                    xCount -= min;
                    yCount -= min;

                    if (xCount > 0)
                    {
                        distinctXItemsWithCount[key] = xCount;
                    }
                    else
                    {
                        distinctXItemsWithCount.Remove(key);
                    }

                    if (yCount > 0)
                    {
                        distinctYItemsWithCount[key] = yCount;
                    }
                    else
                    {
                        distinctYItemsWithCount.Remove(key);
                    }
                }
            }
        }

        static EventHandler CreateEventHandler(ICommand command, string commandName, ICollection<string> notifiedCommandNames)
            => (sender, e) =>
            {
                Assert.AreSame(command, sender!);
                notifiedCommandNames.Add(commandName);
            };

        internal static void AnalyzeResults(
            ICollection<string> expectedNotifiedPropertyNames,
            ICollection<string> notifiedPropertyNames,
            string propertiesOrCommands = "properties")
        {
            CompareLists(
                notifiedPropertyNames,
                expectedNotifiedPropertyNames,
                out var notifiedButUnexpectedPropertyNames,
                out var expectedButNotNotifiedPropertyNames);

            var builder = new StringBuilder();

            if (notifiedButUnexpectedPropertyNames.Count > 0)
            {
                builder.Append($"Unexpected {propertiesOrCommands} notified: ");
                builder.Append(
                    string.Join(
                        ", ",
                        from p in notifiedButUnexpectedPropertyNames orderby p.Key select p.Value > 1 ? $"{p.Key} ({p.Value} times)" : p.Key));
                builder.Append(".");
                builder.AppendLine();
            }

            if (expectedButNotNotifiedPropertyNames.Count > 0)
            {
                builder.Append($"Expected {propertiesOrCommands} not notified: ");
                builder.Append(
                    string.Join(
                        ", ",
                        from p in expectedButNotNotifiedPropertyNames orderby p.Key select p.Value > 1 ? $"{p.Key} ({p.Value} times)" : p.Key));
                builder.Append(".");
                builder.AppendLine();
            }

            if (builder.Length > 0)
            {
                throw new AssertFailedException(builder.ToString());
            }
        }

        public static Task RaisesPropertyChangeNotifications<T>(
            T observableObject,
            Action<T> action,
            string[] expectedNotifiedPropertyNames,
            string[] expectedNotifiedCommandNames,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    var notifiedPropertyNames = new List<string>();
                    var notifiedCommandNames = new List<string>();

                    void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
                    {
                        Assert.AreSame(observableObject, sender!);
                        Assert.IsNotNull(e);
                        Assert.IsFalse(string.IsNullOrEmpty(e.PropertyName));

                        notifiedPropertyNames.Add(e.PropertyName!);
                    }

                    var commands = (
                            from property in observableObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            where property.GetIndexParameters().Length == 0 && typeof(ICommand).IsAssignableFrom(property.PropertyType)
                            let command = (ICommand?)property.GetValue(observableObject)
                            where command != null
                            select new
                                {
                                    Command = (ICommand)command, EventHandler = CreateEventHandler(command, property.Name, notifiedCommandNames),
                                })
                        .ToList();
                    foreach (var item in commands)
                    {
                        item.Command.CanExecuteChanged += item.EventHandler;
                    }

                    observableObject.PropertyChanged += PropertyChangedEventHandler;
                    try
                    {
                        await Task.Delay(50);

                        action(observableObject);

                        await Task.Delay(maxMillisecondsToWait);
                    }
                    finally
                    {
                        observableObject.PropertyChanged -= PropertyChangedEventHandler;

                        foreach (var item in commands)
                        {
                            item.Command.CanExecuteChanged -= item.EventHandler;
                        }
                    }

                    AnalyzeResults(expectedNotifiedPropertyNames, notifiedPropertyNames);
                    AnalyzeResults(expectedNotifiedCommandNames, notifiedCommandNames, "commands");

                    await Task.Delay(50).ConfigureAwait(false);
                });

        public static Task RaisesPropertyChangeNotifications<T>(
            T observableObject,
            Action<T> action,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            => RaisesPropertyChangeNotifications(observableObject, action, Array.Empty<string>(), Array.Empty<string>(), maxMillisecondsToWait);

        public static Task RaisesPropertyChangeNotifications<T>(
            T observableObject,
            Action<T> action,
            string expectedNotifiedPropertyName,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            => RaisesPropertyChangeNotifications(
                observableObject,
                action,
                new[] { expectedNotifiedPropertyName },
                Array.Empty<string>(),
                maxMillisecondsToWait);
    }
}