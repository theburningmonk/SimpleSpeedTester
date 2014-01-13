using SimpleSpeedTester.Core;

namespace SimpleSpeedTester.Interfaces
{
    /// <summary>
    /// Represents the summary of a test result
    /// </summary>
    public interface ITestResultSummary
    {
        /// <summary>
        /// The number of test runs that finished without exception
        /// </summary>
        int Successes { get; }

        /// <summary>
        /// The number of test runs that excepted
        /// </summary>
        int Failures { get; }

        /// <summary>
        /// THe average execution time in milliseconds
        /// </summary>
        double AverageExecutionTime { get; }

        /// <summary>
        /// The test result this summary corresponds to
        /// </summary>
        TestResult TestResult { get; }
    }
}