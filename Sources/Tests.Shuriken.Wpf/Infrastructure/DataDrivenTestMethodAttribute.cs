using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Shuriken.Wpf.Infrastructure
{
    [ExcludeFromCodeCoverage]
    internal sealed class DataDrivenTestMethodAttribute : TestMethodAttribute
    {
        sealed class TestResultException : Exception
        {
            public TestResultException(Exception testFailureException)
                => TestResult = new TestResult { TestFailureException = testFailureException };

            public TestResult TestResult { get; }
        }

        public DataDrivenTestMethodAttribute(string methodName) => MethodName = methodName;

        public string MethodName { get; }

        public Type? GenericArgument { get; set; }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            IEnumerable<object?[]> source;
            try
            {
                source = GetSource(testMethod.MethodInfo!.DeclaringType!);
            }
            catch (TestResultException e)
            {
                return new[] { e.TestResult };
            }

            return ExecuteDataDrivenTests(testMethod, source);
        }

        /// <exception cref="TestResultException">Data source method execution failed.</exception>
        IEnumerable<object?[]> GetSource(Type testType)
        {
            var method =
                testType.GetMethod(
                    MethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Array.Empty<Type>(),
                    Array.Empty<ParameterModifier>()) ??
                    throw new InvalidOperationException($"Cannot find a static parameterless method '{MethodName}'.");

            if (GenericArgument != null)
            {
                method = method.MakeGenericMethod(GenericArgument);
            }

            IEnumerable? source;
            try
            {
                source = method.Invoke(null, Array.Empty<object>()) as IEnumerable;
            }
            catch (TargetInvocationException e)
            {
                throw new TestResultException(e.InnerException ?? e);
            }

            if (source == null)
            {
                throw new InvalidOperationException($"The '{MethodName}' method does not return an {nameof(IEnumerable)}.");
            }

            return from object value in source select new[] { value };
        }

        static TestResult[] ExecuteDataDrivenTests(ITestMethod testMethod, IEnumerable<object?[]> source)
        {
            var testResults = new List<TestResult>();

            try
            {
                foreach (var arguments in source)
                {
                    var testResult = testMethod.Invoke(arguments);

                    string displayArguments;
                    try
                    {
                        // try to create a string
                        displayArguments = string.Join(", ", arguments); // the "ToString" methods can throw exceptions

                        // check if the string ts XML compatible (so the build server can report the test results)
                        using var writer = new StringWriter(CultureInfo.InvariantCulture);

                        using var innerWriter = XmlWriter.Create(writer, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });

                        innerWriter.WriteString(displayArguments); // the "WriteString" method can throw exceptions
                    }
                    catch
                    {
                        displayArguments = $"[{arguments.Length}]";
                    }
                    testResult!.DisplayName = $"{testMethod.TestMethodName} ({displayArguments})";

                    testResults.Add(testResult);
                }
            }
            catch (Exception e)
            {
                testResults.Add(new TestResult { DisplayName = testMethod.TestMethodName, TestFailureException = e });
            }

            foreach (var testResult in testResults)
            {
                const int maxLength = 449; // reported by the test system

                if (testResult.DisplayName!.Length > maxLength)
                {
                    testResult.DisplayName = testResult.DisplayName.Substring(0, maxLength - 3) + "...";
                }
            }

            return testResults.ToArray();
        }
    }
}