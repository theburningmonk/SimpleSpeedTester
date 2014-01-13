using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;
using System.Xaml;
using MongoDB.Bson;
using SimpleSpeedTester.Core;
using SimpleSpeedTester.Core.OutcomeFilters;
using SimpleSpeedTester.Interfaces;
using JayrockJsonConvert = Jayrock.Json.Conversion.JsonConvert;
using JsonNetJsonConvert = Newtonsoft.Json.JsonConvert;
using JsonNetJsonSerializer = Newtonsoft.Json.JsonSerializer;
using JsonNetBsonReader = Newtonsoft.Json.Bson.BsonReader;
using JsonNetBsonWriter = Newtonsoft.Json.Bson.BsonWriter;
using JsonFxReader = JsonFx.Json.JsonReader;
using JsonFxWriter = JsonFx.Json.JsonWriter;

namespace SimlpeSpeedTester.Example
{
    /// <summary>
    /// Demo program which compares the serializatoin and deserialization speed of 4 popular JSON serializers
    /// </summary>
    public static class JsonSerializersSpeedTest
    {
        // test serialization and deserialization on 100k objects
        private const int ObjectsCount = 100000;

        // perform 5 runs of each test
        private const int TestRuns = 5;

        private static readonly Random RandomGenerator = new Random((int)DateTime.UtcNow.Ticks);

        // exclude the min and max results 
        private static readonly ITestOutcomeFilter OutcomeFilter = new ExcludeMinAndMaxTestOutcomeFilter();

        // the objects to perform the tests with
        private static readonly List<SimpleObject> SimpleObjects = Enumerable.Range(1, ObjectsCount).Select(GetSimpleObject).ToList();

        public static void Start()
        {
            // speed test Json.Net serializer
            DoSpeedTest("Json.Net", SerializeWithJsonNet, DeserializeWithJsonNet<SimpleObject>, CountAverageJsonStringPayload);

            // speed test Json.Net BSON serializer
            DoSpeedTest("Json.Net BSON", SerializeWithJsonNetBson, DeserializeWithJsonNetBson<SimpleObject>, CountAverageByteArrayPayload);

            // speed test protobuf-net
            DoSpeedTest("Protobuf-Net", SerializeWithProtobufNet, DeserializeWithProtobufNet<SimpleObject>, CountAverageByteArrayPayload);

            // speed test ServiceStack.Text Json serializer
            DoSpeedTest("ServiceStack.Text", SerializeWithServiceStack, DeserializeWithServiceStack<SimpleObject>, CountAverageJsonStringPayload);

            // speed test DataContractJsonSerializer
            DoSpeedTest("DataContractJsonSerializer", SerializeWithDataContractJsonSerializer, DeserializeWithDataContractJsonSerializer<SimpleObject>, CountAverageJsonStringPayload);

            // speed test JavaScriptSerializer
            DoSpeedTest("JavaScriptSerializer", SerializeWithJavaScriptSerializer, DeserializeWithJavaScriptSerializer<SimpleObject>, CountAverageJsonStringPayload);

            // speed test SimpleJson
            DoSpeedTest("SimpleJson", SerializeWithSimpleJson, DeserializeWithSimpleJson<SimpleObject>, CountAverageJsonStringPayload);

            // speed test fastJson
            DoSpeedTest("fastJson", SerializeWithFastJson, DeserializeWithFastJson<SimpleObject>, CountAverageJsonStringPayload);

            // speed test JayRock
            DoSpeedTest("JayRock", SerializeWithJayRock, DeserializeWithJayRock<SimpleObject>, CountAverageJsonStringPayload);

            // speed test JsonFx
            DoSpeedTest("JsonFx", SerializeWithJsonFx, DeserializeWithJsonFx<SimpleObject>, CountAverageJsonStringPayload);

            // speed test MongoDB Driver
            DoSpeedTest("MongoDB Driver", SerializeWithMongoDbDriver, DeserializeWithMongoDbDriver<SimpleObject>, CountAverageJsonStringPayload);

            // speed test MongoDB Driver
            DoSpeedTest("MongoDB Driver BSON", SerializeWithMongoDbDriverBson, DeserializeWithMongoDbDriverBson<SimpleObject>, CountAverageByteArrayPayload);

            // speed test XamlServices
            //DoSpeedTest("XamlServices", SerializeWithXamlServices, DeserializeWithXamlServices<SimpleObject>);
        }

        private static void DoSpeedTest<T>(
            string testGroupName, 
            Func<List<SimpleObject>, List<T>> serializeFunc, 
            Func<List<T>, List<SimpleObject>> deserializeFunc,
            Func<List<T>, double> getAvgPayload)
        {
            var data = new List<T>();

            var testGroup = new TestGroup(testGroupName);

            var serializationTestSummary =
                testGroup
                    .Plan("Serialization", () => data = serializeFunc(SimpleObjects), TestRuns)
                    .GetResult()
                    .GetSummary(OutcomeFilter);

            Console.WriteLine(serializationTestSummary);

            Console.WriteLine("Test Group [{0}] average serialized byte array size is [{1}]", testGroupName, getAvgPayload(data));

            var objects = new List<SimpleObject>();
            var deserializationTestSummary =
                testGroup
                    .Plan("Deserialization", () => objects = deserializeFunc(data), TestRuns)
                    .GetResult()
                    .GetSummary(OutcomeFilter);

            Console.WriteLine(deserializationTestSummary);

            Console.WriteLine("---------------------------------------------------------\n\n");
        }

        private static SimpleObject GetSimpleObject(int id)
        {
            return new SimpleObject
            {
                Name = string.Format("Simple-{0}", id),
                Id = RandomGenerator.Next(1, ObjectsCount),
                Address = "Planet Earth",
                Scores = Enumerable.Range(0, 10).Select(i => RandomGenerator.Next(1, 100)).ToArray()
            };
        }

