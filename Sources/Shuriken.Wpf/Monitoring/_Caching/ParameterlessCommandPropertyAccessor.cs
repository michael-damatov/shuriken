using System;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterlessCommandPropertyAccessor : PropertyAccessor
    {
        public ParameterlessCommandPropertyAccessor(
            [NotNull] string name,
            [NotNull] string objectTypeName,
            [NotNull] Func<ObservableObject, ParameterlessCommand> getter) : base(name, objectTypeName) => Getter = getter;

        [NotNull]
        public Func<ObservableObject, ParameterlessCommand> Getter { get; }

        public override ValueBag CreateValueBag(ObservableObject observableObject) => new ParameterlessCommandValueBag(observableObject, this);
    }
}