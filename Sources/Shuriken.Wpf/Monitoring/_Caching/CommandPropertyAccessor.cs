using System;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal sealed class CommandPropertyAccessor : PropertyAccessor
    {
        public CommandPropertyAccessor([NotNull] string name, [NotNull] string objectTypeName, [NotNull] Func<ObservableObject, Command> getter)
            : base(name, objectTypeName)
        {
            Getter = getter;
        }

        [NotNull]
        public Func<ObservableObject, Command> Getter { get; }
    }
}