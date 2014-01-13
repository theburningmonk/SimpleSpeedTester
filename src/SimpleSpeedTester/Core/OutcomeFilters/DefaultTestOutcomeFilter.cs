using System.Collections.Generic;
using System.Linq;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core.OutcomeFilters
{
    /// <summary>
    /// A filter which includes outcomes from all the test runs which did not except
    /// </summary>
    public sealed class DefaultTestOutcomeFilter : ITestOutcomeFilter
    {
        /// <summary>
        /// Filters out outcomes of test runs which did not except
        /// </summary>
        public List<TestOutcome> Filter(List<TestOutcome> resultOutcomes)
        {
            return resultOutcomes.Where(o => o.Exception == null).ToList();
        }
    }
}
