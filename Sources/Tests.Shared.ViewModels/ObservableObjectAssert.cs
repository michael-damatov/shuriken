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

namespace Tests.Shared.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal static class ObservableObjectAssert
    {
        const int defaultMaxMillisecondsToWait = 1000;

        [Pure]
        [NotNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        static Dictionary<T, int> ToItemsWithCount<T>([NotNull] this ICollection<T> items)
            => (from item in items group item by item).ToDictionary(g => g.Key, g => g.Count());

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        static void CompareLists<T>(
            [NotNull] ICollection<T> x,
            [NotNull] ICollection<T> y,
            [NotNull] out Dictionary<T, int> distinctXItemsWithCount,
            [NotNull] out Dictionary<T, int> distinctYItemsWithCount)
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

        [NotNull]
        static EventHandler CreateEventHandler(
            [NotNull] ICommand command,
            [NotNull] string commandName,
            [NotNull] [ItemNotNull] ICollection<string> notifiedCommandNames) => (sender, e) =>
            {
                Assert.AreSame(command, sender);
                notifiedCommandNames.Add(commandName);
            };

        internal static void AnalyzeResults(
            [NotNull] ICollection<string> expectedNotifiedPropertyNames,
            [NotNull] ICollection<string> notifiedPropertyNames,
            [NotNull] string propertiesOrCommands = "properties")
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

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
        public static async Task RaisesPropertyChangeNotifications<T>(
            [NotNull] T observableObject,
            [NotNull] Action<T> action,
            [NotNull] [ItemNotNull] string[] expectedNotifiedPropertyNames,
            [NotNull] [ItemNotNull] string[] expectedNotifiedCommandNames,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            => await ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    var notifiedPropertyNames = new List<string>();
                    var notifiedCommandNames = new List<string>();

                    PropertyChangedEventHandler propertyChangedEventHandler = (sender, e) =>
                    {
                        Assert.AreSame(observableObject, sender);
                        Assert.IsNotNull(e);
                        Assert.IsFalse(string.IsNullOrEmpty(e.PropertyName));

                        notifiedPropertyNames.Add(e.PropertyName);
                    };

                    var commands = (
                        from property in observableObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        where property.GetIndexParameters().Length == 0 && typeof(ICommand).IsAssignableFrom(property.PropertyType)
                        let command = (ICommand)property.GetValue(observableObject)
                        where command != null
                        select new { Command = command, EventHandler = CreateEventHandler(command, property.Name, notifiedCommandNames) }).ToList();
                    foreach (var item in commands)
                    {
                        item.Command.CanExecuteChanged += item.EventHandler;
                    }

                    observableObject.PropertyChanged += propertyChangedEventHandler;
                    try
                    {
                        await Task.Delay(50);

                        action(observableObject);

                        await Task.Delay(maxMillisecondsToWait);
                    }
                    finally
                    {
                        observableObject.PropertyChanged -= propertyChangedEventHandler;

                        foreach (var item in commands)
                        {
                            item.Command.CanExecuteChanged -= item.EventHandler;
                        }
                    }

                    AnalyzeResults(expectedNotifiedPropertyNames, notifiedPropertyNames);
                    AnalyzeResults(expectedNotifiedCommandNames, notifiedCommandNames, "commands");

                    await Task.Delay(50);
                });

        public static async Task RaisesPropertyChangeNotifications<T>(
            [NotNull] T observableObject,
            [NotNull] Action<T> action,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            =>
                await
                    RaisesPropertyChangeNotifications(observableObject, action, new string[] { }, new string[] { }, maxMillisecondsToWait)
                        .ConfigureAwait(false);

        public static async Task RaisesPropertyChangeNotifications<T>(
            [NotNull] T observableObject,
            [NotNull] Action<T> action,
            [NotNull] string expectedNotifiedPropertyName,
            int maxMillisecondsToWait = defaultMaxMillisecondsToWait) where T : ObservableObject
            =>
                await
                    RaisesPropertyChangeNotifications(
                        observableObject,
                        action,
                        new[] { expectedNotifiedPropertyName },
                        new string[] { },
                        maxMillisecondsToWait).ConfigureAwait(false);
    }
}