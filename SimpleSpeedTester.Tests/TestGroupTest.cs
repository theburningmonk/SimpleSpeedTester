using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;

namespace SimpleSpeedTester.Tests
{
    [TestFixture]
    public class TestGroupTest : BaseTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNullTestGroupName()
        {
            new TestGroup(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEmptyTestGroupName()
        {
            new TestGroup(string.Empty);
        }

        [Test]
        public void TestValidTestGroupName()
        {
            new TestGroup("MyTestGroup");
        }

        [Test]
        public void TestPlan()
        {
            int onNewTestOutcomeEvents = 0, 
                onNewTestResultEvents = 0, 
                onNewTestResultSummaryEvents = 0;

            var testGroup = GetTestGroup();
            testGroup.OnNewTestOutcome += (s, e) => onNewTestOutcomeEvents++;
            testGroup.OnNewTestResult += (s, e) => onNewTestResultEvents++;
            testGroup.OnNewTestResultSummary += (s, e) => onNewTestResultSummaryEvents++;

            var testAction = testGroup.Plan(TestName, DoNothingAction, 1);

            Assert.IsNotNull(testAction);
            Assert.AreEqual(TestName, testAction.Name);
            Assert.AreEqual(testGroup, testAction.TestGroup);            

            // no event should be fired before the test is executed
            Assert.AreEqual(0, onNewTestOutcomeEvents);
            Assert.AreEqual(0, onNewTestResultEvents);
            Assert.AreEqual(0, onNewTestResultSummaryEvents);
            
            var testResult = testAction.GetResult();

            // new test outcome and new test result have been fired
            Assert.AreEqual(1, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(0, onNewTestResultSummaryEvents);

            testResult.GetSummary();

            // new test result summary is fired as well
            Assert.AreEqual(1, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(1, onNewTestResultSummaryEvents);
        }

        [Test]
        public void TestPlanWithData()
        {
            int onNewTestOutcomeEvents = 0,
                onNewTestResultEvents = 0,
                onNewTestResultSummaryEvents = 0;

            var testGroup = GetTestGroup();
            testGroup.OnNewTestOutcome += (s, e) => onNewTestOutcomeEvents++;
            testGroup.OnNewTestResult += (s, e) => onNewTestResultEvents++;
            testGroup.OnNewTestResultSummary += (s, e) => onNewTestResultSummaryEvents++;

            var testAction = testGroup.Plan(TestName, i => Thread.Sleep(TimeSpan.FromSeconds(i)), 1.1, 3);

            // no event should have been fired yet
            Assert.AreEqual(0, onNewTestOutcomeEvents);
            Assert.AreEqual(0, onNewTestResultEvents);
            Assert.AreEqual(0, onNewTestResultSummaryEvents);

            var start = DateTime.UtcNow;
            var testResult = testAction.GetResult();
            var end = DateTime.UtcNow;

            Assert.IsTrue((end - start).TotalSeconds >= 3);
            Assert.AreEqual(3, testResult.Outcomes.Count);

            // events should have been fired for new test outcome and new test result
            Assert.AreEqual(3, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(0, onNewTestResultSummaryEvents);

            testResult.GetSummary();
            Assert.AreEqual(3, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(1, onNewTestResultSummaryEvents);
        }

        [Test]
        public void TestPlanAndExecuteWithDefaultOutcomeFilter()
        {
            int onNewTestOutcomeEvents = 0,
                onNewTestResultEvents = 0,
                onNewTestResultSummaryEvents = 0;

            var testGroup = GetTestGroup();
            testGroup.OnNewTestOutcome += (s, e) => onNewTestOutcomeEvents++;
            testGroup.OnNewTestResult += (s, e) => onNewTestResultEvents++;
            testGroup.OnNewTestResultSummary += (s, e) => onNewTestResultSummaryEvents++;

            var start = DateTime.UtcNow;
            var testSummary = testGroup.PlanAndExecute(TestName, SleepForJustOverOneSecondAction, 3);
            var end = DateTime.UtcNow;

            Assert.IsTrue((end - start).TotalSeconds >= 3);
            Assert.AreEqual(3, testSummary.Successes);
            Assert.AreEqual(0, testSummary.Failures);

            // we should end up with an average exec time of 1.1s
            Assert.IsTrue(testSummary.AverageExecutionTime >= TimeSpan.FromSeconds(1).TotalMilliseconds);
            Assert.IsTrue(testSummary.AverageExecutionTime < TimeSpan.FromSeconds(1.2).TotalMilliseconds);

            Assert.IsNotNull(testSummary.TestResult);
            Assert.IsNotNull(testSummary.TestResult.Test);
            Assert.AreEqual(testGroup, testSummary.TestResult.Test.TestGroup);

            Assert.AreEqual(3, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(1, onNewTestResultSummaryEvents);
        }

        [Test]
        public void TestPlanAndExecuteWithExcludeMinAndMaxOutcomeFilter()
        {
            int onNewTestOutcomeEvents = 0,
                onNewTestResultEvents = 0,
                onNewTestResultSummaryEvents = 0;

            var testGroup = GetTestGroup();
            testGroup.OnNewTestOutcome += (s, e) => onNewTestOutcomeEvents++;
            testGroup.OnNewTestResult += (s, e) => onNewTestResultEvents++;
            testGroup.OnNewTestResultSummary += (s, e) => onNewTestResultSummaryEvents++;

            var sleepTimes = new[] { 1.1, 2.1, 5.1 };
            var index = 0;

            var start = DateTime.UtcNow;
            var testSummary = testGroup.PlanAndExecute(
                TestName, 
                () => Thread.Sleep(TimeSpan.FromSeconds(sleepTimes[index++])), 
                3,
                new ExcludeMinAndMaxTestOutcomeFilter());
            var end = DateTime.UtcNow;

            Assert.IsTrue((end - start).TotalSeconds >= 8);
            Assert.AreEqual(3, testSummary.Successes);
            Assert.AreEqual(0, testSummary.Failures);
            
            // once the min and max exec times are ignored, we should end up with an average exec time
            // of 2.1s
            Assert.IsTrue(testSummary.AverageExecutionTime >= TimeSpan.FromSeconds(2).TotalMilliseconds);
            Assert.IsTrue(testSummary.AverageExecutionTime < TimeSpan.FromSeconds(2.2).TotalMilliseconds);

            Assert.IsNotNull(testSummary.TestResult);
            Assert.IsNotNull(testSummary.TestResult.Test);
            Assert.AreEqual(testGroup, testSummary.TestResult.Test.TestGroup);

            Assert.AreEqual(3, onNewTestOutcomeEvents);
            Assert.AreEqual(1, onNewTestResultEvents);
            Assert.AreEqual(1, onNewTestResultSummaryEvents);
        }

        [Test]
        public void TestGetPlannedTests()
        {
            var testGroup = GetTestGroup();
            var testAction = testGroup.Plan(TestName, DoNothingAction, 1);

            var plannedTests = testGroup.GetPlannedTests();
            Assert.IsNotNull(plannedTests);
            Assert.AreEqual(1, plannedTests.Count);
            Assert.AreEqual(testAction, plannedTests.First());

            testGroup.Plan(TestName, DoNothingAction, 1);
            plannedTests = testGroup.GetPlannedTests();
            Assert.IsNotNull(plannedTests);
            Assert.AreEqual(2, plannedTests.Count);
        }

        [Test]
        public void TestGetTestResults()
        {
            var testGroup = GetTestGroup();
            var testAction = testGroup.Plan(TestName, DoNothingAction, 10);

            // check that before test is executed, there's no test results
            var testResults = testGroup.GetTestResults();
            Assert.IsNotNull(testResults);
            Assert.AreEqual(0, testResults.Count);
            
            // execute the test and check that the result is available
            var testResult = testAction.GetResult();
            testResults = testGroup.GetTestResults();
            Assert.IsNotNull(testResults);
            Assert.AreEqual(1, testResults.Count);
            Assert.AreEqual(testResult, testResults.First());
        }
    }
}
