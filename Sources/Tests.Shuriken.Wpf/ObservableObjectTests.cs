using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shared.ViewModels;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class ObservableObjectTests
    {
        sealed class Ghost : ObservableObject
        {
            [Observable]
            [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
            public bool Active { get; set; }
        }

        [TestMethod]
        [DoNotParallelize]
        public Task _GhostUpdates()
            => ApplicationMonitorScopeController.ExecuteInApplicationMonitorScope(
                async monitorScope =>
                {
                    // property changes exactly in time between the monitor captures the values, so the monitor doesn't detect any changes

                    var ghost = new Ghost();

                    var eventRaiseCount = 0;

                    async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                    {
                        eventRaiseCount++;

                        ghost.Active = false;

                        await Task.Delay(1);

                        ghost.Active = true;
                    }

                    ghost.PropertyChanged += OnPropertyChanged;
                    try
                    {
                        await Task.Delay(50).ConfigureAwait(false);

                        ghost.Active = true;

                        await Task.Delay(1_000).ConfigureAwait(false);
                    }
                    finally
                    {
                        ghost.PropertyChanged -= OnPropertyChanged;
                    }

                    Assert.IsTrue(eventRaiseCount > 1);

                    await Task.Delay(50).ConfigureAwait(false);
                });
    }
}