using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Shuriken;
using Shuriken.Monitoring;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class SampleViewModelContainer : ObservableObject
    {
        [NotNull]
        [ItemNotNull]
        readonly SampleViewModel[] viewModels;

        int current;

        IDisposable monitoringSuspension;

        public SampleViewModelContainer(int total)
        {
            viewModels = new SampleViewModel[total];

            for (var i = 0; i < viewModels.Length; i++)
            {
                viewModels[i] = new SampleViewModel(i);
            }
        }

        public IEnumerable<SampleViewModel> ViewModels => viewModels;

        [Observable]
        public int Current
        {
            get
            {
                return current;
            }
            set
            {
                current = value;

                for (var i = 0; i < viewModels.Length; i++)
                {
                    viewModels[i].Data = i + current;
                }
            }
        }

        public bool IsMonitoringSuspended
        {
            get
            {
                return monitoringSuspension != null;
            }
            set
            {
                if (value)
                {
                    if (monitoringSuspension == null)
                    {
                        Debug.Assert(ApplicationMonitorScope.Current != null);

                        monitoringSuspension = ApplicationMonitorScope.Current.Suspend();

                        NotifyPropertyChange();
                    }
                }
                else
                {
                    if (monitoringSuspension != null)
                    {
                        monitoringSuspension.Dispose();
                        monitoringSuspension = null;

                        NotifyPropertyChange();
                    }
                }
            }
        }
    }
}