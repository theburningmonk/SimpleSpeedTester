using System;

using SimlpeSpeedTester.Example;

namespace JsonSerializersBenchmark
{
    class Program
    {
        static void Main()
        {
            // more in-depth test of JSON serializers
            JsonSerializersSpeedTest.Run();            

            Console.WriteLine("all done...");
            Console.ReadKey();
        }
    }
}
