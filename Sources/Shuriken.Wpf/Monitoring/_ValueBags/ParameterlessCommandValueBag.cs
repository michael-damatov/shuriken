using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterlessCommandValueBag : ValueBag
    {
        [NotNull]
        readonly ParameterlessCommandPropertyAccessor propertyAccessor;

        volatile ParameterlessCommand currentValue;

        volatile bool currentCanExecute;

        volatile ParameterlessCommand newValue;

        volatile bool newCanExecute;

        volatile bool isValueValid;

        volatile bool isCanExecuteValid;

        volatile bool isValueChanged;

        volatile bool isCanExecuteChanged;

        internal ParameterlessCommandValueBag(
            [NotNull] ObservableObject observableObject,
            [NotNull] ParameterlessCommandPropertyAccessor propertyAccessor)
        {
            this.propertyAccessor = propertyAccessor;

            try
            {
                currentValue = propertyAccessor.Getter(observableObject);
                isValueValid = true;
            }
            catch (Exception e)
            {
                EventSource.Log.UnableInitiallyToReadProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
            }

            if (currentValue != null)
            {
                try
                {
                    currentCanExecute = currentValue.CanExecute();
                    isCanExecuteValid = true;
                }
                catch (Exception e)
                {
                    EventSource.Log.UnableInitiallyToInvokeCommandMethod(
                        propertyAccessor.ObjectTypeName,
                        propertyAccessor.Name,
                        nameof(ParameterlessCommand.CanExecute),
                        e.ToString());
                }
            }
        }

        [MustUseReturnValue]
        ParameterlessCommand GetCurrentValue(ObservableObject observableObject)
        {
            try
            {
                var value = propertyAccessor.Getter(observableObject);

                isValueValid = true;
                return value;
            }
            catch (Exception e)
            {
                if (isValueValid)
                {
                    EventSource.Log.UnableSubsequentlyToReadProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
                }

                isValueValid = false;
                return default;
            }
        }

        [MustUseReturnValue]
        bool GetCurrentGetExecute([NotNull] ParameterlessCommand value)
        {
            try
            {
                var canExecute = value.CanExecute();

                isCanExecuteValid = true;
                return canExecute;
            }
            catch (Exception e)
            {
                if (isCanExecuteValid)
                {
                    EventSource.Log.UnableSubsequentlyToInvokeCommandMethod(
                        propertyAccessor.ObjectTypeName,
                        propertyAccessor.Name,
                        nameof(ParameterlessCommand.CanExecute),
                        e.ToString());
                }

                isCanExecuteValid = false;
                return default;
            }
        }

        public override bool HasValidValue => isValueValid && isCanExecuteValid;

        public override bool HasChangedValue => isValueChanged || isCanExecuteChanged;

        public override void UpdateNewValue(ObservableObject observableObject)
        {
            var value = GetCurrentValue(observableObject);
            if (isValueValid)
            {
                newValue = value;

                if (value != null)
                {
                    var canExecute = GetCurrentGetExecute(value);
                    if (isCanExecuteValid)
                    {
                        newCanExecute = canExecute;
                    }
                }
            }
        }

        public override void AnalyzeNewValue()
        {
            Debug.Assert(isValueValid);
            Debug.Assert(isCanExecuteValid);

            isValueChanged = currentValue != newValue;
            isCanExecuteChanged = isValueChanged || currentCanExecute != newCanExecute;

            newValue = null;
            newCanExecute = false;
        }

        public override void NotifyPropertyChanged(ObservableObject observableObject)
        {
            Debug.Assert(isValueChanged || isCanExecuteChanged);

            if (isValueChanged)
            {
                try
                {
                    observableObject.NotifyPropertyChange(propertyAccessor.Name);
                }
                catch (Exception e)
                {
                    EventSource.Log.UnableToRaisePropertyChangeNotification(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
                }
                finally
                {
                    isValueChanged = false;

                    var value = GetCurrentValue(observableObject);
                    if (isValueValid)
                    {
                        currentValue = value;

                        if (value != null)
                        {
                            var canExecute = GetCurrentGetExecute(value);
                            if (isCanExecuteValid)
                            {
                                currentCanExecute = canExecute;
                            }
                        }
                    }
                }
            }

            if (isCanExecuteChanged)
            {
                var value = currentValue;

                try
                {
                    value?.NotifyCanExecuteChanged();
                }
                catch (Exception e)
                {
                    EventSource.Log.UnableToRaiseCommandPropertyChangeNotification(
                        propertyAccessor.ObjectTypeName,
                        propertyAccessor.Name,
                        e.ToString());
                }
                finally
                {
                    isCanExecuteChanged = false;

                    if (value != null)
                    {
                        var canExecute = GetCurrentGetExecute(value);
                        if (isCanExecuteValid)
                        {
                            currentCanExecute = canExecute;
                        }
                    }
                }
            }
        }
    }
}