        private static double CountAverageJsonStringPayload(List<string> jsonStrings)
        {
            return jsonStrings.Average(str => str.Length);
        }

        private static double CountAverageByteArrayPayload(List<byte[]> bsonArray)
        {
            return bsonArray.Average(arr => arr.Length);
        }

        #region DataContractJsonSerializer

        private static List<string> SerializeWithDataContractJsonSerializer<T>(List<T> objects)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var jsonStrings = objects.Select(
                o =>
                {
                    using (var memStream = new MemoryStream())
                    {
                        serializer.WriteObject(memStream, o);
                        return Encoding.Default.GetString(memStream.ToArray());
                    }
                }).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithDataContractJsonSerializer<T>(List<string> jsonStrings)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var objects = jsonStrings.Select(
                str =>
                {
                    using (var memStream = new MemoryStream(Encoding.Default.GetBytes(str)))
                    {
                        return (T)serializer.ReadObject(memStream);
                    }
                }).ToList();
            return objects;
        }

        #endregion

        #region JavaScriptSerializer

        private static List<string> SerializeWithJavaScriptSerializer<T>(List<T> objects)
        {
            var serializer = new JavaScriptSerializer();
            var jsonStrings = objects.Select(o => serializer.Serialize(o)).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithJavaScriptSerializer<T>(List<string> jsonStrings)
        {
            var serializer = new JavaScriptSerializer();
            var objects = jsonStrings.Select(serializer.Deserialize<T>).ToList();
            return objects;
        }

        #endregion

        #region Json.Net

        private static List<string> SerializeWithJsonNet<T>(List<T> objects)
        {
            var jsonStrings = objects.Select(o => JsonNetJsonConvert.SerializeObject(o)).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithJsonNet<T>(List<string> jsonStrings)
        {
            var objects = jsonStrings.Select(JsonNetJsonConvert.DeserializeObject<T>).ToList();
            return objects;
        }

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

        #region ServiceStack

        private static List<string> SerializeWithServiceStack<T>(List<T> objects)
        {
            var serializer = new ServiceStack.Text.JsonSerializer<T>();

            var jsonStrings = objects.Select(serializer.SerializeToString).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithServiceStack<T>(List<string> jsonStrings)
        {
            var serializer = new ServiceStack.Text.JsonSerializer<T>();

            var objects = jsonStrings.Select(serializer.DeserializeFromString).ToList();
            return objects;
        }

        #endregion

        #region SimpleJson

        private static List<string> SerializeWithSimpleJson<T>(List<T> objects)
        {
            var jsonStrings = objects.Select(o => SimpleJson.SimpleJson.SerializeObject(o)).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithSimpleJson<T>(List<string> jsonStrings)
        {
            var objects = jsonStrings.Select(SimpleJson.SimpleJson.DeserializeObject<T>).ToList();
            return objects;
        }

        #endregion

        #region FastJson

        private static List<string> SerializeWithFastJson<T>(List<T> objects) {
            var jsonStrings = objects.Select(o => fastJSON.JSON.Instance.ToJSON(o)).ToList();
            return jsonStrings;
        }

        private static List<T> DeserializeWithFastJson<T>(List<string> jsonStrings) {
            var objects = jsonStrings.Select(fastJSON.JSON.Instance.ToObject<T>).ToList();
            return objects;
        }

        #endregion

        #region JayRock

        private static List<string> SerializeWithJayRock<T>(List<T> objects) {
            var jsonStrings = 
                objects.Select(o => JayrockJsonConvert.ExportToString(o)).ToList();
                
            return jsonStrings;
        }

        private static List<T> DeserializeWithJayRock<T>(List<string> jsonStrings) {
            var objects = jsonStrings.Select(JayrockJsonConvert.Import<T>).ToList();
            return objects;
        }

        #endregion

        #region JsonFx

        private static List<string> SerializeWithJsonFx<T>(List<T> objects)
        {
            var writer = new JsonFxWriter();

            var jsonStrings = objects.Select(o => writer.Write(o)).ToList();                
            return jsonStrings;
        }

        private static List<T> DeserializeWithJsonFx<T>(List<string> jsonStrings)
        {
            var reader = new JsonFxReader();

            var objects = jsonStrings.Select(reader.Read<T>).ToList();
            return objects;
        }

        #endregion

        #region MongoDB Driver

        private static List<string> SerializeWithMongoDbDriver<T>(List<T> objects)
        {
            return objects.Select(o => BsonExtensionMethods.ToJson(o)).ToList();
        }

        private static List<T> DeserializeWithMongoDbDriver<T>(List<string> jsonStrings)
        {
            return jsonStrings.Select(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<T>).ToList();
        }

        private static List<byte[]> SerializeWithMongoDbDriverBson<T>(List<T> objects)
        {
            return objects.Select(o => o.ToBson()).ToList();
        }

        private static List<T> DeserializeWithMongoDbDriverBson<T>(List<byte[]> byteArrays)
        {
            return byteArrays.Select(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<T>).ToList();
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

        #region XamlServices

        private static List<string> SerializeWithXamlServices<T>(List<T> objects) {
            var xamlStrings = objects.Select(o => XamlServices.Save(o)).ToList();

            return xamlStrings;
        }

        private static List<T> DeserializeWithXamlServices<T>(List<string> xamlStrings)
        {
            var objects = xamlStrings.Select(
                str =>
                {
                    using (var memStream = new MemoryStream(Encoding.Default.GetBytes(str)))
                    {
                        return XamlServices.Load(memStream);
                    }
                }).Cast<T>().ToList();
            return objects;
        }

        #endregion
    }

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
}