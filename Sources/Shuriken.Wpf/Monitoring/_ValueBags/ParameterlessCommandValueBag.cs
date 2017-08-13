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

        public override bool HasValidValue => isValueValid && isCanExecuteValid;

        public override bool HasChangedValue => isValueChanged || isCanExecuteChanged;

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
                try
                {
                    newCanExecute = newValue.CanExecute();

                    isCanExecuteValid = true;
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
                }
            }
        }

        public override void AnalyzeNewValue()
        {
            Debug.Assert(isValueValid);
            Debug.Assert(isCanExecuteValid);

            isValueChanged = currentValue != newValue;

            if (isValueChanged)
            {
                currentValue = newValue;
                isCanExecuteChanged = true;
                currentCanExecute = newCanExecute;
            }
            else
            {
                isCanExecuteChanged = currentCanExecute != newCanExecute;

                if (isCanExecuteChanged)
                {
                    currentCanExecute = newCanExecute;
                }
            }

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
                }
            }

            if (isCanExecuteChanged)
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
                    isCanExecuteChanged = false;
                }
            }
        }
    }
}