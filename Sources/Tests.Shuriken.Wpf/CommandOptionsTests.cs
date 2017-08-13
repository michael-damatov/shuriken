using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed class CommandOptionsTests
    {
        [TestMethod]
        public void _Ctor()
        {
            var options = new CommandOptions(true);

            Assert.IsTrue(options.IsCancelCommandEnabled);
            Assert.IsTrue(options.TraceWhenFailed);
        }
    }
}