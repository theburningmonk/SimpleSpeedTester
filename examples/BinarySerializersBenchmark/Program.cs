using System;

using SimlpeSpeedTester.Example;

namespace BinarySerializersBenchmark
{
    class Program
    {
        static void Main()
        {
            // more in-depth test of BinaryFormatter vs Protobuf-net
            BinarySerializersSpeedTest.Run();

            Console.WriteLine("all done...");
            Console.ReadKey();
        }
    }
}
