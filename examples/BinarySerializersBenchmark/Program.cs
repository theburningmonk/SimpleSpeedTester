using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimlpeSpeedTester.Example;

namespace BinarySerializersBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // more in-depth test of BinaryFormatter vs Protobuf-net
            BinarySerializersSpeedTest.Start();

            Console.WriteLine("all done...");
            Console.ReadKey();
        }
    }
}
