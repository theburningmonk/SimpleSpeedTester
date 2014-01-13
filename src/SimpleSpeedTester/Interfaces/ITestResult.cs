using System;
using System.Collections.Generic;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.Events;

namespace SimpleSpeedTester.Interfaces
{
    /// <summary>
    /// Represents the result of a test
    /// </summary>
    public interface ITestResult
    {
        /// <summary>
        /// Event for when a new test result summary becomes available
        /// </summary>
        event EventHandler<NewTestResultSummaryEventArgs> OnNewTestResultSummary;

        /// <summary>
        /// Outcome for the individual test runs
        /// </summary>
        List<TestOutcome> Outcomes { get; }

        /// <summary>
        /// The test this result corresponds to
        /// </summary>
        ITest Test { get; }

        /// <summary>
        /// Gets a summary for this result using the default outcome filter
        /// </summary>
        ITestResultSummary GetSummary();

        /// <summary>
        /// Gets a summary for this result based on the supplied outcome filter
        /// </summary>
        ITestResultSummary GetSummary(ITestOutcomeFilter outcomeFilter);        
    }
}