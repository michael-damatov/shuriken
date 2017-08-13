using System.Windows.Input;

namespace Shuriken
{
    partial class ParameterizedCommand<T> : ICommand
    {
        bool ICommand.CanExecute(object parameter)
        {
            switch (parameter)
            {
                case T value:
                    return CanExecute(value);

                case null when ReferenceEquals(default(T), null):
                    return CanExecute(default(T));

                default:
                    return false;
            }
        }

        void ICommand.Execute(object parameter)
        {
            switch (parameter)
            {
                case T value:
                    ExecuteCore(value);
                    break;

                case null when ReferenceEquals(default(T), null):
                    ExecuteCore(default(T));
                    break;
            }
        }
    }
}