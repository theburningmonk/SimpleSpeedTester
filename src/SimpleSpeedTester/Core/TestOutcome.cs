using System;

namespace SimpleSpeedTester.Core
{
    /// <summary>
    /// The outcome of executing a test
    /// </summary>
    public struct TestOutcome
    {
        internal TestOutcome(TimeSpan elapsed, Exception exception) 
            : this()
        {
            Elapsed = elapsed;
            Exception = exception;
        }

        /// <summary>
        /// Time taken to execute the test
        /// </summary>
        public TimeSpan Elapsed { get; private set; }

        /// <summary>
        /// The exception (if any) that is thrown by the test
        /// </summary>
        public Exception Exception { get; private set; }
    }
}