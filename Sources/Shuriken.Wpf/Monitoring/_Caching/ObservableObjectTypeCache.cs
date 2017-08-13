using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Shuriken.Monitoring
{
    internal static class ObservableObjectTypeCache
    {
        [NotNull]
        static readonly ReaderWriterLockSlim gate = new ReaderWriterLockSlim();

        [NotNull]
        static readonly Dictionary<Type, List<PropertyAccessor>> cache = new Dictionary<Type, List<PropertyAccessor>>();

        [NotNull]
        [ItemNotNull]
        public static List<PropertyAccessor> Register(
            [NotNull] Type type,
            [InstantHandle] [NotNull] Func<List<PropertyAccessor>> propertyAccessorsFactory)
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
        [ItemNotNull]
        public static List<PropertyAccessor> TryGet([NotNull] Type type)
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