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
            [NotNull] Type type, [InstantHandle] [NotNull] Func<List<PropertyAccessor>> propertyAccessorsFactory)
        {
            gate.EnterReadLock();
            try
            {
                List<PropertyAccessor> list;
                if (cache.TryGetValue(type, out list))
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
                List<PropertyAccessor> list;
                if (cache.TryGetValue(type, out list))
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
                List<PropertyAccessor> list;
                cache.TryGetValue(type, out list);
                return list;
            }
            finally
            {
                gate.ExitReadLock();
            }
        }
    }
}