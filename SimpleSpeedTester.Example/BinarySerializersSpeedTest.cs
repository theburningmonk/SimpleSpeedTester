using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using MessageShark;
using MsgPack;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;
using SimpleSpeedTester.Interfaces;

namespace SimlpeSpeedTester.Example
{
    public static class BinarySerializersSpeedTest
    {
        // test serialization and deserialization on 100k objects
        private const int ObjectsCount = 100000;

        // perform 5 runs of each test
        private const int TestRuns = 5;

        // exclude the min and max results 
        private static readonly ITestOutcomeFilter OutcomeFilter = new ExcludeMinAndMaxTestOutcomeFilter();

        // the objects to perform the tests with
        private static readonly List<SimpleObject> SimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObject).ToList();
        private static readonly List<SimpleObjectWithFields> SimpleObjectsWithFields = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectWithFields).ToList();
        private static readonly List<IserializableSimpleObject> IserializableSimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSerializableSimpleObject).ToList();

        public static void Start()
        {
            // speed test binary formatter
            DoSpeedTest("BinaryFormatter (with properties)", SimpleObjects, SerializeWithBinaryFormatter, DeserializeWithBinaryFormatter<SimpleObject>);

            DoSpeedTest("BinaryFormatter (with fields)", SimpleObjectsWithFields, SerializeWithBinaryFormatter, DeserializeWithBinaryFormatter<SimpleObjectWithFields>);

            // speed test binary formatter when used with an ISerializable type
            DoSpeedTest("BinaryFormatterWithISerializable", IserializableSimpleObjects, SerializeWithBinaryFormatter, DeserializeWithBinaryFormatter<IserializableSimpleObject>);

            // speed test protobuf-net
            DoSpeedTest("Protobuf-Net (with properties)", SimpleObjects, SerializeWithProtobufNet, DeserializeWithProtobufNet<SimpleObject>);

            DoSpeedTest("Protobuf-Net (with fields)", SimpleObjectsWithFields, SerializeWithProtobufNet, DeserializeWithProtobufNet<SimpleObjectWithFields>);

            // speed test binary writer (only for reference, won't be able to deserialize)
            DoSpeedTest("BinaryWriter", SimpleObjects, SerializeWithBinaryWriter, null);

            // speed test message pack
            DoSpeedTest(
                "MessagePack (with properties)", 
                SimpleObjects, 
                lst => SerializeWithMessagePack(lst, true), 
                lst => DeserializeWithMessagePack<SimpleObject>(lst, true));

            // speed test message pack again
            DoSpeedTest(
                "MessagePack (with fields)",
                SimpleObjectsWithFields, 
                lst => SerializeWithMessagePack(lst, false), 
                lst => DeserializeWithMessagePack<SimpleObjectWithFields>(lst, false));

            // speed test message pack again
            DoSpeedTest(
                "MessageShark (with properties)",
                SimpleObjects,
                SerializeWithMessageShark, 
                DeserializeWithMessageShark<SimpleObject>);
        }

        private static void DoSpeedTest<T>(
            string testGroupName, List<T> objects, Func<List<T>, List<byte[]>> serializeFunc, Func<List<byte[]>, List<T>> deserializeFunc)
        {
            var byteArrays = new List<byte[]>();

            var testGroup = new TestGroup(testGroupName);

            var serializationTestSummary =
                testGroup
                    .Plan("Serialization", () => byteArrays = serializeFunc(objects), TestRuns)
                    .GetResult()
                    .GetSummary(OutcomeFilter);            

            Console.WriteLine(serializationTestSummary);

            Console.WriteLine("Test Group [{0}] average serialized byte array size is [{1}]", testGroupName, byteArrays.Average(arr => arr.Length));

            var clones = new List<T>();

            if (deserializeFunc != null)
            {
                var deserializationTestSummary =
                    testGroup
                        .Plan("Deserialization", () => clones = deserializeFunc(byteArrays), TestRuns)
                        .GetResult()
                        .GetSummary(OutcomeFilter);

                Console.WriteLine(deserializationTestSummary);
            }

            Console.WriteLine("--------------------------------------------------------");
        }

        private static SimpleObject GetSimpleObject(int id)
        {
            return new SimpleObject
                       {
                           Name = "Simple",
                           Id = 100000,
                           Address = "Planet Earth",
                           Scores = Enumerable.Range(0, 10).ToArray()
                       };
        }

        private static SimpleObjectWithFields GetSimpleObjectWithFields(int id)
        {
            return new SimpleObjectWithFields
            {
                Name = "Simple",
                Id = 100000,
                Address = "Planet Earth",
                Scores = Enumerable.Range(0, 10).ToArray()
            };
        }

        private static IserializableSimpleObject GetSerializableSimpleObject(int id)
        {
            return new IserializableSimpleObject
                       {
                           Name = "Simple",
                           Id = 100000,
                           Address = "Planet Earth",
                           Scores = Enumerable.Range(0, 10).ToArray()
                       };
        }

        #region Binary Formatter

        private static List<byte[]> SerializeWithBinaryFormatter<T>(List<T> objects)
        {
            var binaryFormatter = new BinaryFormatter();
            return objects.Select(o => SerializeWithBinaryFormatter(binaryFormatter, o)).ToList();
        }

        private static byte[] SerializeWithBinaryFormatter<T>(BinaryFormatter binaryFormatter, T obj)
        {
            using (var memStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memStream, obj);
                return memStream.ToArray();
            }
        }

        private static List<T> DeserializeWithBinaryFormatter<T>(List<byte[]> byteArrays)
        {
            var binaryFormatter = new BinaryFormatter();
            return byteArrays.Select(arr => DeserializeWithBinaryFormatter<T>(binaryFormatter, arr)).ToList();
        }

        private static T DeserializeWithBinaryFormatter<T>(BinaryFormatter binaryFormatter, byte[] byteArray)
        {
            using (var memStream = new MemoryStream(byteArray))
            {
                return (T)binaryFormatter.Deserialize(memStream);
            }
        }

        #endregion
        
        #region Protobuf-Net

        private static List<byte[]> SerializeWithProtobufNet<T>(List<T> objects)
        {
            return objects.Select(SerializeWithProtobufNet).ToList();
        }

        private static byte[] SerializeWithProtobufNet<T>(T obj)
        {
            using (var memStream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(memStream, obj);
                return memStream.ToArray();
            }
        }

        private static List<T> DeserializeWithProtobufNet<T>(List<byte[]> byteArrays)
        {
            return byteArrays.Select(DeserializeWithProtobufNet<T>).ToList();
        }

        private static T DeserializeWithProtobufNet<T>(byte[] byteArray)
        {
            using (var memStream = new MemoryStream(byteArray))
            {
                return ProtoBuf.Serializer.Deserialize<T>(memStream);
            }
        }

        #endregion        
        
        #region Binary Writer

        private static List<byte[]> SerializeWithBinaryWriter(List<SimpleObject> objects)
        {
            return objects.Select(SerializeWithBinaryWriter).ToList();
        }

        private static byte[] SerializeWithBinaryWriter(SimpleObject obj)
        {
            using (var memStream = new MemoryStream())
            {
                var binaryWriter = new BinaryWriter(memStream);

                binaryWriter.Write(obj.Id);
                binaryWriter.Write(obj.Name);
                binaryWriter.Write(obj.Address);
                Array.ForEach(obj.Scores, binaryWriter.Write);
                binaryWriter.Flush();

                return memStream.ToArray();
            }
        }

        #endregion        

        #region MessagePack

        private static List<byte[]> SerializeWithMessagePack<T>(List<T> objects, bool allFields)
        {
            var packer = new CompiledPacker(allFields);
            return objects.Select(packer.Pack).ToList();
        }

        private static List<T> DeserializeWithMessagePack<T>(List<byte[]> byteArrays, bool allFields) 
        {
            var packer = new CompiledPacker(allFields);
            return byteArrays.Select(packer.Unpack<T>).ToList();
        }

        #endregion

        #region MessageShark

        private static List<byte[]> SerializeWithMessageShark<T>(List<T> objects)
            where T : class 
        {
            return objects.Select(MessageSharkSerializer.Serialize).ToList();
        }

        private static List<T> DeserializeWithMessageShark<T>(List<byte[]> byteArrays)
            where T : class
        {
            return byteArrays.Select(MessageSharkSerializer.Deserialize<T>).ToList();
        }

        #endregion

        #region Serialization Objects

        [Serializable]
        [DataContract]
        public class SimpleObjectWithFields
        {
            [DataMember(Order = 1)]
            public int Id;

            [DataMember(Order = 2)]
            public string Name;

            [DataMember(Order = 3)]
            public string Address;

            [DataMember(Order = 4)]
            public int[] Scores;
        }

        [Serializable]
        [DataContract]
        public class SimpleObject
        {
            [DataMember(Order = 1)]
            public int Id { get; set; }

            [DataMember(Order = 2)]
            public string Name { get; set; }

            [DataMember(Order = 3)]
            public string Address { get; set; }

            [DataMember(Order = 4)]
            public int[] Scores { get; set; }
        }

        [Serializable]
        public class IserializableSimpleObject : ISerializable
        {
            public IserializableSimpleObject()
            {                
            }

            // this constructor is used for deserialization
            public IserializableSimpleObject(SerializationInfo info, StreamingContext text)
                : this()
            {
                Id = info.GetInt32("Id");
                Name = info.GetString("Name");
                Address = info.GetString("Address");
                Scores = (int[])info.GetValue("Scores", typeof(int[]));
            }

            public int Id { get; set; }

            public string Name { get; set; }

            public string Address { get; set; }

            public int[] Scores { get; set; }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Id", Id);
                info.AddValue("Name", Name);
                info.AddValue("Address", Address);
                info.AddValue("Scores", Scores);
            }
        }

        #endregion
    }
}
