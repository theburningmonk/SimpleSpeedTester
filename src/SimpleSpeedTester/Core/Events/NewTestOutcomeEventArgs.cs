using System;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core.Events
{
    public sealed class NewTestOutcomeEventArgs : EventArgs
    {
        public NewTestOutcomeEventArgs(ITest test, TestOutcome outcome)
        {
            Test = test;
            Outcome = outcome;
        }

        public ITest Test { get; private set; }

        public TestOutcome Outcome { get; private set; }
    }
}
