using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimlpeSpeedTester.Example;

namespace JsonSerializersBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // more in-depth test of JSON serializers
            JsonSerializersSpeedTest.Start();            

            Console.WriteLine("all done...");
            Console.ReadKey();
        }
    }
}
