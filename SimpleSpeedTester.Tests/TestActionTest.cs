using System;
using NUnit.Framework;
using SimpleSpeedTester.Core;

namespace SimpleSpeedTester.Tests
{
    [TestFixture]
    public class TestActionTest : BaseTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNullActionName()
        {
            new Test(null, DoNothingAction, 1, GetTestGroup());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEmptyActionName()
        {
            new Test(string.Empty, DoNothingAction, 1, GetTestGroup());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullActionDelegate()
        {
            new Test(TestName, null, 1, GetTestGroup());
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInvalidExecCount()
        {
            new Test(TestName, DoNothingAction, 0, GetTestGroup());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullTestGroup()
        {
            new Test(TestName, DoNothingAction, 1, null);
        }

        [Test]
        public void TestExecuteWithNoException()
        {
            var testAction = new Test(TestName, SleepForJustOverOneSecondAction, 1, GetTestGroup());

            var start = DateTime.UtcNow;

            var outcome = testAction.Execute();

            var end = DateTime.UtcNow;
            var elapsed = end - start;

            // make sure the recorded and actual elapsed time are both just over 1 second
            Assert.IsTrue(elapsed >= TimeSpan.FromSeconds(1));
            Assert.IsTrue(outcome.Elapsed >= TimeSpan.FromSeconds(1));

            // make sure no exception was thrown
            Assert.IsNull(outcome.Exception);
        }

        [Test]
        public void TestExecuteWithException()
        {
            var testAction = GetTest(ExceptAction, 1);               

            var start = DateTime.UtcNow;

            var outcome = testAction.Execute();

            var end = DateTime.UtcNow;
            var elapsed = end - start;

            // make sure the recorded and actual elapsed time are both less than 1 second
            Assert.IsTrue(elapsed.TotalSeconds < 1);
            Assert.IsTrue(outcome.Elapsed.TotalSeconds < 1);

            Assert.IsNotNull(outcome.Exception);
            Assert.IsInstanceOf(typeof(ApplicationException), outcome.Exception);
        }

        [Test]
        public void TestGetResult()
        {
            var testAction = GetTest(DoNothingAction, 10);

            var result = testAction.GetResult();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Outcomes);
            Assert.AreEqual(10, result.Outcomes.Count);
            Assert.AreEqual(testAction, result.Test);
        }
    }
}
