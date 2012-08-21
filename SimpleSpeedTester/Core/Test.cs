using System;
using System.Diagnostics;
using System.Linq;
using SimpleSpeedTester.Core.Events;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core
{
    /// <summary>
    /// Represents a test to be executed
    /// </summary>
    public sealed class Test : ITest
    {        
        internal Test(string name, Action action, int count, ITestGroup testGroup)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Test name cannot be null or empty", "name");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action", "Action delegate cannot be null");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException("count", "Execution count must be at least 1");
            }

            if (testGroup == null)
            {
                throw new ArgumentNullException("testGroup", "Test group cannot be null");
            }

            Name = name;
            Action = action;
            Count = count;
            TestGroup = testGroup;
        }

        public event EventHandler<NewTestOutcomeEventArgs> OnNewTestOutcome = (s, e) => { };

        public event EventHandler<NewTestResultEventArgs> OnNewTestResult = (s, e) => { };

        public event EventHandler<NewTestResultSummaryEventArgs> OnNewTestResultSummary = (s, e) => { };

        /// <summary>
        /// A descriptive name for the test
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The action to execute
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// How many times it should be executed
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The test group this test action is part of
        /// </summary>
        public ITestGroup TestGroup { get; private set; }

        /// <summary>
        /// Executes the action delegate as many time as necessary and returns the result
        /// </summary>
        public ITestResult GetResult()
        {
            // get all the test outcomes for all the test runs
            var outcomes = Enumerable.Range(1, Count).Select(i => Execute()).ToList();

            // compose the test result
            var result = new TestResult(this, outcomes);

            // attach event handler for when a new result summary becomes available
            result.OnNewTestResultSummary += OnNewTestResultSummaryHanlder;

            // and fire an event to notify others that a new test result is available
            OnNewTestResult(this, new NewTestResultEventArgs(this, result));

            // return the test result
            return result;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Executes the specified action delegate once and time how long it took whilst
        /// keeping track of any exception that might have been thrown
        /// </summary>
        internal TestOutcome Execute()
        {
            // initialize with null, so if no exception is caught then it stays as null
            Exception caughtException = null;

            // initialize and start the stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                Action();
            }
            catch (Exception ex)
            {
                // if exception is thrown, then remember what exception it was
                caughtException = ex;
            }

            // stop the stop watch and return the result of the timed execution
            stopwatch.Stop();

            var outcome = new TestOutcome(stopwatch.Elapsed, caughtException);

            // fire event to notify others a new test outcome is available
            OnNewTestOutcome(this, new NewTestOutcomeEventArgs(this, outcome));

            return outcome;
        }

        private void OnNewTestResultSummaryHanlder(object sender, NewTestResultSummaryEventArgs eventArgs)
        {
            // relay the event
            OnNewTestResultSummary(this, eventArgs);
        }
    }
}
