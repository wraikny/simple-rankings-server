using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Globalization;

namespace SimpleRankingsServer
{
    [DataContract]
    public class Data<T>
    {
        [DataMember(Name = "id")]
        public long Id { get; internal set; }

        [DataMember(Name = "userId")]
        internal string UserIdStr { get; set; }

        [DataMember(Name = "utcDate")]
        internal string UTCDateStr { get; set; }

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

        private DateTime utcDate;
        public DateTime UTCDate
        {
            get => utcDate;
            set
            {
                utcDate = value;
                UTCDateStr = value.ToString("yyyy/MM/dd/HH/mm/ss");
            }
        }

        public override string ToString()
        {
            return $"{{ \"id\" : {Id}, \"userId\" : {UserIdStr}, \"utcDate\" : {UTCDateStr}, \"values\" : {Values.ToString()} }}";
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext s)
        {
            userId = Guid.Parse(UserIdStr);
            utcDate = DateTime.ParseExact(UTCDateStr, "yyyy/MM/dd HH:mm:ss", null);
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

    public class Client : IDisposable
    {
        private readonly string url;
        private readonly HttpClient client = new HttpClient();

        public Client(string url, string username, string password)
        {
            this.url = url;
            var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", parameter);
        }

        public void Dispose()
        {
            client.Dispose();
        }

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

        public async Task<long> Insert<T>(Guid userId, T data)
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

        public async Task<Data<T>[]> Select<T>(string orderBy = null, bool? isDescending = null, int? limit = 100)
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