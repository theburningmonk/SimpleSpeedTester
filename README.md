# Simple Speed Tester

**Simple Speed Tester** is a simple, easy to use framework that helps you speed test your .Net code by taking care of some of the orchestration for you.

It should **NOT** be confused with a performance profiler such as [JetBrains' dotTrace](http://www.jetbrains.com/profiler/) or [RedGate's ANTS profiler](http://www.red-gate.com/products/dotnet-development/ants-performance-profiler/).

The **Simple Speed Tester** is intended for one thing and one thing only – help you speed test a specific piece of code/method over multiple runs, collate the results and work out the average for you so you only have to focus on producing the code you want to test.

Where it might be useful is when you want to compare the performance of two similar components/methods in terms of speed, for instance, the serialization and deserialization speed of [DataContractJsonSerializer](http://msdn.microsoft.com/en-us/library/system.runtime.serialization.json.datacontractjsonserializer.aspx) vs [JavaScriptSerializer](http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer.aspx).

### Getting Started

A detailed example program is included in the source code, but in the most basic case all you need is one line of code to execute a test (in the form of an Action delegate) a number of times and get back a summary including the number of successful (no exception) and failed (excepted) runs as well as the average time (in milliseconds) each test run took.

    // initialize a new test group
    var testGroup = new TestGroup("Example2");

    // PlanAndExecute actually executes the Action delegate 5 times and returns the result summary
    var testResultSummary = testGroup.PlanAndExecute("Test1", () => ExecuteTest(), 5);

    Console.WriteLine(testResultSummary);
    /* prints out something along the line of
     *
     * Test Group [Example2], Test [Test1] results summary:
     * Successes   [5]
     * Failures    [0] 
     * Average Exec Time [...] milliseconds
     *
     */

For more examples, check out the [Documentation] (https://github.com/theburningmonk/SimpleSpeedTester/wiki/Examples) page.

### NuGet

Download and install **Simple Speed Tester** using [NuGet](http://nuget.org/packages/SimpleSpeedTester).

<a href="http://nuget.org/packages/SimpleSpeedTester"><img src="http://theburningmonk.com/images/sst-nuget-install.png" alt="NuGet package"/></a>