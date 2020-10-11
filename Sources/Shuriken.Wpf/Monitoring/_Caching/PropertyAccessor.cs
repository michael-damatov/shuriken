namespace Shuriken.Monitoring
{
    internal abstract class PropertyAccessor
    {
        protected PropertyAccessor(string name, string objectTypeName)
        {
            Name = name;
            ObjectTypeName = objectTypeName;
        }

        public string Name { get; }

        public string ObjectTypeName { get; }

        public abstract ValueBag CreateValueBag(ObservableObject observableObject);
    }
}