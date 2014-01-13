using System.Collections.Generic;
using System.Linq;
using SimpleSpeedTester.Interfaces;

namespace SimpleSpeedTester.Core.OutcomeFilters
{
    /// <summary>
    /// A filter which includes outcomes from the test runs which did not except, excluding
    /// the outcomes with the min and max execution time
    /// </summary>
    public sealed class ExcludeMinAndMaxTestOutcomeFilter : ITestOutcomeFilter
    {
        public List<TestOutcome> Filter(List<TestOutcome> resultOutcomes)
        {
            // get all the successful outcomes, ordered by by the execution time
            var orderedSuccessOutcome = resultOutcomes
                                        .Where(o => o.Exception == null)
                                        .OrderBy(o => o.Elapsed)
                                        .ToList();

            // work out how many results we have
            var count = orderedSuccessOutcome.Count();

            // if there are more than 2 results, then ignore the top and bottom result
            // if there are 2 or less results, then just return everything
            return count > 2 ? orderedSuccessOutcome.Skip(1).Take(count - 2).ToList() : orderedSuccessOutcome;
        }
    }
}
