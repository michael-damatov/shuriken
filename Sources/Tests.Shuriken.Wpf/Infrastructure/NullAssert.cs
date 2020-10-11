using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [DebuggerStepThrough]
    [ExcludeFromCodeCoverage]
    internal static class NullAssert
    {
        [AssertionMethod]
        public static void IsNull<T>(
#if NETCOREAPP
            [MaybeNull]
#endif
            [AssertionCondition(AssertionConditionType.IS_NULL)][NoEnumeration] T value, string? message = null)
        {
            if (value is null)
            {
                return;
            }

            throw new AssertFailedException(message ?? $"{nameof(NullAssert)}.{nameof(IsNull)} failed.");
        }
    }
}