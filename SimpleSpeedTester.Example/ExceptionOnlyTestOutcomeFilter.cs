using System.Collections.Generic;
using System.Linq;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Interfaces;

namespace SimlpeSpeedTester.Example
{
    public sealed class ExceptionOnlyTestOutcomeFilter : ITestOutcomeFilter
    {
        public List<TestOutcome> Filter(List<TestOutcome> resultOutcomes)
        {
            return resultOutcomes.Where(o => o.Exception != null).ToList();
        }
    }
}
