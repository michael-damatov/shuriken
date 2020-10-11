using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterizedCommandValueBag : ValueBag
    {
        readonly ParameterizedCommandPropertyAccessor propertyAccessor;

        volatile CommandBase? currentValue;

        volatile bool currentIsExecuting;

        volatile CommandBase? newValue;

        volatile bool newIsExecuting;

        volatile bool isValueValid;

        volatile bool isIsExecutingValid;

        volatile bool isValueChanged;

        volatile bool isIsExecutingChanged;

        public ParameterizedCommandValueBag(ObservableObject observableObject, ParameterizedCommandPropertyAccessor propertyAccessor)
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
                currentIsExecuting = currentValue.RunningExecution != null;
                isIsExecutingValid = true;
            }
        }

        [MustUseReturnValue]
        CommandBase? GetCurrentValue(ObservableObject observableObject)
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
        bool GetCurrentIsExecuting(CommandBase value)
        {
            var isExecuting = value.RunningExecution != null;

            isIsExecutingValid = true;

            return isExecuting;
        }

        public override bool HasValidValue => isValueValid && isIsExecutingValid;

        public override bool HasChangedValue => isValueChanged || isIsExecutingChanged;

        public override void UpdateNewValue(ObservableObject observableObject)
        {
            var value = GetCurrentValue(observableObject);
            if (isValueValid)
            {
                newValue = value;

                if (value != null)
                {
                    var isExecuting = GetCurrentIsExecuting(value);

                    Debug.Assert(isIsExecutingValid);
                    newIsExecuting = isExecuting;
                }
            }
        }

        public override void AnalyzeNewValue()
        {
            Debug.Assert(isValueValid);
            Debug.Assert(isIsExecutingValid);

            isValueChanged = currentValue != newValue;
            isIsExecutingChanged = isValueChanged || currentIsExecuting != newIsExecuting;

            newValue = null;
            newIsExecuting = false;
        }

        public override void NotifyPropertyChanged(ObservableObject observableObject)
        {
            Debug.Assert(isValueChanged || isIsExecutingChanged);

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
                            var isExecuting = GetCurrentIsExecuting(value);

                            Debug.Assert(isIsExecutingValid);
                            currentIsExecuting = isExecuting;
                        }
                    }
                }
            }

            if (isIsExecutingChanged)
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
                    isIsExecutingChanged = false;

                    if (value != null)
                    {
                        var isExecuting = GetCurrentIsExecuting(value);

                        Debug.Assert(isIsExecutingValid);
                        currentIsExecuting = isExecuting;
                    }
                }
            }
        }
    }
}