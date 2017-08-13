using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class SampleViewModelContainerRegular : INotifyPropertyChanged
    {
        [NotNull]
        [ItemNotNull]
        readonly SampleViewModelRegular[] viewModels;

        int current;

        public SampleViewModelContainerRegular(int total)
        {
            viewModels = new SampleViewModelRegular[total];

            for (var i = 0; i < viewModels.Length; i++)
            {
                viewModels[i] = new SampleViewModelRegular(i);
            }
        }

        public IEnumerable<SampleViewModelRegular> ViewModels => viewModels;

        public int Current
        {
            get => current;
            set
            {
                if (current != value)
                {
                    current = value;

                    for (var i = 0; i < viewModels.Length; i++)
                    {
                        viewModels[i].Data = i + current;
                    }

                    OnPropertyChanged(new PropertyChangedEventArgs("Current"));
                }
            }
        }

        void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}