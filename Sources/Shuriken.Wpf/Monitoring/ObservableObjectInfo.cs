using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    [SuppressMessage("ReSharper", "LoopCanBePartlyConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery", Justification = "LINQ queries are avoided to reduce GC load.")]
    internal sealed class ObservableObjectInfo
    {
        static void CreateDynamicMethod(PropertyInfo property, Type returnType, out DynamicMethod dynamicMethod, out ILGenerator generator)
        {
            var getMethod = property.GetGetMethod();

            dynamicMethod = new DynamicMethod("", returnType, new[] { typeof(ObservableObject) }, true);

            generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            Debug.Assert(property.DeclaringType != null);

            generator.Emit(OpCodes.Castclass, property.DeclaringType);

            generator.Emit(getMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getMethod);
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
        static Func<ObservableObject, object?> CreatePropertyGetMethod(PropertyInfo property)
        {
            Debug.Assert(property.CanRead);
            Debug.Assert(property.GetIndexParameters().Length == 0);

            CreateDynamicMethod(property, typeof(object), out var dynamicMethod, out var generator);

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
        ///     static ParameterlessCommand DynamicMethod(ObservableObject arg)
        ///     {
        ///         return ((SampleObject)arg).SampleProperty;
        ///     }
        /// </code>
        /// </remarks>
        [Pure]
        static Func<ObservableObject, ParameterlessCommand?> CreateParameterlessCommandPropertyGetMethod(PropertyInfo property)
        {
            Debug.Assert(property.CanRead);
            Debug.Assert(property.GetIndexParameters().Length == 0);
            Debug.Assert(property.PropertyType.IsSubclassOf(typeof(ParameterlessCommand)) || property.PropertyType == typeof(ParameterlessCommand));

            CreateDynamicMethod(property, typeof(ParameterlessCommand), out var dynamicMethod, out var generator);

            generator.Emit(OpCodes.Ret);

            return (Func<ObservableObject, ParameterlessCommand?>)dynamicMethod.CreateDelegate(typeof(Func<ObservableObject, ParameterlessCommand?>));
        }

        /// <remarks>
        /// For the <c>SampleProperty</c> property of a <c>SampleObject</c> the following dynamic method is generated:
        /// <code>
        ///     static CommandBase DynamicMethod(ObservableObject arg)
        ///     {
        ///         return ((SampleObject)arg).SampleProperty;
        ///     }
        /// </code>
        /// </remarks>
        [Pure]
        static Func<ObservableObject, CommandBase?> CreateParameterizedCommandPropertyGetMethod(PropertyInfo property)
        {
            Debug.Assert(property.CanRead);
            Debug.Assert(property.GetIndexParameters().Length == 0);
            Debug.Assert(property.PropertyType.IsGenericType);
            Debug.Assert(
                property.PropertyType.GetGenericTypeDefinition() == typeof(Command<>) ||
                    property.PropertyType.GetGenericTypeDefinition() == typeof(AsyncCommand<>) ||
                    property.PropertyType.GetGenericTypeDefinition() == typeof(ParameterizedCommand<>));

            CreateDynamicMethod(property, typeof(CommandBase), out var dynamicMethod, out var generator);

            generator.Emit(OpCodes.Ret);

            return (Func<ObservableObject, CommandBase?>)dynamicMethod.CreateDelegate(typeof(Func<ObservableObject, CommandBase?>));
        }

        [Pure]
        static IEnumerable<PropertyInfo> GetPropertiesAnnotatedAsObservable(Type type)
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
        internal static bool HasObservableProperties(ObservableObject observableObject)
        {
            var type = observableObject.GetType();

            var propertyAccessors = ObservableObjectTypeCache.TryGet(type);
            if (propertyAccessors != null)
            {
                return propertyAccessors.Count > 0;
            }

            return GetPropertiesAnnotatedAsObservable(type).Any(property => property.GetIndexParameters().Length == 0);
        }

        readonly WeakReference<ObservableObject> observableObjectReference;

        readonly List<ValueBag> valueBags;

        public ObservableObjectInfo(ObservableObject observableObject)
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

                            if (propertyType.IsSubclassOf(typeof(CommandBase)))
                            {
                                if (propertyType.IsSubclassOf(typeof(ParameterlessCommand)) || propertyType == typeof(ParameterlessCommand))
                                {
                                    list.Add(
                                        new ParameterlessCommandPropertyAccessor(
                                            property.Name,
                                            objectTypeName,
                                            CreateParameterlessCommandPropertyGetMethod(property)));
                                }
                                else
                                {
                                    Debug.Assert(propertyType.IsGenericType);
                                    Debug.Assert(
                                        propertyType.GetGenericTypeDefinition() == typeof(Command<>) ||
                                            propertyType.GetGenericTypeDefinition() == typeof(AsyncCommand<>) ||
                                            propertyType.GetGenericTypeDefinition() == typeof(ParameterizedCommand<>));

                                    list.Add(
                                        new ParameterizedCommandPropertyAccessor(
                                            property.Name,
                                            objectTypeName,
                                            CreateParameterizedCommandPropertyGetMethod(property)));
                                }
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
            valueBags = propertyAccessors.ConvertAll(propertyAccessor => propertyAccessor.CreateValueBag(observableObject));
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
            if (observableObjectReference.TryGetTarget(out var observableObject))
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
            if (observableObjectReference.TryGetTarget(out var observableObject))
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