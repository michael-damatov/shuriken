using System;
using System.Diagnostics.CodeAnalysis;

namespace Shuriken
{
    partial class CommandExecutionController : IProgress<float>
    {
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes",
            Justification = "The same functionality is provided by another method.")]
        void IProgress<float>.Report(float value)
        {
            if (value >= 0f && value <= 1f)
            {
                try
                {
                    ReportProgress(value);
                }
                catch (InvalidOperationException) { }
            }
        }
    }
}