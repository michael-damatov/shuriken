using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    internal sealed class ParameterizedCommandValueBag : ValueBag
    {
        [NotNull]
        readonly ParameterizedCommandPropertyAccessor propertyAccessor;

        volatile CommandBase currentValue;

        volatile bool currentIsExecuting;

        volatile CommandBase newValue;

        volatile bool newIsExecuting;

        volatile bool isValueValid;

        volatile bool isIsExecutingValid;

        volatile bool isValueChanged;

        volatile bool isIsExecutingChanged;

        internal ParameterizedCommandValueBag(
            [NotNull] ObservableObject observableObject,
            [NotNull] ParameterizedCommandPropertyAccessor propertyAccessor)
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

        public override bool HasValidValue => isValueValid && isIsExecutingValid;

        public override bool HasChangedValue => isValueChanged || isIsExecutingChanged;

        public override void UpdateNewValue(ObservableObject observableObject)
        {
            try
            {
                newValue = propertyAccessor.Getter(observableObject);
                isValueValid = true;
            }
            catch (Exception e)
            {
                if (isValueValid)
                {
                    EventSource.Log.UnableSubsequentlyToReadProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
                }

                isValueValid = false;
            }

            if (isValueValid && newValue != null)
            {
                newIsExecuting = newValue.RunningExecution != null;

                isIsExecutingValid = true;
            }
        }

        public override void AnalyzeNewValue()
        {
            Debug.Assert(isValueValid);
            Debug.Assert(isIsExecutingValid);

            isValueChanged = currentValue != newValue;

            if (isValueChanged)
            {
                currentValue = newValue;
                isIsExecutingChanged = true;
                currentIsExecuting = newIsExecuting;
            }
            else
            {
                isIsExecutingChanged = currentIsExecuting != newIsExecuting;

                if (isIsExecutingChanged)
                {
                    currentIsExecuting = newIsExecuting;
                }
            }

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
                }
            }

            if (isIsExecutingChanged)
            {
                Debug.Assert(currentValue != null);

                try
                {
                    currentValue.NotifyCanExecuteChanged();
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
                }
            }
        }
    }
}