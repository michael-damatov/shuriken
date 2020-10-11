using System;

namespace Shuriken.Monitoring
{
    internal sealed class PropertyPropertyAccessor : PropertyAccessor
    {
        public PropertyPropertyAccessor(string name, string objectTypeName, Func<ObservableObject, object?> getter, bool useReferenceEquality) : base(
            name,
            objectTypeName)
        {
            Getter = getter;
            UseReferenceEquality = useReferenceEquality;
        }

        public Func<ObservableObject, object?> Getter { get; }

        public bool UseReferenceEquality { get; }

        public override ValueBag CreateValueBag(ObservableObject observableObject) => new PropertyValueBag(observableObject, this);
    }
}