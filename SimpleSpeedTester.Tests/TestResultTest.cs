using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;

namespace SimpleSpeedTester.Tests
{
    [TestFixture]
    public class TestResultTest : BaseTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullTestAction()
        {
            new TestResult(null, new List<TestOutcome>());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullTestActionOutcomes()
        {
            new TestResult(GetTest(DoNothingAction, 1), null);
        }

        [Test]
        public void TestGetSummaryWithDefaultOutcomeFilter()
        {
            // mock up a bunch of fake execution times
            var outcomeSeconds = new[] { 1, 5, 6, 7, 21 };
            var outcomes = outcomeSeconds.Select(sec => new TestOutcome(TimeSpan.FromSeconds(sec), null)).ToList();

            var testActionResult = new TestResult(GetTest(DoNothingAction, 5), outcomes);
            var summary = testActionResult.GetSummary();

            Assert.AreEqual(5, summary.Successes);
            Assert.AreEqual(0, summary.Failures);
            Assert.AreEqual(TimeSpan.FromSeconds(8).TotalMilliseconds, summary.AverageExecutionTime);
            Assert.AreEqual(testActionResult, summary.TestResult);
        }

        [Test]
        public void TestGetSummaryWithExcludeMinAndMaxOutcomeFilter()
        {
            // mock up a bunch of fake execution times
            var outcomeSeconds = new[] { 1, 5, 6, 7, 11 };
            var outcomes = outcomeSeconds.Select(sec => new TestOutcome(TimeSpan.FromSeconds(sec), null)).ToList();

            var testActionResult = new TestResult(GetTest(DoNothingAction, 5), outcomes);
            var summary = testActionResult.GetSummary(new ExcludeMinAndMaxTestOutcomeFilter());

            Assert.AreEqual(5, summary.Successes);
            Assert.AreEqual(0, summary.Failures);
            Assert.AreEqual(TimeSpan.FromSeconds(6).TotalMilliseconds, summary.AverageExecutionTime);
            Assert.AreEqual(testActionResult, summary.TestResult);
        }
    }
}
