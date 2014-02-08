using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;

namespace SimpleSpeedTester.Example
{
    class Program
    {        
        static void Main()
        {
            Example1();

            Example2();

            Example3();

            Example4();

            Console.WriteLine("all done...");
            Console.ReadKey();
        }

        private static void Example1()
        {
            // initialize a new test group
            var testGroup = new TestGroup("Example1");

            // create a test (in the form of an Action delegate) but don't run it yet
            // when executed, the test will invoke the Action delegate 5 times and time
            // how long it took each time
            var test = testGroup.Plan("Test1", () => { }, 5);

            // calling GetResult() executes the test
            var testResult = test.GetResult();

            // the outcome (how long did it take, was an unhandled exception thrown?) 
            // for each test run is tracked
            var testOutcomes = testResult.Outcomes;

            // let's take a look at the TestOutcome object
            var firstTestOutcome = testOutcomes.First();
            var elapsed = firstTestOutcome.Elapsed;         // a TimeSpan object
            var exception = firstTestOutcome.Exception;     // null if no exception

            // you can use the TestResult object to analyse your test, but it's often
            // useful to get a summary
            var testResultSummary = testResult.GetSummary();

            Console.WriteLine(testResultSummary);
            /* prints out something along the line of
             * 
             * Test Group   [Example1], Test [Test1] results summary:
             * Successes    [5]
             * Failures     [0] 
             * Average Exec Time [...] milliseconds
             * 
             */
        }

        private static void Example2()
        {
            // initialize a new test group
            var testGroup = new TestGroup("Example2");

            // PlanAndExecute actually executes the Action delegate 5 times and returns
            // the result summary
            var testResultSummary = testGroup.PlanAndExecute("Test1", () => { }, 5);

            Console.WriteLine(testResultSummary);
            /* prints out something along the line of
             * 
             * Test Group   [Example2], Test [Test1] results summary:
             * Successes    [5]
             * Failures     [0] 
             * Average Exec Time [...] milliseconds
             * 
             */
        }

        private static void Example3()
        {
            // initialize a new test group
            var testGroup = new TestGroup("Example3");

            var randomGenerator = new Random(DateTime.UtcNow.Millisecond);
            
            // you can also plan a test that runs against some piece of data, it provides
            // you with a way to track some arbitrary metric as you run your tests
            var numbers = new List<int>();
            var testAction = testGroup.Plan(
                "Test1", lst => lst.Add(randomGenerator.Next(100)), numbers, 5);

            // when executed, this test will add 5 random numbers between 0 and 99 to the
            // 'numbers' list
            testAction.GetResult();
            
            // this will print the 5 random number, e.g. 15, 7, 4, 9, 38
            Console.WriteLine(string.Join(",", numbers.Select(n => n.ToString())));
        }

        private static void Example4()
        {
            // initialize a new test group
            var testGroup = new TestGroup("Example4");

            var sleepIntervals = new[] { 2, 3, 4, 5, 11 };
            var index = 0;

            // plans a test which puts the thread to sleep for 2, 3, 4, 5 and then 11 seconds, zzz....
            var test = testGroup.Plan("Test1", () => Thread.Sleep(TimeSpan.FromSeconds(sleepIntervals[index++])), 5);

            // execute the test runs and get the result
            var testResult = test.GetResult();

            // summaries the result whilst considering all the test outcomes
            var resultSummaryDefault = testResult.GetSummary();

            // alternatively, provide a filter so that when the average execution time is calculated,  the min
            // and max times are not considered
            var resultSummaryExcludeMinAndMax = testResult.GetSummary(new ExcludeMinAndMaxTestOutcomeFilter());

            Console.WriteLine(resultSummaryDefault);
            /* prints out something along the line of:
             * 
             * Test Group [Example4], Test [Test1] results summary:
             * Successes  [5]
             * Failures   [0] 
             * Average Exec Time [5000.05438] milliseconds
             * 
             */

            Console.WriteLine(resultSummaryExcludeMinAndMax);
            /* prints out something along the line of:
             * 
             * Test Group [Example4], Test [Test1] results summary:
             * Successes  [5]
             * Failures   [0] 
             * Average Exec Time [4000.1766] milliseconds
             * 
             */
        }
    }
}