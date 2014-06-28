using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MessageShark;

using MsgPack;

using Nessos.FsPickler;

using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;
using SimpleSpeedTester.Interfaces;

using JsonNetBsonReader = Newtonsoft.Json.Bson.BsonReader;
using JsonNetBsonWriter = Newtonsoft.Json.Bson.BsonWriter;
using JsonNetJsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace SimpleSpeedTester.Example
{
    using Filbert.Core;

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
        private static readonly List<TestRecords.SimpleRecord> SimpleRecords = Enumerable.Range(1, ObjectsCount).Select(GetSimpleRecord).ToList();
        private static readonly List<Bert> BertSimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectBert).ToList();
        private static readonly List<SimpleObjectWithFields> SimpleObjectsWithFields = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectWithFields).ToList();
        private static readonly List<IserializableSimpleObject> IserializableSimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSerializableSimpleObject).ToList();

        public static Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary, double>> Run()
        {
            var results = new Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary, double>>();

            results.Add(
                "BinaryFormatter (with properties)",
                DoSpeedTest(
                    "BinaryFormatter (with properties)", 
                    SimpleObjects, 
                    SerializeWithBinaryFormatter, 
                    DeserializeWithBinaryFormatter<SimpleObject>));

            results.Add(
                "BinaryFormatter (with fields)",
                DoSpeedTest(
                    "BinaryFormatter (with fields)", 
                    SimpleObjectsWithFields, 
                    SerializeWithBinaryFormatter, 
                    DeserializeWithBinaryFormatter<SimpleObjectWithFields>));

            // speed test binary formatter when used with an ISerializable type            
            results.Add(
                "BinaryFormatter (with ISerializable)",
                DoSpeedTest(
                    "BinaryFormatter (with ISerializable)", 
                    IserializableSimpleObjects, 
                    SerializeWithBinaryFormatter, 
                    DeserializeWithBinaryFormatter<IserializableSimpleObject>));

            results.Add(
                "Protobuf-Net (with properties)",
                DoSpeedTest(
                    "Protobuf-Net (with properties)", 
                    SimpleObjects, 
                    SerializeWithProtobufNet, 
                    DeserializeWithProtobufNet<SimpleObject>));

            results.Add(
                "Protobuf-Net (with fields)",
                DoSpeedTest(
                    "Protobuf-Net (with fields)", 
                    SimpleObjectsWithFields, 
                    SerializeWithProtobufNet, 
                    DeserializeWithProtobufNet<SimpleObjectWithFields>));

            // speed test binary writer (only for reference, won't be able to deserialize)
            results.Add(
                "BinaryWriter",
                DoSpeedTest(
                    "BinaryWriter", 
                    SimpleObjects, 
                    SerializeWithBinaryWriter, 
                    DeserializeWithBinaryReader,
                    ignoreDeserializationResult: true));

            results.Add(
                "FsPickler (F# records)",
                DoSpeedTest(
                    "FsPickler", 
                    SimpleRecords, 
                    SerializeWithFsPickler, 
                    DeserializeWithFsPickler<TestRecords.SimpleRecord>));

            results.Add(
                "MessagePack (with properties)",
                DoSpeedTest(
                    "MessagePack (with properties)",
                    SimpleObjects,
                    lst => SerializeWithMessagePack(lst, true),
                    lst => DeserializeWithMessagePack<SimpleObject>(lst, true)));

            results.Add(
                "MessagePack (with fields)",
                DoSpeedTest(
                    "MessagePack (with fields)",
                    SimpleObjectsWithFields,
                    lst => SerializeWithMessagePack(lst, false),
                    lst => DeserializeWithMessagePack<SimpleObjectWithFields>(lst, false)));

            results.Add(
                "MessageShark (with properties)",
                DoSpeedTest(
                    "MessageShark (with properties)", 
                    SimpleObjects, 
                    SerializeWithMessageShark, 
                    DeserializeWithMessageShark<SimpleObject>));

            results.Add(
                "FluorineFx",
                DoSpeedTest(
                    "FluorineFx", 
                    SimpleObjects, 
                    SerializeWithFluorineFx, 
                    DeserializeWithFluorineFx<SimpleObject>));

            results.Add(
                "Filbert",
                DoSpeedTest(
                    "Filbert", 
                    BertSimpleObjects, 
                    SerializeWithFilbert, 
                    DeserializeWithFilbert));

            results.Add(
                "Json.Net BSON",
                DoSpeedTest(
                    "Json.Net BSON", 
                    SimpleObjects, 
                    SerializeWithJsonNetBson, 
                    DeserializeWithJsonNetBson<SimpleObject>));

            return results;
        }

        private static Tuple<ITestResultSummary, ITestResultSummary, double> DoSpeedTest<T>(
            string testGroupName, 
            List<T> objects, 
            Func<List<T>, List<byte[]>> serializeFunc, 
            Func<List<byte[]>, List<T>> deserializeFunc,
            bool ignoreSerializationResult = false,
            bool ignoreDeserializationResult = false)
        {
            var byteArrays = new List<byte[]>();

            var testGroup = new TestGroup(testGroupName);

            var serializationTestSummary =
                testGroup
                    .Plan("Serialization", () => byteArrays = serializeFunc(objects), TestRuns)
                    .GetResult()
                    .GetSummary(OutcomeFilter);            

            Console.WriteLine(serializationTestSummary);

            var avgPayload = byteArrays.Average(arr => arr.Length);
            Console.WriteLine("Test Group [{0}] average serialized byte array size is [{1}]", testGroupName, avgPayload);

            var clones = new List<T>();
            ITestResultSummary deserializationTestSummary = null;

            if (deserializeFunc != null)
            {
                deserializationTestSummary =
                    testGroup
                        .Plan("Deserialization", () => clones = deserializeFunc(byteArrays), TestRuns)
                        .GetResult()
                        .GetSummary(OutcomeFilter);

                Console.WriteLine(deserializationTestSummary);
            }

            Console.WriteLine("--------------------------------------------------------\n\n");

            return Tuple.Create(
                ignoreSerializationResult ? null : serializationTestSummary, 
                ignoreDeserializationResult ? null : deserializationTestSummary, 
                avgPayload);
        }

        private static Bert GetSimpleObjectBert(int id)
        {
            return Bert.NewTuple(new[]
            {
                Bert.NewTuple(new[] { Bert.NewAtom("Name"), Bert.NewAtom("Simple") }),
                Bert.NewTuple(new[] { Bert.NewAtom("Id"), Bert.NewInteger(100000) }),
                Bert.NewTuple(new[] { Bert.NewAtom("Address"), Bert.NewByteList(System.Text.Encoding.ASCII.GetBytes("Planet Earth")) }),
                Bert.NewTuple(new[] { Bert.NewAtom("Scores"), Bert.NewList(Enumerable.Range(0, 10).Select(Bert.NewInteger).ToArray()) })                
            });
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

        private static TestRecords.SimpleRecord GetSimpleRecord(int id)
        {
            return new TestRecords.SimpleRecord(100000, "Simple", "Planet Earth", Enumerable.Range(0, 10).ToArray());
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

        private static int[] ReadScores(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            return Enumerable.Range(0, count).Select(_ => reader.ReadInt32()).ToArray();
        }

        private static List<SimpleObject> DeserializeWithBinaryReader(List<byte[]> payloads)
        {
            return
                payloads.Select(o =>
                {
                    using (var ms = new MemoryStream(o))
                    {
                        var reader = new BinaryReader(ms);
                        return new SimpleObject
                        {
                            Id = reader.ReadInt32(),
                            Name = reader.ReadString(),
                            Address = reader.ReadString(),
                            Scores = ReadScores(reader)
                        };
                    }
                }).ToList();
        }

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
                binaryWriter.Write(obj.Scores.Length);
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

        #region FluorineFX

        private static List<byte[]> SerializeWithFluorineFx<T>(List<T> objects)
            where T : class 
        {
            return objects.Select(SerializeWithFluorineFx).ToList();
        }

        private static byte[] SerializeWithFluorineFx<T>(T obj)
        {
            using (var memStream = new MemoryStream())
            {
                var writer = new FluorineFx.IO.AMFWriter(memStream);
                writer.WriteAMF3Object(obj);
                return memStream.ToArray();
            }
        }

        private static List<T> DeserializeWithFluorineFx<T>(List<byte[]> byteArrays)
            where T : class
        {
            return byteArrays.Select(DeserializeWithFluorineFx<T>).ToList();
        }

        private static T DeserializeWithFluorineFx<T>(byte[] byteArray)
        {
            using (var memStream = new MemoryStream(byteArray))
            {
                var reader = new FluorineFx.IO.AMFReader(memStream);
                return (T)reader.ReadAMF3Object();
            }
        }

        #endregion

        #region Filbert

        private static List<byte[]> SerializeWithFilbert(List<Bert> objects)
        {
            return objects.Select(SerializeWithFilbert).ToList();
        }

        private static byte[] SerializeWithFilbert(Bert bert)
        {
            using (var memStream = new MemoryStream())
            {
                Filbert.Encoder.encode(bert, memStream);
                return memStream.ToArray();
            }
        }

        private static List<Bert> DeserializeWithFilbert(List<byte[]> byteArrays)
        {
            return byteArrays.Select(DeserializeWithFilbert).ToList();
        }

        private static Bert DeserializeWithFilbert(byte[] byteArray)
        {
            using (var memStream = new MemoryStream(byteArray))
            {
                return Filbert.Decoder.decode(memStream);
            }
        }

        #endregion

        #region Json.Net BSON

        private static List<byte[]> SerializeWithJsonNetBson<T>(List<T> objects)
        {
            var serializer = new JsonNetJsonSerializer();
            return objects.Select(obj => SerializeWithJsonNetBson(obj, serializer)).ToList();
        }

        private static byte[] SerializeWithJsonNetBson<T>(T obj, JsonNetJsonSerializer serializer)
        {
            using (var memStream = new MemoryStream())
            {
                var writer = new JsonNetBsonWriter(memStream);
                serializer.Serialize(writer, obj);

                return memStream.ToArray();
            }
        }

        private static List<T> DeserializeWithJsonNetBson<T>(List<byte[]> byteArrays)
        {
            var serializer = new JsonNetJsonSerializer();
            return byteArrays.Select(arr => DeserializeWithJsonNetBson<T>(arr, serializer)).ToList();
        }

        private static T DeserializeWithJsonNetBson<T>(byte[] byteArray, JsonNetJsonSerializer serializer)
        {
            using (var memStream = new MemoryStream(byteArray))
            {
                var reader = new JsonNetBsonReader(memStream);
                return serializer.Deserialize<T>(reader);
            }
        }

        #endregion

        #region FsPickler

        private static List<T> DeserializeWithFsPickler<T>(List<byte[]> payloads)
        {
            var fsp = FsPickler.CreateBinary();

            return
                payloads.Select(payload =>
                {
                    using (var ms = new MemoryStream(payload))
                    {
                        return fsp.Deserialize<T>(ms);
                    }
                }).ToList();
        }

        private static List<byte[]> SerializeWithFsPickler<T>(List<T> objects)
        {
            var fsp = FsPickler.CreateBinary();

            return
                objects.Select(o =>
                {
                    using (var ms = new MemoryStream())
                    {
                        fsp.Serialize(ms, o);
                        return ms.ToArray();
                    }
                }).ToList();
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