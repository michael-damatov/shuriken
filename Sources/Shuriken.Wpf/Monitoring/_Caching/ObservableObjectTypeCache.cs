using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal static class ObservableObjectTypeCache
    {
        static readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim();

        static readonly Dictionary<Type, List<PropertyAccessor>> cache = new Dictionary<Type, List<PropertyAccessor>>();

        public static List<PropertyAccessor> Register(Type type, [InstantHandle] Func<List<PropertyAccessor>> propertyAccessorsFactory)
        {
            gate.EnterReadLock();
            try
            {
                if (cache.TryGetValue(type, out var list))
                {
                    return list;
                }
            }
            finally
            {
                gate.ExitReadLock();
            }

            gate.EnterWriteLock();
            try
            {
                if (cache.TryGetValue(type, out var list))
                {
                    return list;
                }

                list = propertyAccessorsFactory();
                cache.Add(type, list);
                return list;
            }
            finally
            {
                gate.ExitWriteLock();
            }
        }

        [Pure]
        public static List<PropertyAccessor>? TryGet(Type type)
        {
            gate.EnterReadLock();
            try
            {
                return cache.TryGetValue(type, out var list) ? list : null;
            }
            finally
            {
                gate.ExitReadLock();
            }
        }
    }
}