using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal abstract class ValueBag
    {
        public abstract bool HasValidValue { get; }

        public abstract bool HasChangedValue { get; }

        public abstract void UpdateNewValue([NotNull] ObservableObject observableObject);

        public abstract void AnalyzeNewValue();

        public abstract void NotifyPropertyChanged([NotNull] ObservableObject observableObject);
    }
}