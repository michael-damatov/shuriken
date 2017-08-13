using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal abstract class PropertyAccessor
    {
        protected PropertyAccessor([NotNull] string name, [NotNull] string objectTypeName)
        {
            Name = name;
            ObjectTypeName = objectTypeName;
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public string ObjectTypeName { get; }

        [NotNull]
        public abstract ValueBag CreateValueBag([NotNull] ObservableObject observableObject);
    }
}