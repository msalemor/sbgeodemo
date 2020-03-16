using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SBDemo.Domain.Services
{
    public static class EncodingService
    {
        public static byte[] Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var b64 = Convert.ToBase64String(plainTextBytes);
            return Encoding.UTF8.GetBytes(b64);
        }

        public static string Base64Decode(byte[] body)
        {
            var base64bytes = Encoding.UTF8.GetString(body);
            var base64EncodedBytes = Convert.FromBase64String(base64bytes);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string EncodeJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T DecodeJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
