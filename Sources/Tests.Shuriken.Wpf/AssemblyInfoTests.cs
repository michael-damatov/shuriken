using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shared;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed class AssemblyInfoTests
    {
        [TestMethod]
        public void _AssemblyCopyright() => AssemblyAssert.AreAttributesValid(typeof(ObservableObject).Assembly);
    }
}