using Newtonsoft.Json;
using System;

namespace RecM
{
    public static class Json
    {
        public static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            T obj;
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                };

                obj = JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                obj = null;
            }

            return obj;
        }

        public static T Parse<T>(string json, string nullified = "") where T : struct
        {
            if (string.IsNullOrWhiteSpace(json)) return default;

            T obj;
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                };

                obj = JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                obj = default;
            }

            return obj;
        }

        public static string Stringify(object data)
        {
            if (data == null) return null;

            string json;
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                json = JsonConvert.SerializeObject(data, settings);
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                json = null;
            }

            return json;
        }
    }
}