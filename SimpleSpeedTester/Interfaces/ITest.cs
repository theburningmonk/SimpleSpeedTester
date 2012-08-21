using System;
using SimpleSpeedTester.Core.Events;

namespace SimpleSpeedTester.Interfaces
{
    /// <summary>
    /// Represents a test
    /// </summary>
    public interface ITest
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
        /// A descriptive name for the test
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The action to execute as part of the test
        /// </summary>
        Action Action { get; }

        /// <summary>
        /// How many times the test should be executed
        /// </summary>
        int Count { get; }

        /// <summary>
        /// The test group this test is part of
        /// </summary>
        ITestGroup TestGroup { get; }        

        /// <summary>
        /// Executes the action delegate as many time as necessary and returns a result for the test
        /// </summary>
        ITestResult GetResult();        
    }
}