using System;
using System.Windows.Input;

namespace Shuriken
{
    partial class Command<T> : ICommand
    {
        static readonly bool isReferenceOrNullableValueType = !typeof(T).IsValueType ||
                                                              typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);

        bool ICommand.CanExecute(object parameter)
        {
            if (parameter is T)
            {
                return CanExecute((T)parameter);
            }

            if (parameter == null && isReferenceOrNullableValueType)
            {
                return CanExecute((T)(null as object));
            }

            return false;
        }

        void ICommand.Execute(object parameter)
        {
            if (((ICommand)this).CanExecute(parameter))
            {
                Execute((T)parameter);
            }
        }
    }
}