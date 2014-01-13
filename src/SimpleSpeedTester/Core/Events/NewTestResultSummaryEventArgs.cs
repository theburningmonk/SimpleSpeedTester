using System;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core.Events
{
    public sealed class NewTestResultSummaryEventArgs : EventArgs
    {
        public NewTestResultSummaryEventArgs(ITest test, ITestResult result, ITestOutcomeFilter outcomeFilter, ITestResultSummary summary)
        {
            Test = test;
            Result = result;
            OutcomeFilter = outcomeFilter;
            Summary = summary;
        }

        public ITest Test { get; private set; }

        public ITestResult Result { get; private set; }

        public ITestOutcomeFilter OutcomeFilter { get; private set; }

        public ITestResultSummary Summary { get; private set; }
    }
}
