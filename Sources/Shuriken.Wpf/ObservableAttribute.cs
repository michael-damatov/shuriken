using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Shuriken
{
    /// <summary>
    /// Annotates the property as observable.
    /// </summary>
    /// <remarks>
    /// To be monitored the property must belong to a class that is a subclass of the <see cref="ObservableObject"/> class.<para />
    /// For thread-safety reasons the equality analysis uses the <see cref="Object.ReferenceEquals"/> method to compare reference type objects and
    /// the <see cref="Object.Equals(object,object)"/> method for value type objects. Mutable value type should be avoided, because analyzing value
    /// changes is not guaranteed to be thread-safe.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ObservableAttribute : Attribute
    {
        [Pure]
        internal static bool IsDefined([NotNull] PropertyInfo property) => IsDefined(property, typeof(ObservableAttribute), true);
    }
}