using System.Windows.Input;

namespace Shuriken
{
    partial class Command : ICommand
    {
        bool ICommand.CanExecute(object parameter) => CanExecute();

        void ICommand.Execute(object parameter) => Execute();
    }
}