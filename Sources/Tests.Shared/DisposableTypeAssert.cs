using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Tests.Shared
{
    [ExcludeFromCodeCoverage]
    internal static class DisposableTypeAssert
    {
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static void IsValid([NotNull] Func<IDisposable> factory, bool multipleInstancesAllowed = true)
        {
            // normal case
            using (var disposable = factory())
            {
                (disposable != null).IsRequiredForTest();
            }

            // "Finalizer" case
            factory();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // double-disposing
            using (var disposable = factory())
            {
                (disposable != null).IsRequiredForTest();

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