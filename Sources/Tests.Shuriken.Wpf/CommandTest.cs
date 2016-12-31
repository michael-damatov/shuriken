using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuriken;
using Tests.Shared;

namespace Tests.Shuriken.Wpf
{
    [TestClass]
    public sealed class CommandTest
    {
        static void CanExecuteChanged([NotNull] CommandBase command)
        {
            var eventRaised = false;

            command.CanExecuteChanged += (sender, e) =>
            {
                Assert.AreSame(command, sender);
                Assert.AreEqual(EventArgs.Empty, e);
                eventRaised = true;
            };

            command.NotifyCanExecuteChanged();

            Assert.IsTrue(eventRaised);
        }

        static void Command([NotNull] Command command, ref int value, bool canExecute)
        {
            Assert.AreEqual(canExecute, command.CanExecute());
            Assert.AreEqual(canExecute, ((ICommand)command).CanExecute(null));

            var expectedValue = value;

            command.Execute();
            if (canExecute)
            {
                expectedValue++;
            }
            Assert.AreEqual(expectedValue, value);

            ((ICommand)command).Execute(null);
            if (canExecute)
            {
                expectedValue++;
            }
            Assert.AreEqual(expectedValue, value);

            CanExecuteChanged(command);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void Command()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command(null), "execute");

            var i = 0;
            Action execute = () => i++;

            Command(new Command(execute), ref i, true);
            Command(new Command(execute, () => true), ref i, true);
            Command(new Command(execute, () => false), ref i, false);
        }

        static void Command<T>(
            [NotNull] Command<T> command,
            ref int value,
            bool canExecute,
            [NotNull] [ItemNotNull] params Tuple<object, bool>[] canExecuteInputOutputs)
        {
            Assert.AreEqual(canExecute, command.CanExecute(default(T)));
            foreach (var inputOutput in canExecuteInputOutputs)
            {
                Assert.AreEqual(inputOutput.Item2, ((ICommand)command).CanExecute(inputOutput.Item1));
            }

            var expectedValue = value;

            command.Execute(default(T));
            if (canExecute)
            {
                expectedValue++;
            }
            Assert.AreEqual(expectedValue, value);

            ((ICommand)command).Execute(default(T));
            if (canExecute)
            {
                expectedValue++;
            }
            Assert.AreEqual(expectedValue, value);

            CanExecuteChanged(command);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void Command_WithParameter()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new Command<int>(null), "execute");

            var i = 0;

            Action<int> executeForValueType = arg => i++;
            Command(
                new Command<int>(executeForValueType),
                ref i,
                true,
                Tuple.Create(null as object, false),
                Tuple.Create(1 as object, true),
                Tuple.Create("one" as object, false));
            Command(
                new Command<int>(executeForValueType, arg => true),
                ref i,
                true,
                Tuple.Create(null as object, false),
                Tuple.Create(1 as object, true),
                Tuple.Create("one" as object, false));
            Command(
                new Command<int>(executeForValueType, arg => false),
                ref i,
                false,
                Tuple.Create(null as object, false),
                Tuple.Create(1 as object, false),
                Tuple.Create("one" as object, false));

            Action<int?> executeForNullableValueType = arg => i++;
            Command(
                new Command<int?>(executeForNullableValueType),
                ref i,
                true,
                Tuple.Create(null as object, true),
                Tuple.Create(1 as object, true),
                Tuple.Create("one" as object, false));
            Command(
                new Command<int?>(executeForNullableValueType, arg => true),
                ref i,
                true,
                Tuple.Create(null as object, true),
                Tuple.Create(1 as object, true),
                Tuple.Create("one" as object, false));
            Command(
                new Command<int?>(executeForNullableValueType, arg => false),
                ref i,
                false,
                Tuple.Create(null as object, false),
                Tuple.Create(1 as object, false),
                Tuple.Create("one" as object, false));

            Action<string> executeForReferenceType = arg => i++;
            Command(
                new Command<string>(executeForReferenceType),
                ref i,
                true,
                Tuple.Create(null as object, true),
                Tuple.Create(1 as object, false),
                Tuple.Create("one" as object, true));
            Command(
                new Command<string>(executeForReferenceType, arg => true),
                ref i,
                true,
                Tuple.Create(null as object, true),
                Tuple.Create(1 as object, false),
                Tuple.Create("one" as object, true));
            Command(
                new Command<string>(executeForReferenceType, arg => false),
                ref i,
                false,
                Tuple.Create(null as object, false),
                Tuple.Create(1 as object, false),
                Tuple.Create("one" as object, false));
        }
    }
}