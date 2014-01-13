using System;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core.Events
{
    public sealed class NewTestResultEventArgs : EventArgs
    {
        public NewTestResultEventArgs(ITest test, ITestResult result)
        {
            Test = test;
            Result = result;
        }

        public ITest Test { get; private set; }

        public ITestResult Result { get; private set; }
    }
}
