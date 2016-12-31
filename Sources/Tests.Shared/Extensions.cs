using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Shared
{
    [ExcludeFromCodeCoverage]
    internal static class Extensions
    {
        [ContractAnnotation("false => stop", true)]
        public static void IsRequiredForTest(this bool precondition)
        {
            if (!precondition)
            {
                Assert.Inconclusive("Test precondition failed.");
            }
        }
    }
}