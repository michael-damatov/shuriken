using System;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterizedCommandPropertyAccessor : PropertyAccessor
    {
        public ParameterizedCommandPropertyAccessor(string name, string objectTypeName, Func<ObservableObject, CommandBase?> getter) : base(
            name,
            objectTypeName)
            => Getter = getter;

        public Func<ObservableObject, CommandBase?> Getter { get; }

        public override ValueBag CreateValueBag(ObservableObject observableObject) => new ParameterizedCommandValueBag(observableObject, this);
    }
}