using System.Windows.Input;

namespace Shuriken
{
    partial class ParameterlessCommand : ICommand
    {
        bool ICommand.CanExecute(object? parameter) => CanExecute();

        void ICommand.Execute(object? parameter) => ExecuteCore();
    }
}