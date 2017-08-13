using System;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterizedCommandPropertyAccessor : PropertyAccessor
    {
        public ParameterizedCommandPropertyAccessor(
            [NotNull] string name,
            [NotNull] string objectTypeName,
            [NotNull] Func<ObservableObject, CommandBase> getter) : base(name, objectTypeName) => Getter = getter;

        [NotNull]
        public Func<ObservableObject, CommandBase> Getter { get; }

        public override ValueBag CreateValueBag(ObservableObject observableObject) => new ParameterizedCommandValueBag(observableObject, this);
    }
}