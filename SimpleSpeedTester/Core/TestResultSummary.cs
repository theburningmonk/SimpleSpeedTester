using System.Linq;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core
{
    /// <summary>
    /// Represents the summary of a test result
    /// </summary>
    public sealed class TestResultSummary : ITestResultSummary
    {
        internal TestResultSummary(TestResult testResult, ITestOutcomeFilter outcomeFilter)
        {
            TestResult = testResult;
            Successes = TestResult.Outcomes.Count(o => o.Exception == null);
            Failures = TestResult.Outcomes.Count() - Successes;

            var eligibleOutcomes = outcomeFilter.Filter(TestResult.Outcomes);

            if (eligibleOutcomes.Any())
            {
                AverageExecutionTime = eligibleOutcomes.Average(o => o.Elapsed.TotalMilliseconds);
            }            
        }        

        /// <summary>
        /// The number of test runs that finished without exception
        /// </summary>
        public int Successes { get; private set; }

        /// <summary>
        /// The number of test runs that excepted
        /// </summary>
        public int Failures { get; private set; }

        /// <summary>
        /// THe average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTime { get; private set; }

        /// <summary>
        /// The test result this summary corresponds to
        /// </summary>
        public TestResult TestResult { get; private set; }

        public override string ToString()
        {
            return string.Format(
@"Test Group [{0}], Test [{1}] results summary:
Successes   [{2}]
Failures    [{3}] 
Average Exec Time [{4}] milliseconds", 
                        TestResult.Test.TestGroup,
                        TestResult.Test,
                        Successes,
                        Failures,
                        AverageExecutionTime);
        }
    }
}
