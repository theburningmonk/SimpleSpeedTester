using System;
using System.Threading;
using SimpleSpeedTester.Core;

namespace SimpleSpeedTester.Tests
{
    public abstract class BaseTest
    {
        protected const string TestGroupName = "MyTestGroup";
        protected const string TestName = "MyTestAction";

        protected static readonly Action DoNothingAction = () => { };
        protected static readonly Action SleepForJustOverOneSecondAction = () => Thread.Sleep(TimeSpan.FromSeconds(1.1));
        protected static readonly Action ExceptAction = () => { throw new ApplicationException(); };

        protected virtual TestGroup GetTestGroup(string testGroupName = TestGroupName)
        {
            return new TestGroup(testGroupName);
        }

        protected virtual Test GetTest(Action action, int count, string testName = TestName, TestGroup testGroup = null)
        {
            return new Test(testName, action, count, testGroup ?? GetTestGroup());
        }
    }
}
