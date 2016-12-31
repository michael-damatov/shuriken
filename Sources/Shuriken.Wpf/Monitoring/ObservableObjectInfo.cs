using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    [SuppressMessage("ReSharper", "LoopCanBePartlyConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    internal sealed class ObservableObjectInfo
    {
        static void CreateDynamicMethod(
            [NotNull] PropertyInfo property,
            [NotNull] Type returnType,
            [NotNull] out DynamicMethod dynamicMethod,
            [NotNull] out ILGenerator generator)
        {
            var getMethod = property.GetGetMethod();

            Debug.Assert(getMethod != null);

            dynamicMethod = new DynamicMethod("", returnType, new[] { typeof(ObservableObject) }, true);

            generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            Debug.Assert(property.DeclaringType != null);

            generator.Emit(OpCodes.Castclass, property.DeclaringType);

            generator.Emit(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getMethod);
        }

        /// <remarks>
        /// For the <c>SampleProperty</c> property of a <c>SampleObject</c> the following dynamic method is generated:
        /// <code>
        ///     static object DynamicMethod(ObservableObject arg)
        ///     {
        ///         return ((SampleObject)arg).SampleProperty;
        ///     }
        /// </code>
        /// </remarks>
        [Pure]
        [NotNull]
        static Func<ObservableObject, object> CreatePropertyGetMethod([NotNull] PropertyInfo property)
        {
            Debug.Assert(property.CanRead);
            Debug.Assert(property.GetIndexParameters().Length == 0);

            DynamicMethod dynamicMethod;
            ILGenerator generator;
            CreateDynamicMethod(property, typeof(object), out dynamicMethod, out generator);

            if (property.PropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, property.PropertyType);
            }

            generator.Emit(OpCodes.Ret);

            return (Func<ObservableObject, object>)dynamicMethod.CreateDelegate(typeof(Func<ObservableObject, object>));
        }

        /// <remarks>
        /// For the <c>SampleProperty</c> property of a <c>SampleObject</c> the following dynamic method is generated:
        /// <code>
        ///     static Command DynamicMethod(ObservableObject arg)
        ///     {
        ///         return ((SampleObject)arg).SampleProperty;
        ///     }
        /// </code>
        /// </remarks>
        [Pure]
        [NotNull]
        static Func<ObservableObject, Command> CreateCommandPropertyGetMethod([NotNull] PropertyInfo property)
        {
            Debug.Assert(property.CanRead);
            Debug.Assert(property.GetIndexParameters().Length == 0);
            Debug.Assert(property.PropertyType == typeof(Command));

            DynamicMethod dynamicMethod;
            ILGenerator generator;
            CreateDynamicMethod(property, typeof(Command), out dynamicMethod, out generator);

            generator.Emit(OpCodes.Ret);

            return (Func<ObservableObject, Command>)dynamicMethod.CreateDelegate(typeof(Func<ObservableObject, Command>));
        }

        [Pure]
        [NotNull]
        [ItemNotNull]
        static IEnumerable<PropertyInfo> GetPropertiesAnnotatedAsObservable([NotNull] Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && ObservableAttribute.IsDefined(property))
                {
                    yield return property;
                }
            }
        }

        [Pure]
        internal static bool HasObservableProperties([NotNull] ObservableObject observableObject)
        {
            var type = observableObject.GetType();

            var propertyAccessors = ObservableObjectTypeCache.TryGet(type);
            if (propertyAccessors != null)
            {
                return propertyAccessors.Count > 0;
            }

            return GetPropertiesAnnotatedAsObservable(type).Any(property => property.GetIndexParameters().Length == 0);
        }

        [NotNull]
        readonly WeakReference<ObservableObject> observableObjectReference;

        [NotNull]
        [ItemNotNull]
        readonly List<ValueBag> valueBags;

        public ObservableObjectInfo([NotNull] ObservableObject observableObject)
        {
            var type = observableObject.GetType();

            var propertyAccessors = ObservableObjectTypeCache.Register(
                type,
                () =>
                {
                    var list = new List<PropertyAccessor>();

                    var objectTypeName = type.Name;

                    foreach (var property in GetPropertiesAnnotatedAsObservable(type))
                    {
                        if (property.GetIndexParameters().Length == 0)
                        {
                            var propertyType = property.PropertyType;

                            if (propertyType == typeof(Command))
                            {
                                list.Add(new CommandPropertyAccessor(property.Name, objectTypeName, CreateCommandPropertyGetMethod(property)));
                            }
                            else
                            {
                                list.Add(
                                    new PropertyPropertyAccessor(
                                        property.Name,
                                        objectTypeName,
                                        CreatePropertyGetMethod(property),
                                        !propertyType.IsValueType));
                            }
                        }
                    }

                    return list;
                });

            observableObjectReference = new WeakReference<ObservableObject>(observableObject);
            valueBags = propertyAccessors.ConvertAll<ValueBag>(
                propertyAccessor =>
                {
                    var propertyPropertyAccessor = propertyAccessor as PropertyPropertyAccessor;
                    if (propertyPropertyAccessor != null)
                    {
                        return new PropertyValueBag(observableObject, propertyPropertyAccessor);
                    }

                    var commandPropertyAccessor = propertyAccessor as CommandPropertyAccessor;
                    if (commandPropertyAccessor != null)
                    {
                        return new CommandValueBag(observableObject, commandPropertyAccessor);
                    }

                    throw new NotSupportedException();
                });
        }

        public bool HasChangedProperties
        {
            get
            {
                foreach (var valueBag in valueBags)
                {
                    if (valueBag.HasChangedValue)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <returns>
        /// <c>true</c> if the monitoring of the observable object should continue; <c>false</c> if the monitoring of the observable object should be
        /// stopped because the object has been GCed.
        /// </returns>
        public bool UpdateValues()
        {
            ObservableObject observableObject;
            if (observableObjectReference.TryGetTarget(out observableObject))
            {
                foreach (var valueBag in valueBags)
                {
                    valueBag.UpdateNewValue(observableObject);
                }

                return true;
            }

            return false;
        }

        /// <returns>The number of properties.</returns>
        public int AnalyzeValues()
        {
            foreach (var valueBag in valueBags)
            {
                if (valueBag.HasValidValue)
                {
                    valueBag.AnalyzeNewValue();
                }
            }

            return valueBags.Count;
        }

        /// <returns>
        /// <c>true</c> if the monitoring of the observable object should continue; <c>false</c> if the monitoring of the observable object should be
        /// stopped because the object has been GCed.
        /// </returns>
        public bool SendNotifications()
        {
            ObservableObject observableObject;
            if (observableObjectReference.TryGetTarget(out observableObject))
            {
                foreach (var valueBag in valueBags)
                {
                    if (valueBag.HasChangedValue)
                    {
                        valueBag.NotifyPropertyChanged(observableObject);
                    }
                }

                return true;
            }

            return false;
        }
    }
}