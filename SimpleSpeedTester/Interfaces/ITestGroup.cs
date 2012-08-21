using System;
using System.Collections.Generic;
using SimpleSpeedTester.Core.Events;

namespace SimpleSpeedTester.Interfaces
{
    /// <summary>
    /// Represents a test group with a name that can be used to identify it
    /// </summary>
    public interface ITestGroup
    {
        /// <summary>
        /// Event for when a new test outcome becomes available
        /// </summary>
        event EventHandler<NewTestOutcomeEventArgs> OnNewTestOutcome;

        /// <summary>
        /// Event for when a new test result becomes available
        /// </summary>
        event EventHandler<NewTestResultEventArgs> OnNewTestResult;

        /// <summary>
        /// Event for when a new test result summary becomes available
        /// </summary>
        event EventHandler<NewTestResultSummaryEventArgs> OnNewTestResultSummary;

        /// <summary>
        /// Name of this test group
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plans the execution of an action delegate for a number of times.
        /// </summary>
        ITest Plan(string actionName, Action action, int count);

        /// <summary>
        /// Plans the execution of an action delegate against the specified data for a number of times.
        /// </summary>
        ITest Plan<T>(string actionName, Action<T> action, T data, int count);        

        /// <summary>
        /// Plans and executes a test for a number of times and returns the test result summary
        /// </summary>
        ITestResultSummary PlanAndExecute(string actionName, Action action, int count);

        /// <summary>
        /// Plans and executes a test for a number of times and returns the test result summary using the specified outcome filter
        /// </summary>
        ITestResultSummary PlanAndExecute(string actionName, Action action, int count, ITestOutcomeFilter outcomeFilter);

        /// <summary>
        /// Plans and executes a test against the specified data for a number of times
        /// </summary>
        ITestResultSummary PlanAndExecute<T>(string actionName, Action<T> action, T data, int count);

        /// <summary>
        /// Plans and executes a test against the specified data for a number of times and returns the test result summary using the 
        /// specified outcome filter
        /// </summary>
        ITestResultSummary PlanAndExecute<T>(string actionName, Action<T> action, T data, int count, ITestOutcomeFilter outcomeFilter);

        /// <summary>
        /// Returns the tests that have been planned thus far
        /// </summary>
        List<ITest> GetPlannedTests();

        /// <summary>
        /// Returns the test results that we have so far
        /// </summary>
        List<ITestResult> GetTestResults();
    }
}