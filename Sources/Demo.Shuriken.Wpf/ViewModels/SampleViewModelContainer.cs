using System.Collections.Generic;
using Shuriken;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class SampleViewModelContainer : ObservableObject
    {
        readonly SampleViewModel[] viewModels;

        int current;

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
            get => current;
            set
            {
                current = value;

                for (var i = 0; i < viewModels.Length; i++)
                {
                    viewModels[i].Data = i + current;
                }
            }
        }
    }
}