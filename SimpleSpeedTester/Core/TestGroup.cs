using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SimpleSpeedTester.Core.Events;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core
{
    /// <summary>
    /// Represents a test group with a name that can be used to identify it
    /// </summary>
    public class TestGroup : ITestGroup
    {        
        public TestGroup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Test group name cannot be null or empty", "name");
            }

            Name = name;
            PlannedActions = new ConcurrentDictionary<ITest, ITestResult>();
        }

        public event EventHandler<NewTestOutcomeEventArgs> OnNewTestOutcome = (s, e) => { };

        public event EventHandler<NewTestResultEventArgs> OnNewTestResult = (s, e) => { };

        public event EventHandler<NewTestResultSummaryEventArgs> OnNewTestResultSummary = (s, e) => { };

        /// <summary>
        /// Name of this test group
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The tests that have been planned
        /// </summary>
        private ConcurrentDictionary<ITest, ITestResult> PlannedActions { get; set; }

        /// <summary>
        /// Plans the execution of a test for a number of times
        /// </summary>
        public ITest Plan(string actionName, Action action, int count)
        {
            return AddPlannedTest(new Test(actionName, action, count, this));
        }

        /// <summary>
        /// Plans the execution of a test against the specified data for a number of times
        /// </summary>
        public ITest Plan<T>(string actionName, Action<T> action, T data, int count)
        {
            return AddPlannedTest(new Test(actionName, () => action(data), count, this));            
        }

        /// <summary>
        /// Plans and executes a test for a number of times and returns the test result summary
        /// </summary>
        public ITestResultSummary PlanAndExecute(string actionName, Action action, int count)
        {
            var test = Plan(actionName, action, count);
            return GetTestSummary(test);
        }

        /// <summary>
        /// Plans and executes a test for a number of times and returns the test result summary using the specified outcome filter
        /// </summary>
        public ITestResultSummary PlanAndExecute(string actionName, Action action, int count, ITestOutcomeFilter outcomeFilter)
        {
            var test = Plan(actionName, action, count);
            return GetTestSummary(test, outcomeFilter);
        }

        /// <summary>
        /// Plans and executes a test against the specified data for a number of times and returns the test result summary
        /// </summary>
        public ITestResultSummary PlanAndExecute<T>(string actionName, Action<T> action, T data, int count)
        {
            var test = Plan(actionName, action, data, count);
            return GetTestSummary(test);
        }

        /// <summary>
        /// Plans and executes a test against the specified data for a number of times and returns the test result summary using the 
        /// specified outcome filter
        /// </summary>
        public ITestResultSummary PlanAndExecute<T>(string actionName, Action<T> action, T data, int count, ITestOutcomeFilter outcomeFilter)
        {
            var test = Plan(actionName, action, data, count);
            return GetTestSummary(test, outcomeFilter);
        }

        /// <summary>
        /// Returns the tests that have been planned thus far
        /// </summary>
        public List<ITest> GetPlannedTests()
        {
            return PlannedActions.Keys.ToList();
        }

        /// <summary>
        /// Returns the test results that we have so far
        /// </summary>
        public List<ITestResult> GetTestResults()
        {
            // the first ToList() is to get a transient copy of the values from the concurrent dictionary
            // to avoid enumerable modified exception in case the source was updated whilst invoking
            // the where clause
            return PlannedActions.Values.ToList().Where(tr => tr != null).ToList();
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Executes the specified test straight away and returns a summary of the test result
        /// </summary>
        private static ITestResultSummary GetTestSummary(ITest test)
        {
            return test.GetResult().GetSummary();
        }

        /// <summary>
        /// Overload which uses the specified outcome filter to generate the summary
        /// </summary>
        private static ITestResultSummary GetTestSummary(ITest test, ITestOutcomeFilter outcomeFilter)
        {
            return test.GetResult().GetSummary(outcomeFilter);
        }

        /// <summary>
        /// Add a newly planned test to the bag of planned tests and returns it
        /// </summary>
        private ITest AddPlannedTest(ITest newTest)
        {
            PlannedActions.TryAdd(newTest, null);

            // attach event handlers
            newTest.OnNewTestOutcome += OnNewTestOutcomeHandle;
            newTest.OnNewTestResult += OnNewTestResultHandler;
            newTest.OnNewTestResultSummary += OnNewTestResultSummaryHandler;

            return newTest;
        }        

        #region Event Handlers

        private void OnNewTestOutcomeHandle(object sender, NewTestOutcomeEventArgs eventArgs)
        {
            //  relay the event
            OnNewTestOutcome(this, eventArgs);
        }

        private void OnNewTestResultHandler(object sender, NewTestResultEventArgs eventArgs)
        {
            // set the result associated with the test
            PlannedActions.AddOrUpdate(eventArgs.Test, eventArgs.Result, (t, tr) => eventArgs.Result);

            // relay the event
            OnNewTestResult(this, eventArgs);
        }

        private void OnNewTestResultSummaryHandler(object sender, NewTestResultSummaryEventArgs eventArgs)
        {
            // relay the event
            OnNewTestResultSummary(this, eventArgs);
        }

        #endregion
    }
}
