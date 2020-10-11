using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shuriken.Wpf.Infrastructure;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed class AssemblyInfoTests
    {
        [TestMethod]
        public void _AssemblyCopyright() => AssemblyAssert.AreAttributesValid(typeof(ObservableObject).Assembly);
    }
}