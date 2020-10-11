using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using JetBrains.Annotations;
using Shuriken.Monitoring;

namespace Shuriken
{
    partial class ObservableObject : INotifyPropertyChanged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly HashSet<PropertyChangedEventHandler> subscribers = new HashSet<PropertyChangedEventHandler>();

        [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Local",
            Justification = "The collection type should be used to improve performance and reduce memory load for iterations.")]
        List<PropertyChangedEventHandler> Subscribers
        {
            get
            {
                lock (subscribers)
                {
                    return subscribers.ToList();
                }
            }
        }

        /// <summary>
        /// Notifies the property change.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected internal void NotifyPropertyChange([CallerMemberName] string? propertyName = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Notifies the indexer change.
        /// </summary>
        [NotifyPropertyChangedInvocator]
        protected void NotifyIndexerChange() => OnPropertyChanged(new PropertyChangedEventArgs(Binding.IndexerName));

        void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            foreach (var subscriber in Subscribers)
            {
                subscriber(this, args);
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                if (value != null)
                {
                    lock (subscribers)
                    {
                        if (subscribers.Count == 0)
                        {
                            var applicationMonitorScope = ApplicationMonitorScope.Current;
                            if (applicationMonitorScope != null)
                            {
                                applicationMonitorScope.Register(this);
                            }
                            else
                            {
                                if (ObservableObjectInfo.HasObservableProperties(this))
                                {
                                    Diagnostics.EventSource.Log.MissingMonitoringScope(nameof(PropertyChanged), nameof(ApplicationMonitorScope));
                                }
                            }
                        }

                        subscribers.Add(value);
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (subscribers)
                    {
                        subscribers.Remove(value);

                        if (subscribers.Count == 0)
                        {
                            ApplicationMonitorScope.Current?.Unregister(this);
                        }
                    }
                }
            }
        }
    }
}