using System.Windows.Input;

namespace Shuriken
{
    partial class ParameterizedCommand<T> : ICommand
    {
        bool ICommand.CanExecute(object parameter)
            => parameter switch
            {
                T value => CanExecute(value),
                null when default(T) is null => CanExecute(default!),
                _ => false,
            };

        void ICommand.Execute(object? parameter)
        {
            switch (parameter)
            {
                case T value:
                    ExecuteCore(value);
                    break;

                case null when default(T) is null:
                    ExecuteCore(default!);
                    break;
            }
        }
    }
}