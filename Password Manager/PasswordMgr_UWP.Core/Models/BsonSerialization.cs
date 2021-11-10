using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace PasswordMgr_UWP.Core.Models
{
    public static class BsonSerialization
    {
        /// <summary>
        /// Serializes an object to bson text.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The .bson text of the serialized object.</returns>
        public static string Serialize(object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BsonDataWriter bsonWriter = new BsonDataWriter(ms))
                    jsonser.Serialize(bsonWriter, obj);

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Deserializes bson text to an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="bson">The bson text to deserialize.</param>
        /// <returns>The object from the deserialized .bson test.</returns>
        public static T Deserialize<T>(string bson)
        {
            byte[] data = Convert.FromBase64String(bson);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BsonDataReader bsonReader = new BsonDataReader(ms))
                {
                    return jsonser.Deserialize<T>(bsonReader);
                }
            }
        }


        /// <summary>
        /// Converts a .json string to a .bson string.
        /// </summary>
        /// <typeparam name="T">The type of the object to convert.</typeparam>
        /// <param name="json">The json text to convert.</param>
        /// <returns>The .json text converted to .bson.</returns>
        public static string JsonToBson<T>(string json)
        {
            var obj = JsonConvert.DeserializeObject<T>(json);
            return Serialize(obj);
        }

        /// <summary>
        /// Converts a .bson string to a .json string.
        /// </summary>
        /// <typeparam name="T">The type of the object to convert.</typeparam>
        /// <param name="bson">The bson text to convert.</param>
        /// <returns>The .bson text converted to .json.</returns>
        public static string BsonToJson<T>(string bson)
        {
            byte[] data = Convert.FromBase64String(bson);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BsonDataReader bsonReader = new BsonDataReader(ms))
                {
                    T obj = jsonser.Deserialize<T>(bsonReader);
                    return JsonConvert.SerializeObject(obj);
                }
            }
        }

        private static readonly JsonSerializer jsonser = new JsonSerializer();
    }
}
