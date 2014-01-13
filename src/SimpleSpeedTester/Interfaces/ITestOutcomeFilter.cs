using System.Collections.Generic;
using SimpleSpeedTester.Core;

namespace SimpleSpeedTester.Interfaces
{
    /// <summary>
    /// Represents a filter for a set of outcomes from test runs
    /// </summary>
    public interface ITestOutcomeFilter
    {
        /// <summary>
        /// Filters the result outcomes to include only the outcomes that are of interest
        /// </summary>
        List<TestOutcome> Filter(List<TestOutcome> resultOutcomes);
    }
}