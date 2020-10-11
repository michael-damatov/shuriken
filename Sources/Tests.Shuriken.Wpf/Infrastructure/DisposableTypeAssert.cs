using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [ExcludeFromCodeCoverage]
    internal static class DisposableTypeAssert
    {
        public static void IsValid([InstantHandle] Func<IDisposable> factory, bool multipleInstancesAllowed = true)
        {
            // normal case
            using (factory()) { }

            // "Finalizer" case
            factory();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // double-disposing
            using (var disposable = factory())
            {
                disposable.Dispose();
            }

            if (multipleInstancesAllowed)
            {
                // nested disposables
                using (factory())
                {
                    using (factory()) { }
                }

                // cross-scope disposables
                IDisposable disposable2;
                using (factory())
                {
                    disposable2 = factory();
                }
                using (disposable2) { }
            }
        }
    }
}