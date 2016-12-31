using System;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal sealed class PropertyPropertyAccessor : PropertyAccessor
    {
        public PropertyPropertyAccessor(
            [NotNull] string name,
            [NotNull] string objectTypeName,
            [NotNull] Func<ObservableObject, object> getter,
            bool useReferenceEquality) : base(name, objectTypeName)
        {
            Getter = getter;
            UseReferenceEquality = useReferenceEquality;
        }

        [NotNull]
        public Func<ObservableObject, object> Getter { get; }

        public bool UseReferenceEquality { get; }
    }
}