namespace Shuriken.Monitoring
{
    internal abstract class ValueBag
    {
        public abstract bool HasValidValue { get; }

        public abstract bool HasChangedValue { get; }

        public abstract void UpdateNewValue(ObservableObject observableObject);

        public abstract void AnalyzeNewValue();

        public abstract void NotifyPropertyChanged(ObservableObject observableObject);
    }
}