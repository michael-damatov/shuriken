using System;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterlessCommandPropertyAccessor : PropertyAccessor
    {
        public ParameterlessCommandPropertyAccessor(string name, string objectTypeName, Func<ObservableObject, ParameterlessCommand?> getter) : base(
            name,
            objectTypeName)
            => Getter = getter;

        public Func<ObservableObject, ParameterlessCommand?> Getter { get; }

        public override ValueBag CreateValueBag(ObservableObject observableObject) => new ParameterlessCommandValueBag(observableObject, this);
    }
}