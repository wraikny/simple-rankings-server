using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SimpleRankingsServer
{
    [DataContract]
    public class Data<T>
    {
        [DataMember(Name = "id")]
        public long Id { get; internal set; }

        [DataMember(Name = "userId")]
        internal string UserIdStr { get; set; }

        [DataMember(Name = "values")]
        public T Values { get; internal set; }

        private Guid userId;
        public Guid UserId
        {
            get => userId;
            set
            {
                userId = value;
                UserIdStr = value.ToString();
            }
        }

        public override string ToString()
        {
            return $"{{ \"id\" : {Id}, \"userId\" : {UserId}, \"values\" : {Values.ToString()} }}";
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext s)
        {
            userId = Guid.Parse(UserIdStr);
        }
    }

    public static class JsonUtils
    {
        public static string Serialize<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }

    public static class Client
    {
        private static readonly HttpClient client = new HttpClient();

        [DataContract]
        private class InsertParam<T>
        {
            [DataMember(Name = "userId")]
            public string UserIdStr { get; set; }
            [DataMember(Name = "values")]
            public T Values { get; set; }

            private Guid userId;
            public Guid UserId
            {
                get => userId;
                set
                {
                    userId = value;
                    UserIdStr = value.ToString();
                }
            }
        }

        [DataContract]
        private class InsertResult
        {
            [DataMember(Name = "id")]
            public long Id { get; set; }
        }

        public static async Task<long> Insert<T>(string url, Guid userId, T data)
        {
            var json = JsonUtils.Serialize(new InsertParam<T> { UserId = userId, Values = data });
            using (var content = new StringContent(json, Encoding.UTF8, @"application/json"))
            {
                var result = await client.PostAsync(url, content);
                var resString = await result.Content.ReadAsStringAsync();

                if (result.IsSuccessStatusCode)
                {
                    return JsonUtils.Deserialize<InsertResult>(resString).Id;
                }

                throw new Exception($"{result.StatusCode}:{resString}");
            }
        }

        public static async Task<Data<T>[]> Select<T>(string url, string orderBy = null, bool? isDescending = null, int? limit = null)
        {
            var param = new Dictionary<string, string>();

            if (orderBy != null) param.Add("orderBy", orderBy);
            if (isDescending != null) param.Add("isDescending", isDescending.Value.ToString());
            if (limit != null) param.Add("limit", limit.Value.ToString());

            var result = await client.GetAsync($"{url}?{await new FormUrlEncodedContent(param).ReadAsStringAsync()}");
            var resString = await result.Content.ReadAsStringAsync();

            if (result.IsSuccessStatusCode)
            {
                return JsonUtils.Deserialize<Data<T>[]>(resString);
            }

            throw new Exception($"{result.StatusCode}:{resString}");
        }
    }
}