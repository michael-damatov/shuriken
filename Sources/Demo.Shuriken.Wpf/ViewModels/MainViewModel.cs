using JetBrains.Annotations;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class MainViewModel
    {
        const int total = 200;

        [NotNull]
        public SampleViewModelContainerRegular ContainerRegular { get; } = new SampleViewModelContainerRegular(total);

        [NotNull]
        public SampleViewModelContainer Container { get; } = new SampleViewModelContainer(total);
    }
}