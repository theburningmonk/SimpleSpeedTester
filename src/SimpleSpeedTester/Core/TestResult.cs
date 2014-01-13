using System;
using System.Collections.Generic;
using SimpleSpeedTester.Core.Events;
using SimpleSpeedTester.Core.OutcomeFilters;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core
{
    /// <summary>
    /// Represents the result of a test
    /// </summary>
    public sealed class TestResult : ITestResult
    {
        internal TestResult(ITest test, List<TestOutcome> outcomes)
        {
            if (test == null)
            {
                throw new ArgumentNullException("test", "Test cannot be null");
            }

            if (outcomes == null)
            {
                throw new ArgumentNullException("outcomes", "Test outcomes cannot be null");
            }

            Outcomes = outcomes;
            Test = test;
        }

        public event EventHandler<NewTestResultSummaryEventArgs> OnNewTestResultSummary = (s, e) => { };

        /// <summary>
        /// Outcome for the individual test runs
        /// </summary>
        public List<TestOutcome> Outcomes { get; private set; }

        /// <summary>
        /// The test this result corresponds to
        /// </summary>
        public ITest Test { get; private set; }

        /// <summary>
        /// Gets a summary for this result using the default outcome filter
        /// </summary>
        public ITestResultSummary GetSummary()
        {
            return GetSummary(new DefaultTestOutcomeFilter());
        }

        /// <summary>
        /// Gets a summary for this result based on the supplied outcome filter
        /// </summary>
        public ITestResultSummary GetSummary(ITestOutcomeFilter outcomeFilter)
        {
            var summary = new TestResultSummary(this, outcomeFilter);

            // notify others that a new test summary is available
            OnNewTestResultSummary(this, new NewTestResultSummaryEventArgs(Test, this, outcomeFilter, summary));

            return summary;
        }
    }
}
