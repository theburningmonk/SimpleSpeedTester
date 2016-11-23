using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MessageShark;

using MsgPack;

using MBrace.FsPickler;

using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;
using SimpleSpeedTester.Interfaces;

using JsonNetBsonReader = Newtonsoft.Json.Bson.BsonReader;
using JsonNetBsonWriter = Newtonsoft.Json.Bson.BsonWriter;
using JsonNetJsonSerializer = Newtonsoft.Json.JsonSerializer;

using Bond;
using Bond.Protocols;
using Bond.IO.Safe;

namespace SimpleSpeedTester.Example
{
    using Filbert.Core;
    using ZeroFormatter;

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
        private static readonly List<SimpleBondObject> SimpleBondObjects = Enumerable.Range(1, ObjectsCount).Select(GetSimpleBondObject).ToList();
        private static readonly List<TestRecords.SimpleRecord> SimpleRecords = Enumerable.Range(1, ObjectsCount).Select(GetSimpleRecord).ToList();
        private static readonly List<Bert> BertSimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectBert).ToList();
        private static readonly List<SimpleObjectWithFields> SimpleObjectsWithFields = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectWithFields).ToList();
        private static readonly List<SimpleObjectWithFieldsStruct> SimpleObjectsWithFieldsStruct = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObjectWithFieldsStruct).ToList();
        private static readonly List<IserializableSimpleObject> IserializableSimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSerializableSimpleObject).ToList();

        public static Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary, double>> Run()
        {
            var results = new Dictionary<string, Tuple<ITestResultSummary, ITestResultSummary, double>>();

            results.Add(
                "Bond v4.0.2",
                DoSpeedTest(
                    "Bond",
                    SimpleBondObjects,
                    SerializeWithBond,
                    DeserializeWithBond<SimpleBondObject>));

            results.Add(
                "HandCoded",
                DoSpeedTest(
                    "HandCoded",
                    SimpleObjects,
                    HandCodedSerialize,
                    HandCodedDeserialize));

            results.Add(
                "BinaryFormatter (properties)",
                DoSpeedTest(
                    "BinaryFormatter (with properties)",
                    SimpleObjects,
                    SerializeWithBinaryFormatter,
                    DeserializeWithBinaryFormatter<SimpleObject>));

            results.Add(
                "BinaryFormatter (fields)",
                DoSpeedTest(
                    "BinaryFormatter (with fields)",
                    SimpleObjectsWithFields,
                    SerializeWithBinaryFormatter,
                    DeserializeWithBinaryFormatter<SimpleObjectWithFields>));

            // speed test binary formatter when used with an ISerializable type            
            results.Add(
                "BinaryFormatter (ISerializable)",
                DoSpeedTest(
                    "BinaryFormatter (with ISerializable)",
                    IserializableSimpleObjects,
                    SerializeWithBinaryFormatter,
                    DeserializeWithBinaryFormatter<IserializableSimpleObject>));

            results.Add(
                "Protobuf-Net (properties) v2.0.0.668",
                DoSpeedTest(
                    "Protobuf-Net (with properties)",
                    SimpleObjects,
                    SerializeWithProtobufNet,
                    DeserializeWithProtobufNet<SimpleObject>));

            results.Add(
                "Protobuf-Net (fields) v2.0.0.668",
                DoSpeedTest(
                    "Protobuf-Net (with fields)",
                    SimpleObjectsWithFields,
                    SerializeWithProtobufNet,
                    DeserializeWithProtobufNet<SimpleObjectWithFields>));

            results.Add(
                "ZeroFormatter(properties)",
                DoSpeedTest(
                    "ZeroFormatter (with properties)",
                    SimpleObjects,
                    SerializeWithZeroFormatter,
                    DeserializeWithZeroFormatter<SimpleObject>));

            results.Add(
                "ZeroFormatter (fields, struct)",
                DoSpeedTest(
                    "ZeroFormatter (with fields, struct)",
                    SimpleObjectsWithFieldsStruct,
                    SerializeWithZeroFormatter,
                    DeserializeWithZeroFormatter<SimpleObjectWithFieldsStruct>));

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
                "FsPickler (F# records) v1.7.1",
                DoSpeedTest(
                    "FsPickler",
                    SimpleRecords,
                    SerializeWithFsPickler,
                    DeserializeWithFsPickler<TestRecords.SimpleRecord>));

            results.Add(
                "MessagePack (properties) v0.1.0",
                DoSpeedTest(
                    "MessagePack (with properties)",
                    SimpleObjects,
                    lst => SerializeWithMessagePack(lst, true),
                    lst => DeserializeWithMessagePack<SimpleObject>(lst, true)));

            results.Add(
                "MessagePack (fields) v0.1.0",
                DoSpeedTest(
                    "MessagePack (with fields)",
                    SimpleObjectsWithFields,
                    lst => SerializeWithMessagePack(lst, false),
                    lst => DeserializeWithMessagePack<SimpleObjectWithFields>(lst, false)));

            results.Add(
                "MessageShark (properties)",
                DoSpeedTest(
                    "MessageShark (with properties)",
                    SimpleObjects,
                    SerializeWithMessageShark,
                    DeserializeWithMessageShark<SimpleObject>));

            results.Add(
                "FluorineFx v1.2.4",
                DoSpeedTest(
                    "FluorineFx",
                    SimpleObjects,
                    SerializeWithFluorineFx,
                    DeserializeWithFluorineFx<SimpleObject>));

            results.Add(
                "Filbert v0.2.0",
                DoSpeedTest(
                    "Filbert",
                    BertSimpleObjects,
                    SerializeWithFilbert,
                    DeserializeWithFilbert));

            results.Add(
                "Json.Net BSON v8.0.2",
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

        private static SimpleBondObject GetSimpleBondObject(int id)
        {
            return new SimpleBondObject
            {
                Name = "Simple",
                Id = 100000,
                Address = "Planet Earth",
                Scores = Enumerable.Range(0, 10).ToList()
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

        private static SimpleObjectWithFieldsStruct GetSimpleObjectWithFieldsStruct(int id)
        {
            return new SimpleObjectWithFieldsStruct
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

        #region Bond

        private static List<byte[]> SerializeWithBond<T>(List<T> objects)
        {
            return objects.Select(SerializeWithBond).ToList();
        }

        private static byte[] SerializeWithBond<T>(T obj)
        {
            var output = new OutputBuffer(256);
            var writer = new CompactBinaryWriter<OutputBuffer>(output);

            Serialize.To(writer, obj);
            return output.Data.ToArray();
        }

        private static List<T> DeserializeWithBond<T>(List<byte[]> byteArrays)
        {
            return byteArrays.Select(arr => DeserializeWithBond<T>(arr)).ToList();
        }

        private static T DeserializeWithBond<T>(byte[] byteArray)
        {
            var input = new InputBuffer(byteArray);
            var reader = new CompactBinaryReader<InputBuffer>(input);

            return Deserialize<T>.From(reader);
        }

        #endregion

        #region Hand Coded

        private static List<SimpleObject> HandCodedDeserialize(List<byte[]> payloads)
        {
            return
                payloads.Select(bytes =>
                {
                    var index = 0;
                    var result = new SimpleObject();

                    result.Id = ReadInt32(bytes, ref index);

                    var len = ReadInt32(bytes, ref index);
                    result.Name = Encoding.UTF8.GetString(bytes, index, len);
                    index += len;

                    len = ReadInt32(bytes, ref index);
                    result.Address = Encoding.UTF8.GetString(bytes, index, len);
                    index += len;

                    len = ReadInt32(bytes, ref index);
                    result.Scores = new int[len];

                    for (var i = 0; i < len; i++)
                    {
                        result.Scores[i] = ReadInt32(bytes, ref index);
                    }

                    return result;
                }).ToList();
        }

        private static List<byte[]> HandCodedSerialize(List<SimpleObject> objects)
        {
            return objects.Select(HandCodedSerialize).ToList();
        }

        private const int Flag = 0x80;
        private const int Bits = 0x7F;

        private static void WriteInt32(MemoryStream stream, int value)
        {
            var b = Bits & value;
            value >>= 7;

            while (value != 0)
            {
                stream.WriteByte((byte)b);

                b = (byte)(Bits & value);
                value >>= 7;
            }

            stream.WriteByte((byte)(Flag | b));
        }

        private static int ReadInt32(byte[] bytes, ref int index)
        {
            var b = bytes[index++];
            var result = b & Bits;
            var shift = 7;

            while (b < 128)
            {
                b = bytes[index++];
                result = ((b & Bits) << shift) | result;
                shift += 7;
            }

            return result;
        }

        private static byte[] HandCodedSerialize(SimpleObject obj)
        {
            using (var ms = new MemoryStream())
            {
                WriteInt32(ms, obj.Id);

                var bytes = Encoding.UTF8.GetBytes(obj.Name);
                WriteInt32(ms, bytes.Length);
                ms.Write(bytes, 0, bytes.Length);

                bytes = Encoding.UTF8.GetBytes(obj.Address);
                WriteInt32(ms, bytes.Length);
                ms.Write(bytes, 0, bytes.Length);

                WriteInt32(ms, obj.Scores.Length);
                for (var i = 0; i < obj.Scores.Length; i++)
                {
                    WriteInt32(ms, obj.Scores[i]);
                }

                return ms.ToArray();
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

        #region ZeroFormatter

        private static List<byte[]> SerializeWithZeroFormatter<T>(List<T> objects)
        {
            return objects.Select(SerializeWithZeroFormatter).ToList();
        }

        private static byte[] SerializeWithZeroFormatter<T>(T obj)
        {
            return ZeroFormatterSerializer.Serialize(obj);
        }

        private static List<T> DeserializeWithZeroFormatter<T>(List<byte[]> byteArrays)
        {
            return byteArrays.Select(DeserializeWithZeroFormatter<T>).ToList();
        }

        private static T DeserializeWithZeroFormatter<T>(byte[] byteArray)
        {
            return ZeroFormatterSerializer.Deserialize<T>(byteArray);
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
            var fsp = FsPickler.CreateBinarySerializer();

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
            var fsp = FsPickler.CreateBinarySerializer();

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

        [ZeroFormattable]
        public struct SimpleObjectWithFieldsStruct
        {
            [Index(0)]
            public int Id;
            [Index(1)]
            public string Name;

            [Index(2)]
            public string Address;

            [Index(3)]
            public int[] Scores;

            public SimpleObjectWithFieldsStruct(int id, string name, string address, int[] scores)
            {
                this.Id = id;
                this.Name = name;
                this.Address = address;
                this.Scores = scores;
            }
        }

        [Serializable]
        [DataContract]
        [ZeroFormattable]
        public class SimpleObject
        {
            [DataMember(Order = 1)]
            [Index(0)]
            public virtual int Id { get; set; }

            [DataMember(Order = 2)]
            [Index(1)]
            public virtual string Name { get; set; }

            [DataMember(Order = 3)]
            [Index(2)]
            public virtual string Address { get; set; }

            [DataMember(Order = 4)]
            [Index(3)]
            public virtual int[] Scores { get; set; }
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

        //------------------------------------------------------------------------------
        // This code was generated by a tool.
        //
        //   Tool : Bond Compiler 0.4.0.1
        //   File : test_types.cs
        //
        // Changes to this file may cause incorrect behavior and will be lost when
        // the code is regenerated.
        // <auto-generated />
        //------------------------------------------------------------------------------

        #region ReSharper warnings
        // ReSharper disable PartialTypeWithSinglePart
        // ReSharper disable RedundantNameQualifier
        // ReSharper disable InconsistentNaming
        // ReSharper disable CheckNamespace
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable RedundantUsingDirective
        #endregion

        [global::Bond.Schema]
        [System.CodeDom.Compiler.GeneratedCode("gbc", "0.4.0.1")]
        public partial class SimpleBondObject
        {
            [global::Bond.Id(0)]
            public int Id { get; set; }

            [global::Bond.Id(1)]
            public string Name { get; set; }

            [global::Bond.Id(2)]
            public string Address { get; set; }

            [global::Bond.Id(3)]
            public List<int> Scores { get; set; }

            public SimpleBondObject()
                : this("SimpleSpeedTester.Example.SimpleBondObject", "SimpleBondObject") { }

            protected SimpleBondObject(string fullName, string name)
            {
                Name = "";
                Address = "";
                Scores = new List<int>();
            }
        }

        #endregion
    }
}